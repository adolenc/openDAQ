using RTGen.Exceptions;
using RTGen.Generation;
using RTGen.Interfaces;
using RTGen.Types;
using RTGen.Util;
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
        protected static readonly IDictionary<string, string> ValueTypeConverters = new Dictionary<string, string>
        {
            { "ComplexFloat64", "copendaq::utils::toDaqComplexFloat64" }
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
                sb.AppendLine();
                sb.AppendLine("#include <copendaq_private.h>");
            }
            return sb.ToString();
        }

        public override void GenerateFile(string templatePath)
        {
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
            sb.AppendLine(base.Indentation + $"EXPORTED extern const {Prefix}IntfID {PrefixUpper}{RtFile.CurrentClass.Type.NonInterfaceName.ToLowerSnakeCase().ToUpper()}_INTF_ID;");
            sb.AppendLine(base.Indentation + $"void EXPORTED {Prefix}{RtFile.CurrentClass.Type.NonInterfaceName}_getInterfaceId({Prefix}IntfID* intfId);");
            return sb.ToString();
        }

        protected string GetIntfIDDefinition()
        {
            StringBuilder sb = new StringBuilder();
            string ns = RtFile.CurrentClass.Type.Namespace.ToString();
            string iface = RtFile.CurrentClass.Type.Name;

            sb.Append($"const {Prefix}IntfID {PrefixUpper}{RtFile.CurrentClass.Type.NonInterfaceName.ToLowerSnakeCase().ToUpper()}_INTF_ID = ");
            sb.AppendLine($"{{ {ns}::{iface}::Id.Data1, {ns}::{iface}::Id.Data2, {ns}::{iface}::Id.Data3, {ns}::{iface}::Id.Data4_UInt64 }};");
            sb.AppendLine("");
            sb.AppendLine($"void {Prefix}{RtFile.CurrentClass.Type.NonInterfaceName}_getInterfaceId({Prefix}IntfID* intfId)");
            sb.AppendLine("{");
            sb.AppendLine(base.Indentation + $"*intfId = {PrefixUpper}{RtFile.CurrentClass.Type.NonInterfaceName.ToLowerSnakeCase().ToUpper()}_INTF_ID;");
            sb.AppendLine("}");
            return sb.ToString();
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
            return String.IsNullOrWhiteSpace(overridenExtension) ? GetOutputFilePath() : Path.ChangeExtension(GetOutputFilePath(), overridenExtension);
        }

        protected string GetHeaderTemplatePath(string templatePath)
        {
            return Path.Combine(Path.GetDirectoryName(templatePath), "c.header.template");
        }

        protected string ArgumentListToString(IEnumerable<IArgument> args, string separator)
        {
            return String.Join(separator, args.Select(arg => arg.Name));
        }

        protected string ArgumentsToString<T>(T methodOrFactory, ArgsListType listType)
        {
            IRTFactory factory = methodOrFactory as IRTFactory;
            IMethod method = methodOrFactory as IMethod;
            IOverload overload = method != null ? method.Overloads[0] : factory.ToOverload();

            IList<IArgument> args = overload.Arguments.ToList();

            //adding self pointer to the method declaration
            if (listType == ArgsListType.MethodDeclaration && method != null)
            {
                IArgument self = new Argument(RtFile.CurrentClass.Type, "self");
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
                        if ((!arg.Type.Flags.IsCoreType && arg.Type.Name != "void" && arg.Type.Name != "IntfID") || arg.Type.Name == "CoreType")
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
                        else if (arg.Type.Name == "IntfID")
                        {
                            sb.Append($"copendaq::utils::toDaqIntfId({arg.Name})");
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

        protected void GenerateHeader(string templatePath, string outputPath)
        {
            string methodTemplatePath = Path.Combine(Path.GetDirectoryName(templatePath), Path.GetFileNameWithoutExtension(templatePath) + ".method.template");

            SetVariables(RtFile.CurrentClass, templatePath);
            StringBuilder methods = GenerateMethodsHeader(methodTemplatePath);
            methods.TrimTrailingNewLines();

            Variables["Methods"] = methods.ToString();
            Variables["headers"] = GetIncludes(RtFile);
            Variables["typedefs"] = GetTypedefs();
            Variables["intfid_declaration"] = GetIntfIDDeclaration();

            GenerateOutput(RtFile.CurrentClass, templatePath, outputPath);
        }
        protected void GenerateSource(string templatePath, string outputPath)
        {
            string methodTemplatePath = Path.Combine(Path.GetDirectoryName(templatePath), Path.GetFileNameWithoutExtension(templatePath) + ".method.template");

            SetVariables(RtFile.CurrentClass, templatePath);
            StringBuilder methods = GenerateMethodsSource(methodTemplatePath);
            methods.TrimTrailingNewLines();

            Variables["Methods"] = methods.ToString();
            Variables["headers"] = GetIncludes(RtFile);
            Variables["intfid_definition"] = GetIntfIDDefinition();

            GenerateOutput(RtFile.CurrentClass, templatePath, outputPath);
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
                        return ArgumentsToString(methodOrFactory, ArgsListType.MethodDeclaration);
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
                        return iface.Type.FullName();
                    case "FactoryName":
                        return iface.Type.Namespace.ToString() + "::" + overload.Method.Name;
                    case "Arguments":
                        return ArgumentsToString(methodOrFactory, ArgsListType.MethodDeclaration);
                    case "ArgumentsForwarding":
                        return ArgumentsToString(methodOrFactory, ArgsListType.ArgsForwarding);
                    default:
                        LogIgnoredVariable(variable, templatePath);
                        return string.Empty;
                }
            }
            return String.Empty;
        }

        protected StringBuilder GenerateMethodsHeader(string templatePath)
        {
            IRTInterface rtClass = RtFile.CurrentClass;
            StringBuilder methods = new StringBuilder();
            if (!File.Exists(templatePath))
            {
                Log.Warning($"Method template {templatePath} does not exist");
                return null;
            }

            string[] methodTemplate = File.ReadAllLines(templatePath);

            string methodDeclarationTemplate = methodTemplate[0];
            string factoryDeclarationTemplate = methodTemplate[1];

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

            foreach (IRTFactory factory in RtFile.Factories)
            {
                IOverload factoryMethod = factory.ToOverload();
                string generatedFactory = ReplacementRegex.Replace(factoryDeclarationTemplate, m =>
                {
                    string variable = m.Groups[1].Value;
                    return ReplaceVariable(variable, GeneratorType.Header, rtClass, factory, templatePath);
                });

                methods.AppendLine(base.Indentation + generatedFactory);
            }

            return methods;
        }

        protected StringBuilder GenerateMethodsSource(string templatePath)
        {
            IRTInterface rtClass = RtFile.CurrentClass;
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

            foreach (IRTFactory factory in RtFile.Factories)
            {
                IOverload factoryMethod = factory.ToOverload();

                string generatedFactory = ReplacementRegex.Replace(factoryDefinitionTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, rtClass, factory, templatePath)
                );

                string objectPointer = ReplacementRegex.Replace(pointerDeclarationTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, rtClass, factory, templatePath)
                );

                string factoryCall = ReplacementRegex.Replace(factoryCallTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, rtClass, factory, templatePath)
                );

                string objectPointerCast = ReplacementRegex.Replace(pointerCastTemplate, m =>
                    ReplaceVariable(m.Groups[1].Value, GeneratorType.Source, rtClass, factory, templatePath)
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

