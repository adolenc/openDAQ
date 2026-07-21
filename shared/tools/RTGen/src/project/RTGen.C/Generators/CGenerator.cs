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

        protected static ISet<string> ForbiddenTypes => new HashSet<string>
        {
            "ComplexFloat64",
            "SourceLocation"
        };

        // Factories whose C++ symbol is not universally available (platform-specific /
        // optional); their C wrappers are commented out, matching the curated bindings.
        protected static ISet<string> ForbiddenFactories => new HashSet<string>
        {
            "createMiMallocAllocator",
            // Declared on IInstance but constructs an IDevice (mismatched out-parameter type
            // the mechanical factory wrapper cannot express); left out of the curated bindings.
            "createClient"
        };

        // Methods commented out of the bindings: getInterfaceIds returns an IntfID** array
        // that has no clean C ownership marshalling (matches the curated bindings).
        protected static ISet<string> ForbiddenMethods => new HashSet<string>
        {
            "getInterfaceIds"
        };

        // Function-pointer callback typedefs that need a reinterpret_cast when forwarded.
        protected static ISet<string> CallbackTypes => new HashSet<string>
        {
            "FuncCall", "ProcCall", "EventCall"
        };

        // Factories declared behind #ifdef _WIN32 in the core headers (the C++ parser drops
        // preprocessor conditionals); their C wrappers must be guarded the same way so the
        // non-Windows build does not reference a symbol that only exists on Windows.
        protected static ISet<string> WindowsOnlyFactories => new HashSet<string>
        {
            "createWinDebugLoggerSink"
        };

        protected static readonly String Prefix = "daq";
        protected static readonly String PrefixUpper = "DAQ_";

        // Name-collision renames: these interfaces share their C name with a value type
        // declared in ccommon.h (IFloat vs the daqFloat double alias, ICoreType vs the
        // daqCoreType enum), so their C-facing symbols get an "Object" suffix. Matched on
        // the full interface name (with the leading I) so the colliding value types, whose
        // names have no I, are left untouched.
        protected static string CNonInterfaceName(ITypeName type)
        {
            switch (type.Name)
            {
                case "IFloat":    return "FloatObject";
                case "ICoreType": return "CoreTypeObject";
                default:          return type.NonInterfaceName;
            }
        }

        // Same collision rename for factories, which carry the constructed interface name as
        // a plain string; strips the leading I for the non-colliding common case.
        protected static string CNonInterfaceNameFromInterface(string interfaceName)
        {
            if (string.IsNullOrEmpty(interfaceName))
            {
                return "";
            }
            switch (interfaceName)
            {
                case "IFloat":    return "FloatObject";
                case "ICoreType": return "CoreTypeObject";
                default:          return interfaceName.Remove(0, 1);
            }
        }

        protected GeneratorType _generatorType = GeneratorType.Header;
        protected ISet<string> _typesToDeclare = new HashSet<string>();
        protected ISet<string> _methodNamesToCommentOut = new HashSet<string>();

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

        protected override string GetMethodArgumentVariable(IArgument arg, IOverload overload, string variable)
        {
            if (variable == "ArgTypeNameNonInterface")
            {
                return Prefix + arg.Type.NonInterfaceName;
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
                // The opendaq.h umbrella does not pull in every interface (private /
                // internal / newer ones); include the interface's own core C++ header so
                // its daq::I<Name> type is always declared for the reinterpret_casts below.
                string coreHeader = CoreHeaderPath(lib, file);
                if (coreHeader != null)
                {
                    sb.AppendLine($"#include <{coreHeader}>");
                }
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
            sb.AppendLine(base.Indentation + $"EXPORTED extern const {Prefix}IntfID {PrefixUpper}{CNonInterfaceName(RtFile.CurrentClass.Type).ToLowerSnakeCase().ToUpper()}_INTF_ID;");
            sb.AppendLine(base.Indentation + $"void EXPORTED {Prefix}{CNonInterfaceName(RtFile.CurrentClass.Type)}_getInterfaceId({Prefix}IntfID* intfId);");
            return sb.ToString();
        }

        protected string GetIntfIDDefinition()
        {
            StringBuilder sb = new StringBuilder();
            string ns = RtFile.CurrentClass.Type.Namespace.ToString();
            string iface = RtFile.CurrentClass.Type.Name;

            sb.Append($"const {Prefix}IntfID {PrefixUpper}{CNonInterfaceName(RtFile.CurrentClass.Type).ToLowerSnakeCase().ToUpper()}_INTF_ID = ");
            sb.AppendLine($"{{ {ns}::{iface}::Id.Data1, {ns}::{iface}::Id.Data2, {ns}::{iface}::Id.Data3, {ns}::{iface}::Id.Data4_UInt64 }};");
            sb.AppendLine("");
            sb.AppendLine($"void {Prefix}{CNonInterfaceName(RtFile.CurrentClass.Type)}_getInterfaceId({Prefix}IntfID* intfId)");
            sb.AppendLine("{");
            sb.AppendLine(base.Indentation + $"*intfId = {PrefixUpper}{CNonInterfaceName(RtFile.CurrentClass.Type).ToLowerSnakeCase().ToUpper()}_INTF_ID;");
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

        // Maps the C library directory to the core C++ include for the same interface:
        // ccoretypes -> <coretypes/x.h>, ccoreobjects -> <coreobjects/x.h>,
        // copendaq/<sub> -> <opendaq/x.h> (the core opendaq headers are flat under opendaq/).
        protected static string CoreHeaderPath(string lib, string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return null;
            }
            if (lib == "ccoretypes")
            {
                return "coretypes/" + file + ".h";
            }
            if (lib == "ccoreobjects")
            {
                return "coreobjects/" + file + ".h";
            }
            if (lib != null && lib.StartsWith("copendaq"))
            {
                return "opendaq/" + file + ".h";
            }
            return null;
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

                //TODO: should be removed later
                //as some parts of the bindings are not yet implemented
                //we need to comment out the methods that use non implemented types
                if (ForbiddenTypes.Contains(arg.Type.NonInterfaceName))
                {
                    _methodNamesToCommentOut.Add(overload.Method.Name);
                }
                else if (!arg.Type.Flags.IsValueType)
                {
                    //filling types for typedefs
                    if (arg.Type.Name != "void") _typesToDeclare.Add(CNonInterfaceName(arg.Type));
                }

                if (listType == ArgsListType.MethodDeclaration)
                {
                    sb.Append($"{(arg.Type.Name != "void" ? Prefix : "")}{CNonInterfaceName(arg.Type)}{arg.Type.Modifiers} {arg.Name}");
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
                            // Callback typedefs (FuncCall/ProcCall/EventCall) are function
                            // pointers whose C and C++ forms differ only in opaque pointer
                            // types, so they need a reinterpret_cast, not a static_cast, even
                            // though they carry no pointer modifier.
                            if (String.IsNullOrEmpty(arg.Type.Modifiers) && !CallbackTypes.Contains(arg.Type.Name))
                            {
                                sb.Append($"static_cast<{arg.Type.Namespace}::{arg.Type.Name}>({arg.Name})");
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
                        {
                            // Full C return type. void-returning methods keep the bare C
                            // "void"; everything else (ErrCode, and factories) is daq-prefixed.
                            string rt = method != null ? method.ReturnType.NonInterfaceName : "ErrCode";
                            return rt == "void" ? "void" : Prefix + rt;
                        }
                    case "NonInterfaceType":
                        return method != null
                            ? CNonInterfaceName(iface.Type)
                            : CNonInterfaceNameFromInterface(factory.InterfaceName);
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
                        {
                            // Full C return type. void-returning methods keep the bare C
                            // "void"; everything else (ErrCode, and factories) is daq-prefixed.
                            string rt = method != null ? method.ReturnType.NonInterfaceName : "ErrCode";
                            return rt == "void" ? "void" : Prefix + rt;
                        }
                    case "NonInterfaceType":
                        return method != null
                            ? CNonInterfaceName(iface.Type)
                            : CNonInterfaceNameFromInterface(factory.InterfaceName);
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

                bool isCommentedOut = _methodNamesToCommentOut.Contains(method.Name)
                    || ForbiddenMethods.Contains(method.Name);
                if (isCommentedOut) methods.AppendLine("/*");
                methods.AppendLine(base.Indentation + generatedMethod);
                if (isCommentedOut) methods.AppendLine("*/");
            }

            foreach (IRTFactory factory in RtFile.Factories)
            {
                IOverload factoryMethod = factory.ToOverload();
                string generatedFactory = ReplacementRegex.Replace(factoryDeclarationTemplate, m =>
                {
                    string variable = m.Groups[1].Value;
                    return ReplaceVariable(variable, GeneratorType.Header, rtClass, factory, templatePath);
                });

                bool isCommentedOut = _methodNamesToCommentOut.Contains(factory.Name)
                    || ForbiddenFactories.Contains(factory.Name);
                bool winOnly = WindowsOnlyFactories.Contains(factory.Name);
                if (winOnly) methods.AppendLine("#ifdef _WIN32");
                if (isCommentedOut) methods.AppendLine("/*");
                methods.AppendLine(base.Indentation + generatedFactory);
                if (isCommentedOut) methods.AppendLine("*/");
                if (winOnly) methods.AppendLine("#endif");
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

                bool isCommentedOut = _methodNamesToCommentOut.Contains(method.Name)
                    || ForbiddenMethods.Contains(method.Name);

                if (isCommentedOut) methods.AppendLine("/*");
                methods.AppendLine(generatedMethod);
                methods.AppendLine("{");
                methods.AppendLine(base.Indentation + generatedMethodImpl);
                methods.AppendLine("}");
                if (isCommentedOut) methods.AppendLine("*/");
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

                bool isCommentedOut = _methodNamesToCommentOut.Contains(factory.Name)
                    || ForbiddenFactories.Contains(factory.Name);
                bool winOnly = WindowsOnlyFactories.Contains(factory.Name);

                if (winOnly) methods.AppendLine("#ifdef _WIN32");
                if (isCommentedOut) methods.AppendLine("/*");
                methods.AppendLine(generatedFactory);
                methods.AppendLine("{");
                methods.AppendLine(base.Indentation + objectPointer);
                methods.AppendLine(base.Indentation + factoryCall);
                methods.AppendLine(base.Indentation + objectPointerCast);
                methods.AppendLine(base.Indentation + returnErrorTemplate);
                methods.AppendLine("}");
                if (isCommentedOut) methods.AppendLine("*/");
                if (winOnly) methods.AppendLine("#endif");
                methods.AppendLine();
            }
            return methods;
        }
    }
}

