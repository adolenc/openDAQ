using RTGen.Exceptions;
using RTGen.Generation;
using RTGen.Interfaces;
using RTGen.Types;
using RTGen.Util;
using RTGen.Cpp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RTGen.C.Generators
{
    class CGenerator : TemplateGenerator
    {
        protected enum GeneratorType
        {
            Header = 0,
            Source = 1,
        }

        protected enum ArgsListType
        {
            MethodDeclaration,
            ArgsForwarding
        }

        /// <summary>Value types whose C and C++ representations are distinct structs, and so need a hand
        /// written conversion (declared in "private/utils.h") when passed by value.</summary>
        /// <remarks>
        /// Only applies to by value arguments. The structs are layout compatible, so a pointer to one is
        /// simply reinterpreted, which also keeps out parameters writing into the caller's memory.
        /// </remarks>
        protected static readonly IDictionary<string, string> ValueTypeConverters = new Dictionary<string, string>
        {
            { "IntfID", "copendaq::utils::toDaqIntfId" },
            { "ComplexFloat64", "copendaq::utils::toDaqComplexFloat64" },
            { "SourceLocation", "copendaq::utils::toDaqSourceLocation" }
        };

        /// <summary>Renames the C name of a CoreTypes value type (the interface names are left untouched).</summary>
        /// <remarks>
        /// Plain C values get a "daqC" prefix, openDAQ objects keep the plain "daq" prefix, so that every
        /// interface "IX" maps to "daqX" without exception. Without this the C bindings, which drop the "I"
        /// prefix from interface names, would emit the same identifier for an interface and a value type
        /// that share a name (Float/IFloat, CoreType/ICoreType).
        /// Only the aliases of C fundamental types are renamed; openDAQ's own enums (SampleType, PacketType,
        /// LogLevel, ...) as well as ErrCode and IntfID are domain types rather than C values and keep their
        /// "daq" prefix.
        /// </remarks>
        protected static readonly IDictionary<string, string> ValueTypeNames = new Dictionary<string, string>
        {
            { "Bool", "CBool" },
            { "Int", "CInt" },
            { "UInt", "CUInt" },
            { "Float", "CFloat" },
            { "CharPtr", "CCharPtr" },
            { "ConstCharPtr", "CConstCharPtr" },
            { "VoidPtr", "CVoidPtr" },
            { "SizeT", "CSizeT" },
            { "EnumType", "CEnumType" },
            { "CoreType", "CCoreType" }
        };

        protected static readonly String Prefix = "daq";
        protected static readonly String PrefixUpper = "DAQ_";

        protected GeneratorType _generatorType = GeneratorType.Header;
        /// <summary>Exact file to write, used when generating a whole tree at once.</summary>
        protected string _outputPathOverride;
        protected ISet<string> _typesToDeclare = new HashSet<string>();

        //Overriden

        public override IVersionInfo Version => new VersionInfo
        {
            Major = 0,
            Minor = 7,
            Patch = 0
        };

        public override IGeneratorOptions Options
        {
            get => base.Options;
            set
            {
                base.Options = value;

                if (string.IsNullOrEmpty(base.Options.GeneratedExtension))
                {
                    base.Options.GeneratedExtension = ".h";
                }

                if (base.Options.LibraryInfo == null)
                {
                    base.Options.LibraryInfo = new LibraryInfo();
                }
                if (string.IsNullOrEmpty(base.Options.LibraryInfo.OutputName))
                {
                    base.Options.LibraryInfo.OutputName = "copendaq";
                }

                if (base.Options.GeneratedExtension == ".h")
                {
                    _generatorType = GeneratorType.Header;
                }
                else if (base.Options.GeneratedExtension == ".cpp")
                {
                    _generatorType = GeneratorType.Source;
                }
            }
        }

        protected override StringBuilder WriteMethods(IRTInterface rtClass, string baseTemplatePath)
        {
            return new StringBuilder();
        }

        /// <summary>Applies the value type renames to an already resolved type name.</summary>
        /// <param name="name">The type name without the "daq" prefix.</param>
        protected static string MapValueTypeName(string name)
        {
            return ValueTypeNames.TryGetValue(name, out string renamed) ? renamed : name;
        }

        /// <summary>Gets the C name of the type (without the "daq" prefix), honouring the value type renames.</summary>
        /// <param name="type">The type as parsed from the RT interface file.</param>
        protected static string GetCTypeName(ITypeName type)
        {
            return type.Flags.IsValueType ? MapValueTypeName(type.NonInterfaceName) : type.NonInterfaceName;
        }

        protected override string GetMethodArgumentVariable(IArgument arg, IOverload overload, string variable)
        {
            if (variable == "ArgTypeNameNonInterface")
            {
                return Prefix + GetCTypeName(arg.Type);
            }
            return null;
        }

        /// <summary>Gets the core include directory that corresponds to a generated library.</summary>
        /// <param name="library">Generated library, for example "ccoretypes" or "copendaq/signal".</param>
        protected static string GetCoreIncludeDir(string library)
        {
            switch (library)
            {
                case "ccoretypes":   return "coretypes";
                case "ccoreobjects": return "coreobjects";
                default:             return "opendaq";
            }
        }

        protected override string GetIncludes(IRTFile rtFile)
        {
            StringBuilder sb = new StringBuilder();

            if (_generatorType == GeneratorType.Header)
            {
                string common = "ccommon";

                sb.AppendLine($"#include <{common}.h>");
            }
            else if (_generatorType == GeneratorType.Source)
            {
                string lib = Options.LibraryInfo.Name;
                string file = Options.Filename ?? "";
                var header = (lib ?? "") + (String.IsNullOrEmpty(lib) ? "" : "/") + (file ?? "") + ".h";
                sb.AppendLine($"#include <{header}>");
                sb.AppendLine();
                sb.AppendLine("#include <opendaq/opendaq.h>");
                //the interface being wrapped is not necessarily reachable through the umbrella header
                sb.AppendLine($"#include <{GetCoreIncludeDir(lib)}/{RtFile.SourceFileName}>");
                sb.AppendLine();
                sb.AppendLine("#include <copendaq_private.h>");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates bindings for every interface below the source directory, so that no list of files has
        /// to be kept in sync with the repository. Entered when the input is a source tree rather than a
        /// single header (see <see cref="CBatchParser"/>).
        /// </summary>
        private void GenerateAll(CBatchFile batch, string templatePath)
        {
            string bindingsRoot = String.IsNullOrEmpty(Options.OutputDir) ? "." : Options.OutputDir;
            IParserOptions parserOptions = Options as IParserOptions;

            int generated = 0;
            int excluded = 0;
            List<string> withoutOutput = new List<string>();
            HashSet<string> expected = new HashSet<string>();

            foreach (BindingTarget target in CBatchGeneration.Discover(batch.SourceDir))
            {
                string headerPath = Path.Combine(bindingsRoot, "include", target.Library, target.Name + ".h");
                string sourcePath = Path.Combine(bindingsRoot, "src", target.Library, target.Name + ".cpp");
                expected.Add(Path.GetFullPath(headerPath));
                expected.Add(Path.GetFullPath(sourcePath));

                if (CBatchGeneration.IsExcluded(target.HeaderPath, out string reason))
                {
                    Log.Info($"excluded {target.Library}/{target.Name}: {reason}");
                    excluded++;
                    continue;
                }

                IRTFile parsed;
                try
                {
                    parsed = new CppParser().Parse(target.HeaderPath, parserOptions);
                    parsed.SourceFileName = Path.GetFileName(target.HeaderPath) ?? "";
                }
                catch (Exception e)
                {
                    withoutOutput.Add($"{target.Library}/{target.Name} ({e.Message.TrimEnd('.')})");
                    continue;
                }

                if (parsed.Classes.Count == 0 && parsed.Factories.Count == 0)
                {
                    withoutOutput.Add($"{target.Library}/{target.Name} (nothing to generate)");
                    continue;
                }

                foreach (var output in new[] { new { Ext = ".h", Path = headerPath },
                                               new { Ext = ".cpp", Path = sourcePath } })
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(output.Path) ?? ".");

                    IGeneratorOptions fileOptions = (IGeneratorOptions) Options.Clone();
                    fileOptions.Filename = target.Name;
                    fileOptions.GeneratedExtension = output.Ext;
                    fileOptions.LibraryInfo = new LibraryInfo
                    {
                        Name = target.Library,
                        Namespace = Options.LibraryInfo.Namespace,
                        Version = Options.LibraryInfo.Version,
                        OutputName = Options.LibraryInfo.OutputName
                    };

                    CGenerator generator = new CGenerator
                    {
                        Options = fileOptions,
                        RtFile = parsed,
                        _outputPathOverride = output.Path
                    };
                    generator.GenerateFile(templatePath);
                }

                generated++;
            }

            IList<string> orphans = CBatchGeneration.PruneOrphans(bindingsRoot, expected);
            foreach (string orphan in orphans)
            {
                Log.Info($"removed {orphan}: no longer generated by any interface");
            }

            CBatchGeneration.WriteUmbrellaHeader(bindingsRoot);

            Log.Info($"generated {generated}, excluded {excluded}, removed {orphans.Count}, no output {withoutOutput.Count}");
            if (withoutOutput.Count > 0)
            {
                Log.Info("No output produced, previously generated files were left untouched:");
                foreach (string name in withoutOutput)
                {
                    Log.Info($"    {name}");
                }
            }
        }

        public override void GenerateFile(string templatePath)
        {
            if (RtFile is CBatchFile batch)
            {
                GenerateAll(batch, templatePath);
                return;
            }

            string headerTemplatePath = GetHeaderTemplatePath(templatePath);
            if (Log.Verbose)
            {
                Log.Info($"Generating {Options.Language} bindings for file: {RtFile.SourceFileName} extension: {Options.GeneratedExtension}");
            }

            try
            {
                if (_generatorType == GeneratorType.Source)
                {
                    string tmpSourcePath = Path.GetTempFileName();
                    string outputSourcePath = GetOutputPath(".cpp");
                    GenerateSource(templatePath, tmpSourcePath);
                    File.Copy(tmpSourcePath, outputSourcePath, true);
                }
                else
                {
                    string tmpHeaderPath = Path.GetTempFileName();
                    string outputHeaderPath = GetOutputPath();
                    GenerateHeader(headerTemplatePath, tmpHeaderPath);
                    File.Copy(tmpHeaderPath, outputHeaderPath, true);
                }
            }
            catch (Exception e)
            {
                throw new GeneratorException($"Failed to generate {Options.Language} bindings for file: {RtFile.SourceFileName} with error: {e.Message}");
            }
        }

        //Overriden end

        protected string GetIntfIDDeclaration()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IRTInterface rtClass in RtFile.Classes)
            {
                string name = rtClass.Type.NonInterfaceName;
                sb.AppendLine(base.Indentation + $"EXPORTED extern const {Prefix}IntfID {PrefixUpper}{name.ToLowerSnakeCase().ToUpper()}_INTF_ID;");
                sb.AppendLine(base.Indentation + $"void EXPORTED {Prefix}{name}_getInterfaceId({Prefix}IntfID* intfId);");
            }
            return sb.ToString();
        }

        protected string GetIntfIDDefinition()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IRTInterface rtClass in RtFile.Classes)
            {
                string ns = rtClass.Type.Namespace.ToString();
                string iface = rtClass.Type.Name;
                string name = rtClass.Type.NonInterfaceName;
                string id = $"{PrefixUpper}{name.ToLowerSnakeCase().ToUpper()}_INTF_ID";

                sb.Append($"const {Prefix}IntfID {id} = ");
                sb.AppendLine($"{{ {ns}::{iface}::Id.Data1, {ns}::{iface}::Id.Data2, {ns}::{iface}::Id.Data3, {ns}::{iface}::Id.Data4_UInt64 }};");
                sb.AppendLine("");
                sb.AppendLine($"void {Prefix}{name}_getInterfaceId({Prefix}IntfID* intfId)");
                sb.AppendLine("{");
                sb.AppendLine(base.Indentation + $"*intfId = {id};");
                sb.AppendLine("}");
                sb.AppendLine("");
            }
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        protected string GetTypedefs()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var type in _typesToDeclare)
            {
                if (type == "BaseObject")
                {
                    continue;
                }
                sb.AppendLine(base.Indentation + $"typedef struct {Prefix}{type} {Prefix}{type};");
            }
            return sb.ToString();
        }

        protected string GetOutputPath(string overridenExtension = null)
        {
            if (!String.IsNullOrEmpty(_outputPathOverride))
            {
                return _outputPathOverride;
            }
            return String.IsNullOrWhiteSpace(overridenExtension) ? GetOutputFilePath() : Path.ChangeExtension(GetOutputFilePath(), overridenExtension);
        }

        protected string GetHeaderTemplatePath(string templatePath)
        {
            return Path.Combine(Path.GetDirectoryName(templatePath), "c.header.template");
        }

        /// <summary>Gets the namespace of the interface, falling back to the library one.</summary>
        /// <param name="iface">Interface being generated, which is a placeholder for factory only headers.</param>
        protected string GetNamespace(IRTInterface iface)
        {
            string ns = iface?.Type?.Namespace?.ToString();
            return String.IsNullOrEmpty(ns) ? Options.LibraryInfo.Namespace?.ToString() ?? "daq" : ns;
        }

        protected string ArgumentListToString(IEnumerable<IArgument> args, string separator)
        {
            return String.Join(separator, args.Select(arg => arg.Name));
        }

        protected string ArgumentsToString<T>(IRTInterface iface, T methodOrFactory, ArgsListType listType)
        {
            IRTFactory factory = methodOrFactory as IRTFactory;
            IMethod method = methodOrFactory as IMethod;
            IOverload overload = method != null ? method.Overloads[0] : factory.ToOverload();

            IList<IArgument> args = overload.Arguments.ToList();

            //adding self pointer to the method declaration
            if (listType == ArgsListType.MethodDeclaration && method != null)
            {
                IArgument self = new Argument(iface.Type, "self");
                self.Type.Modifiers = "*";
                args.Insert(0, self);
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < args.Count; i++)
            {
                IArgument arg = args[i];

                if (!arg.Type.Flags.IsValueType)
                {
                    //filling types for typedefs
                    if (arg.Type.Name != "void") _typesToDeclare.Add(arg.Type.NonInterfaceName);
                }

                if (listType == ArgsListType.MethodDeclaration)
                {
                    sb.Append($"{(arg.Type.Name != "void" ? Prefix : "")}{GetCTypeName(arg.Type)}{arg.Type.Modifiers} {arg.Name}");
                }
                else
                {
                    //skipping self pointer in factory call
                    if (factory != null && i == 0)
                    {
                        if (i != args.Count - 1) sb.Append(", ");
                        continue;
                    }

                    if (arg.Type.Flags.IsValueType || arg.Type.Name == "void")
                    {
                        if ((!arg.Type.Flags.IsCoreType && arg.Type.Name != "void") || arg.Type.Name == "CoreType")
                        {
                            if (String.IsNullOrEmpty(arg.Type.Modifiers))
                            {
                                //a struct passed by value has no conversion to its C++ counterpart, so it goes through a helper
                                if (ValueTypeConverters.TryGetValue(arg.Type.Name, out string converter))
                                {
                                    sb.Append($"{converter}({arg.Name})");
                                }
                                else
                                {
                                    sb.Append($"static_cast<{arg.Type.Namespace}::{arg.Type.Name}>({arg.Name})");
                                }
                            }
                            else
                            {
                                sb.Append($"reinterpret_cast<{arg.Type.Namespace}::{arg.Type.Name}{arg.Type.Modifiers}>({arg.Name})");
                            }
                        }
                        else
                        {
                            sb.Append(arg.Name);
                        }
                    }
                    else
                    {
                        sb.Append($"reinterpret_cast<{arg.Type.Namespace}::{arg.Type.Name}{arg.Type.Modifiers}>({arg.Name})");
                    }
                }
                if (i != args.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Stands in for the current interface when a header declares only factories. Its name never reaches
        /// the output: no interface id block and no methods are emitted for it, and the factories carry the
        /// name of the interface they create.
        /// </summary>
        private IRTInterface GetCurrentOrPlaceholderClass()
        {
            if (RtFile.CurrentClass != null)
            {
                return RtFile.CurrentClass;
            }

            string ns = Options.LibraryInfo.Namespace?.ToString() ?? "daq";
            ITypeName type = new TypeName(RtFile.AttributeInfo, ns, "IBaseObject");
            return new RTInterface { Type = type, BaseType = type };
        }

        protected void GenerateHeader(string templatePath, string outputPath)
        {
            string methodTemplatePath = Path.Combine(Path.GetDirectoryName(templatePath), Path.GetFileNameWithoutExtension(templatePath) + ".method.template");

            SetVariables(GetCurrentOrPlaceholderClass(), templatePath);
            StringBuilder methods = GenerateMethodsHeader(methodTemplatePath);
            methods.TrimTrailingNewLines();

            Variables["Methods"] = methods.ToString();
            Variables["headers"] = GetIncludes(RtFile);
            Variables["typedefs"] = GetTypedefs();
            Variables["intfid_declaration"] = GetIntfIDDeclaration();

            GenerateOutput(GetCurrentOrPlaceholderClass(), templatePath, outputPath);
        }
        protected void GenerateSource(string templatePath, string outputPath)
        {
            string methodTemplatePath = Path.Combine(Path.GetDirectoryName(templatePath), Path.GetFileNameWithoutExtension(templatePath) + ".method.template");

            SetVariables(GetCurrentOrPlaceholderClass(), templatePath);
            StringBuilder methods = GenerateMethodsSource(methodTemplatePath);
            methods.TrimTrailingNewLines();

            Variables["Methods"] = methods.ToString();
            Variables["headers"] = GetIncludes(RtFile);
            Variables["intfid_definition"] = GetIntfIDDefinition();

            GenerateOutput(GetCurrentOrPlaceholderClass(), templatePath, outputPath);
        }

        protected void GenerateOutput(IRTInterface rtClass, string templatePath, string outputPath)
        {
            StreamReader template = null;
            try
            {
                FileInfo info = new FileInfo(templatePath);
                FileInfo outputInfo = new FileInfo(outputPath);
                template = new StreamReader(info.Open(FileMode.Open, FileAccess.Read, FileShare.Read));

                using (StreamWriter output = new StreamWriter(outputInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    while (!template.EndOfStream)
                    {
                        string templateLine = template.ReadLine();

                        if (string.IsNullOrEmpty(templateLine))
                        {
                            output.WriteLine(templateLine);
                            continue;
                        }

                        string outputLine = ReplacementRegex.Replace(templateLine, m =>
                        {
                            string variable = m.Groups[1].Value;

                            var str = GetGlobalVariable(rtClass, templatePath, variable);
                            return str;
                        });
                        output.WriteLine(outputLine);
                    }
                }
                template.Close();
            }
            catch (Exception)
            {
                template?.Dispose();
                throw;
            }
        }

        protected string ReplaceVariable<T>(string variable, GeneratorType generatorType, IRTInterface iface, T methodOrFactory, string templatePath)
        {
            IMethod method = methodOrFactory as IMethod;
            IRTFactory factory = methodOrFactory as IRTFactory;
            IOverload overload = method != null ? method.Overloads[0] : factory.ToOverload();

            //TODO: merge these two branches
            if (generatorType == GeneratorType.Header)
            {
                switch (variable)
                {
                    case "Prefix":
                        return Prefix;
                    case "Name":
                        return method != null ? method.Name : factory.Name ?? "";
                    case "ReturnType":
                        return method != null ? GetCTypeName(method.ReturnType) : MapValueTypeName("ErrCode");
                    case "ErrCodeType":
                        return MapValueTypeName("ErrCode");
                    case "NonInterfaceType":
                        string typeName = method != null ? iface.Type.NonInterfaceName : factory.InterfaceName ?? "";
                        if (factory != null && !String.IsNullOrEmpty(typeName))
                        {
                            typeName = typeName.Remove(0, 1);
                        }
                        return typeName;
                    case "Arguments":
                        return ArgumentsToString(iface, methodOrFactory, ArgsListType.MethodDeclaration);
                    default:
                        LogIgnoredVariable(variable, templatePath);
                        return string.Empty;
                }
            }
            else if (generatorType == GeneratorType.Source)
            {
                switch (variable)
                {
                    case "Prefix":
                        return Prefix;
                    case "Name":
                        return method != null ? method.Name : factory.Name ?? "";
                    case "ReturnType":
                        return method != null ? GetCTypeName(method.ReturnType) : MapValueTypeName("ErrCode");
                    case "ErrCodeType":
                        return MapValueTypeName("ErrCode");
                    case "NonInterfaceType":
                        string typeName = method != null ? iface.Type.NonInterfaceName : factory.InterfaceName ?? "";
                        if (factory != null && !String.IsNullOrEmpty(typeName))
                        {
                            typeName = typeName.Remove(0, 1);
                        }
                        return typeName;
                    case "ArgTypeFull":
                        //a factory can create an interface other than the one the file declares
                        return factory != null && !String.IsNullOrEmpty(factory.InterfaceName)
                                   ? $"{GetNamespace(iface)}::{factory.InterfaceName}"
                                   : iface.Type.FullName();
                    case "FactoryName":
                        return GetNamespace(iface) + "::" + overload.Method.Name;
                    case "Arguments":
                        return ArgumentsToString(iface, methodOrFactory, ArgsListType.MethodDeclaration);
                    case "ArgumentsForwarding":
                        return ArgumentsToString(iface, methodOrFactory, ArgsListType.ArgsForwarding);
                    default:
                        LogIgnoredVariable(variable, templatePath);
                        return string.Empty;
                }
            }
            return String.Empty;
        }

        protected StringBuilder GenerateMethodsHeader(string templatePath)
        {
            StringBuilder methods = new StringBuilder();
            if (!File.Exists(templatePath))
            {
                Log.Warning($"Method template {templatePath} does not exist");
                return null;
            }

            string[] methodTemplate = File.ReadAllLines(templatePath);

            string methodDeclarationTemplate = methodTemplate[0];
            string factoryDeclarationTemplate = methodTemplate[1];

            foreach (IRTInterface rtClass in RtFile.Classes)
            {
                foreach (IMethod method in rtClass.Methods)
                {
                    string template = methodDeclarationTemplate;

                    if (!HandleMemberMethod(rtClass, method))
                    {
                        continue;
                    }

                    string generatedMethod = ReplacementRegex.Replace(template, m =>
                    {
                        string variable = m.Groups[1].Value;
                        return ReplaceVariable(variable, GeneratorType.Header, rtClass, method, templatePath);
                    });

                    methods.AppendLine(base.Indentation + generatedMethod);
                }
            }

            IRTInterface factoryClass = GetCurrentOrPlaceholderClass();
            foreach (IRTFactory factory in RtFile.Factories)
            {
                IOverload factoryMethod = factory.ToOverload();
                string generatedFactory = ReplacementRegex.Replace(factoryDeclarationTemplate, m =>
                {
                    string variable = m.Groups[1].Value;
                    return ReplaceVariable(variable, GeneratorType.Header, factoryClass, factory, templatePath);
                });

                methods.AppendLine(base.Indentation + generatedFactory);
            }

            return methods;
        }

        protected StringBuilder GenerateMethodsSource(string templatePath)
        {
            StringBuilder methods = new StringBuilder();
            if (!File.Exists(templatePath))
            {
                Log.Warning($"Method template {templatePath} does not exist");
                return null;
            }

            string[] methodTemplate = File.ReadAllLines(templatePath);

            string methodDefinitionTemplate = methodTemplate[0];
            string factoryDefinitionTemplate = methodTemplate[1];
            string implMethodTemplate = methodTemplate[2];
            string pointerDeclarationTemplate = methodTemplate[3];
            string factoryCallTemplate = methodTemplate[4];
            string pointerCastTemplate = methodTemplate[5];
            string returnZeroTemplate = methodTemplate[6];
            string returnErrorTemplate = methodTemplate[7];

            foreach (IRTInterface rtClass in RtFile.Classes)
            {
                foreach (IMethod method in rtClass.Methods)
                {
                    if (!HandleMemberMethod(rtClass, method))
                    {
                        continue;
                    }

                    string generatedMethod = ReplacementRegex.Replace(methodDefinitionTemplate, m =>
                    {
                        string variable = m.Groups[1].Value;

                        string customVariable = GetMethodVariable(method, variable);
                        if (customVariable != null)
                        {
                            return customVariable;
                        }

                        return ReplaceVariable(variable, GeneratorType.Source, rtClass, method, templatePath);
                    });

                    string generatedMethodImpl = ReplacementRegex.Replace(implMethodTemplate, m =>
                    {
                        string variable = m.Groups[1].Value;
                        string customVariable = GetMethodVariable(method, variable);
                        if (customVariable != null)
                        {
                            return customVariable;
                        }
                        return ReplaceVariable(variable, GeneratorType.Source, rtClass, method, templatePath);
                    });

                    methods.AppendLine(generatedMethod);
                    methods.AppendLine("{");
                    methods.AppendLine(base.Indentation + generatedMethodImpl);
                    methods.AppendLine("}");
                    methods.AppendLine();
                }
            }

            IRTInterface factoryClass = GetCurrentOrPlaceholderClass();
            foreach (IRTFactory factory in RtFile.Factories)
            {
                IOverload factoryMethod = factory.ToOverload();

                string generatedFactory = ReplacementRegex.Replace(factoryDefinitionTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, factoryClass, factory, templatePath)
                );

                string objectPointer = ReplacementRegex.Replace(pointerDeclarationTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, factoryClass, factory, templatePath)
                );

                string factoryCall = ReplacementRegex.Replace(factoryCallTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, factoryClass, factory, templatePath)
                );

                string objectPointerCast = ReplacementRegex.Replace(pointerCastTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, factoryClass, factory, templatePath)
                );

                methods.AppendLine(generatedFactory);
                methods.AppendLine("{");
                methods.AppendLine(base.Indentation + objectPointer);
                methods.AppendLine(base.Indentation + factoryCall);
                methods.AppendLine(base.Indentation + objectPointerCast);
                methods.AppendLine(base.Indentation + returnErrorTemplate);
                methods.AppendLine("}");
                methods.AppendLine();
            }
            return methods;
        }
    }
}

