using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RTGen.Cpp;
using RTGen.Interfaces;
using RTGen.Shared;
using RTGen.Types;
using RTGen.Util;

namespace RTGen.Json.Generators
{
    /// <summary>Generates the single merged API description (invoked as
    /// `rtgen --language=json --config`): discovers every core header that declares
    /// interfaces or class factories, parses each with the C++ parser in-process
    /// (rtgen's CLI handles one source file per invocation, which is not a parser
    /// limitation — <see cref="CppParser"/> is stateless per parse), and links the
    /// results: resolves inheritance chains into absolute vtable slots, verifies
    /// interface IDs against the UUIDv5 scheme, scans the preprocessor-only error-code
    /// headers and enum-only headers, and nests every factory under the interface it
    /// constructs. Headers the grammar rejects verbatim are re-parsed from a sanitized
    /// copy (see <see cref="HeaderSanitizer"/>).
    ///
    /// Options: --source points at the core sources root,
    /// --outputDir/--output/--extension name the merged output file.</summary>
    class JsonConfigGenerator : IConfigGenerator
    {
        private const string DefaultNamespace = "daq";

        private static readonly Regex ErrTypeRegex = new Regex(
            @"#define\s+OPENDAQ_ERRTYPE_(\w+)\s+0x([0-9A-Fa-f]+)u?");
        private static readonly Regex ErrCodeRegex = new Regex(
            @"#define\s+(OPENDAQ_ERR_\w+)\s+OPENDAQ_ERROR_CODE\(\s*OPENDAQ_ERRTYPE_(\w+)\s*,\s*0x([0-9A-Fa-f]+)u?\s*\)");
        // Status codes that are not errors (OPENDAQ_SUCCESS, OPENDAQ_NO_MORE_ITEMS, ...) are spelled as a
        // full 32-bit literal, unlike the error codes, which are composed by OPENDAQ_ERROR_CODE.
        private static readonly Regex StatusRegex = new Regex(
            @"#define\s+(OPENDAQ_[A-Z_]+)\s+0x([0-9A-Fa-f]{8})u?\s*$", RegexOptions.Multiline);
        private static readonly Regex CommentRegex = new Regex(
            @"/\*.*?\*/|//[^\n]*", RegexOptions.Singleline);
        private static readonly Regex EnumRegex = new Regex(
            @"enum(?:\s+class)?\s+(\w+)\s*(?::\s*\w+)?\s*\{(.*?)\}\s*;", RegexOptions.Singleline);
        private IGeneratorOptions _options;

        public IVersionInfo Version => new VersionInfo
        {
            Major = 1,
            Minor = 0,
            Patch = 0
        };

        public IGeneratorOptions Options
        {
            get => _options;
            set
            {
                _options = value;
                if (string.IsNullOrEmpty(_options.GeneratedExtension))
                {
                    _options.GeneratedExtension = ".json";
                }
                if (string.IsNullOrEmpty(_options.Filename))
                {
                    _options.Filename = "opendaq";
                }
            }
        }

        public void GenerateConfigFile(string templatePath)
        {
            string outputDir = string.IsNullOrEmpty(Options.OutputDir) ? "." : Options.OutputDir;
            string outputPath = Path.Combine(outputDir, Options.Filename + Options.GeneratedExtension);
            string coreRoot = GetSourceOption();
            if (string.IsNullOrEmpty(coreRoot))
            {
                throw new RTGen.Exceptions.GeneratorException(
                    "--source=<core dir> is required (the directory to discover headers in).");
            }

            string summary;
            OrderedDict root = BuildApi(coreRoot, out summary);

            Directory.CreateDirectory(outputDir);
            File.WriteAllText(outputPath, JsonValue.Serialize(root, 0) + "\n", new UTF8Encoding(false));

            Log.Info($"{summary} -> {outputPath}");
        }

        /// <summary>Parses every core header below <paramref name="coreRoot"/> and links the result
        /// into the API description that is written out as opendaq.json. The C generator builds the
        /// same description to emit the parts of the C bindings that span the whole API rather than
        /// one header (the enums and the status codes).</summary>
        internal OrderedDict BuildApi(string coreRoot, out string summary)
        {
            var interfaces = new List<OrderedDict>();
            var interfacesByName = new Dictionary<string, OrderedDict>();
            var enums = new Dictionary<string, OrderedDict>();
            var factories = new Dictionary<string, OrderedDict>();
            var coveredSources = new HashSet<string>();

            Action<OrderedDict, string> addInterface = (iface, header) =>
            {
                string name = iface.GetString("name");
                OrderedDict existing;
                if (interfacesByName.TryGetValue(name, out existing))
                {
                    string kept = existing.GetString("header");
                    Log.Warning($"Duplicate interface {name} from {header} ignored (kept {kept}).");
                    return;
                }
                interfaces.Add(iface);
                interfacesByName.Add(name, iface);
            };

            var parser = new CppParser();
            Dictionary<string, long> macros = ScanNumericMacros(coreRoot);
            var parserOptions = (IParserOptions)Options;
            var functions = new List<OrderedDict>();
            int parsedCount = 0;
            int failedCount = 0;

            foreach (KeyValuePair<string, string> moduleHeader in DiscoverHeaders(coreRoot))
            {
                string header = RelativeHeader(moduleHeader.Key, coreRoot);
                string library = moduleHeader.Value;

                OrderedDict data;
                List<ExportedFunction> exported;
                try
                {
                    parserOptions.LibraryInfo.Name = library;
                    exported = new List<ExportedFunction>();
                    IRTFile rtFile = HeaderSanitizer.Parse(parser, parserOptions, moduleHeader.Key, exported);
                    rtFile.SourceFileName = Path.GetFileName(moduleHeader.Key);
                    data = new JsonGenerator { Options = Options, RtFile = rtFile }.BuildModel(header);
                    parsedCount++;
                }
                catch (Exception e)
                {
                    failedCount++;
                    Log.Warning($"FAILED {header}: {e.Message}");
                    continue;
                }

                coveredSources.Add(Path.GetFileName(header));

                // The C bindings expose a freestanding function as a member of the interface its
                // header declares (the last one, should there be several), and leave out one whose
                // signature C cannot express.
                string owner = DictItems(data.GetList("interfaces"))
                    .Select(i => i.GetString("name"))
                    .LastOrDefault(name => name != HeaderSanitizer.PlaceholderInterface);
                foreach (ExportedFunction function in exported)
                {
                    if (owner != null && IsGeneratedForC(header) && CBindingRules.IsBindable(function))
                    {
                        functions.Add(BuildExportedFunction(function, owner, header, library));
                    }
                }

                IDictionary<string, string> factoryConditions =
                    PreprocessorConditions.OfFactories(File.ReadAllText(moduleHeader.Key));

                foreach (OrderedDict iface in DictItems(data.GetList("interfaces")))
                {
                    if (iface.GetString("name") == HeaderSanitizer.PlaceholderInterface)
                    {
                        continue;
                    }
                    iface.Set("library", library);
                    iface.Set("header", header);
                    addInterface(iface, header);
                }

                foreach (OrderedDict enumeration in DictItems(data.GetList("enums")))
                {
                    enumeration.Set("header", header);
                    ResolveEnumValues(enumeration);
                    ApplyMacroEnumValues(enumeration, moduleHeader.Key, macros);
                    if (!enums.ContainsKey(enumeration.GetString("name")))
                    {
                        enums.Add(enumeration.GetString("name"), enumeration);
                    }
                }

                foreach (OrderedDict factory in DictItems(data.GetList("factories")))
                {
                    factory.Set("library", library);
                    factory.Set("header", header);
                    string factoryName = factory.GetString("name");
                    if (factoryConditions.ContainsKey(factoryName))
                    {
                        // Only exists in some builds of the core, so the C bindings leave it out.
                        continue;
                    }
                    if (factories.ContainsKey(factoryName))
                    {
                        Log.Warning($"Duplicate factory {factoryName} from {header} ignored.");
                        continue;
                    }
                    factories.Add(factoryName, factory);
                }
            }

            if (parsedCount == 0)
            {
                throw new RTGen.Exceptions.GeneratorException(
                    $"No parseable interface headers found under \"{coreRoot}\".");
            }

            foreach (OrderedDict factory in ScanPlainFactories(coreRoot))
            {
                string factoryName = factory.GetString("name");
                if (!factories.ContainsKey(factoryName))
                {
                    factories.Add(factoryName, factory);
                }
            }

            foreach (CBindingRules.HandWrittenFactory handWritten in CBindingRules.HandWrittenFactories)
            {
                factories.Add(handWritten.Name, BuildHandWrittenFactory(handWritten));
            }

            VerifyInterfaceIds(interfacesByName);
            AssignVtableSlots(interfaces, interfacesByName);
            string callingConvention = HoistCallingConvention(interfaces);
            ApplyHandWrittenArguments(interfacesByName);
            NestMembers("factories", factories.Values, interfacesByName);
            NestMembers("functions", functions, interfacesByName);

            // The supplemental scan sweeps every header under the include roots, which
            // also finds internal enums (eval-value lexer/parser, sha1, type traits).
            // Keep only enums the API surface actually references; this reproduces
            // exactly the enum set the curated language bindings expose.
            HashSet<string> referenced = CollectReferencedTypeNames(interfaces);
            foreach (OrderedDict enumeration in ScanSupplementalEnums(coreRoot, coveredSources))
            {
                string name = enumeration.GetString("name");
                if (referenced.Contains(name) && !enums.ContainsKey(name))
                {
                    enums.Add(name, enumeration);
                }
            }

            // Everything so far describes the C++ API. What is written out describes the C bindings:
            // only what they contain, under the names it has there.
            string unboundReason;
            interfaces.RemoveAll(i => CBindingRules.IsUnbound(i.GetString("header"), out unboundReason));
            List<object> errorCodes = ScanErrorCodes(coreRoot);
            SpellInC(interfaces, enums.Values, errorCodes);

            var root = new OrderedDict
            {
                { "schema", "opendaq-bindings/1" },
                {
                    "abi", new OrderedDict
                    {
                        { "calling_convention", callingConvention == "stdcall"
                            ? "INTERFACE_FUNC: __stdcall on 32-bit Windows, platform default elsewhere"
                            : callingConvention },
                        { "intf_id_scheme", "RFC 4122 UUIDv5, DNS namespace, of the C++ interface name: "
                                            + "'IString.daq' for daqString" },
                        { "vtable", "single inheritance, declaration order; absolute_vtable_slot indexes the "
                                    + "whole vtable, relative_vtable_slot only the interface's own methods" }
                    }
                },
                { "types", ScanAbiTypes(coreRoot) },
                { "interfaces", interfaces.OrderBy(i => i.GetString("name"), StringComparer.Ordinal).ToList<object>() },
                { "enums", enums.Values.OrderBy(e => e.GetString("name"), StringComparer.Ordinal).ToList<object>() },
                { "error_codes", errorCodes }
            };

            Func<string, int> count = key => interfaces.Sum(i => i.GetList(key)?.Count ?? 0);
            summary = $"Parsed {parsedCount} headers ({failedCount} parse failures, expected: see README.md); "
                      + $"described {interfaces.Count} interfaces, {count("methods")} methods, "
                      + $"{count("factories")} factories, {count("functions")} functions, {enums.Count} enums, "
                      + $"{errorCodes.Count} status codes";
            return root;
        }

        // -------------------------------------------------------------------
        //  Linking
        // -------------------------------------------------------------------

        private static void VerifyInterfaceIds(Dictionary<string, OrderedDict> interfaces)
        {
            foreach (OrderedDict iface in interfaces.Values)
            {
                if (iface.GetString("name") == "IUnknown")
                {
                    // DEFINE_EXTERNAL_INTFID(UnknownGuid): the COM IUnknown IID, not a UUIDv5.
                    continue;
                }
                string ns = iface.GetString("namespace") ?? DefaultNamespace;
                string expected = Utility.InterfaceUuid($"{iface.GetString("name")}.{ns}").ToString();
                string actual = iface.GetString("id");
                if (actual == null)
                {
                    iface.Set("id", expected);
                }
                else if (actual != expected)
                {
                    Log.Warning($"Interface {iface.GetString("name")}: parsed ID {actual} "
                                + $"does not match UUIDv5 scheme ({expected}).");
                }
            }
        }

        private static void AssignVtableSlots(List<OrderedDict> interfaces,
                                              Dictionary<string, OrderedDict> interfacesByName)
        {
            Func<string, HashSet<string>, long?> slotCount = null;
            slotCount = (name, trail) =>
            {
                OrderedDict iface;
                if (!interfacesByName.TryGetValue(name, out iface) || !trail.Add(name))
                {
                    return null;
                }
                long own = iface.GetList("methods").Count;
                string baseName = iface.GetString("base");
                if (baseName == null)
                {
                    return own;
                }
                long? below = slotCount(baseName, trail);
                return below == null ? null : below + own;
            };

            foreach (OrderedDict iface in interfaces)
            {
                string baseName = iface.GetString("base");
                long? firstSlot = baseName == null ? 0 : slotCount(baseName, new HashSet<string>());
                if (firstSlot == null)
                {
                    Log.Warning($"Interface {iface.GetString("name")}: base "
                                + $"{baseName} not found, vtable slots not assigned.");
                    continue;
                }

                // The relative slot is the method's index among the interface's own
                // methods; the absolute slot adds the size of the inherited vtable.
                long relative = 0;
                foreach (OrderedDict method in DictItems(iface.GetList("methods")))
                {
                    method.Set("absolute_vtable_slot", firstSlot.Value + relative);
                    method.Set("relative_vtable_slot", relative);
                    relative++;
                }
            }
        }

        /// <summary>Moves every factory and every freestanding function into the interface it
        /// belongs to: a factory to the interface it constructs, a function to the interface whose
        /// header declares it. Provenance is kept on the member only when it differs from the
        /// interface's. A member whose interface is unknown cannot be named and is left out.</summary>
        private static void NestMembers(string key, IEnumerable<OrderedDict> members,
                                        Dictionary<string, OrderedDict> interfacesByName)
        {
            foreach (OrderedDict member in members.OrderBy(m => m.GetString("name"), StringComparer.Ordinal))
            {
                string interfaceName = member.GetString("interface");
                OrderedDict iface;
                if (!interfacesByName.TryGetValue(interfaceName, out iface))
                {
                    Log.Warning($"{member.GetString("name")}: interface {interfaceName} not found, left out.");
                    continue;
                }

                member.Remove("interface");
                if (member.GetString("header") == iface.GetString("header"))
                {
                    member.Remove("header");
                }
                if (member.GetString("library") == iface.GetString("library"))
                {
                    member.Remove("library");
                }

                List<object> list = iface.GetList(key);
                if (list == null)
                {
                    list = new List<object>();
                    iface.Add(key, list);
                }
                list.Add(member);
            }
        }

        /// <summary>All type names appearing in method/factory signatures (including
        /// generic arguments), used to limit the supplemental enum scan to enums the
        /// API surface actually uses.</summary>
        private static HashSet<string> CollectReferencedTypeNames(List<OrderedDict> interfaces)
        {
            var referenced = new HashSet<string>();

            Action<OrderedDict> walkType = null;
            walkType = type =>
            {
                if (type == null)
                {
                    return;
                }
                string name = type.GetString("name");
                if (name != null)
                {
                    referenced.Add(name);
                }
                foreach (OrderedDict generic in DictItems(type.GetList("generic_arguments")))
                {
                    walkType(generic);
                }
                // Container element types are plain names, not nested type objects.
                foreach (string element in new[] { type.GetString("key_type"), type.GetString("value_type"),
                                                   type.GetString("sender_type"), type.GetString("args_type") })
                {
                    if (element != null)
                    {
                        referenced.Add(element);
                    }
                }
            };

            Action<OrderedDict> walkSignature = member =>
            {
                walkType(member.GetDict("return_type"));
                foreach (OrderedDict arg in DictItems(member.GetList("arguments")))
                {
                    walkType(arg.GetDict("type"));
                }
            };

            foreach (OrderedDict iface in interfaces)
            {
                foreach (OrderedDict method in DictItems(iface.GetList("methods")))
                {
                    walkSignature(method);
                }
                foreach (OrderedDict factory in DictItems(iface.GetList("factories")))
                {
                    walkSignature(factory);
                }
            }
            return referenced;
        }

        /// <summary>Removes the per-method calling convention when it is uniform across the
        /// whole API (it always is: INTERFACE_FUNC) and returns the hoisted value.</summary>
        private static string HoistCallingConvention(List<OrderedDict> interfaces)
        {
            var conventions = new HashSet<string>();
            foreach (OrderedDict iface in interfaces)
            {
                foreach (OrderedDict method in DictItems(iface.GetList("methods")))
                {
                    conventions.Add(method.GetString("calling_convention") ?? "stdcall");
                }
            }

            if (conventions.Count > 1)
            {
                Log.Warning("Calling convention differs between methods ("
                            + string.Join(", ", conventions) + "); keeping per-method values.");
                return string.Join("|", conventions.OrderBy(c => c));
            }

            foreach (OrderedDict iface in interfaces)
            {
                foreach (OrderedDict method in DictItems(iface.GetList("methods")))
                {
                    method.Remove("calling_convention");
                }
            }
            return conventions.FirstOrDefault() ?? "stdcall";
        }

        private static OrderedDict BuildExportedFunction(ExportedFunction function, string owner,
                                                         string header, string library)
        {
            return new OrderedDict
            {
                { "name", CBindingRules.FunctionMemberName(function.Name) },
                { "return_type", CppTypeDesc(function.ReturnType) },
                { "interface", owner },
                {
                    "arguments", function.Arguments
                        .Select(a => (object) BuildDeclaredArgument(a.Key, a.Value))
                        .ToList()
                },
                { "library", library },
                { "header", header }
            };
        }

        private static OrderedDict CppTypeDesc(string declaration)
        {
            int pointers = declaration.Count(c => c == '*');
            string name = declaration.Replace("*", "").Replace("&", "").Trim();
            bool isConst = name.StartsWith("const ");
            if (isConst)
            {
                name = name.Substring("const ".Length).Trim();
            }
            if (name.StartsWith("daq::"))
            {
                name = name.Substring("daq::".Length);
            }
            var result = new OrderedDict { { "name", name } };
            if (pointers > 0)
            {
                result.Add("pointers", pointers);
            }
            if (isConst)
            {
                result.Add("is_const", true);
            }
            return result;
        }

        // -------------------------------------------------------------------
        //  Header discovery
        // -------------------------------------------------------------------

        /// <summary>The public include roots under the core sources directory, each with
        /// the library its headers belong to. corecontainers installs its headers into
        /// the coretypes include directory and is part of that library.</summary>
        private static IEnumerable<KeyValuePair<string, string>> ModuleRoots(string coreRoot)
        {
            var roots = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(
                    Path.Combine(coreRoot, "coretypes", "include", "coretypes"), "coretypes"),
                new KeyValuePair<string, string>(
                    Path.Combine(coreRoot, "corecontainers", "include", "coretypes"), "coretypes"),
                new KeyValuePair<string, string>(
                    Path.Combine(coreRoot, "coreobjects", "include", "coreobjects"), "coreobjects"),
            };
            string opendaqRoot = Path.Combine(coreRoot, "opendaq");
            if (Directory.Exists(opendaqRoot))
            {
                roots.AddRange(Directory.GetDirectories(opendaqRoot)
                    .OrderBy(dir => dir, StringComparer.Ordinal)
                    .Select(dir => new KeyValuePair<string, string>(
                        Path.Combine(dir, "include", "opendaq"), "opendaq")));
            }
            return roots.Where(root => Directory.Exists(root.Key));
        }

        private static IEnumerable<string> IncludeRoots(string coreRoot)
        {
            return ModuleRoots(coreRoot).Select(root => root.Key);
        }

        /// <summary>Headers that declare interfaces or class factories, paired with their
        /// library name. Everything else (smart pointers, impls, factory helpers) has no
        /// ABI declarations to parse.</summary>
        private static IEnumerable<KeyValuePair<string, string>> DiscoverHeaders(string coreRoot)
        {
            foreach (KeyValuePair<string, string> root in ModuleRoots(coreRoot))
            {
                foreach (string header in HeadersUnder(root.Key))
                {
                    // Match on define-stripped text so the factory macro machinery
                    // (factory.h) is not mistaken for declarations.
                    if (HeaderSanitizer.DeclaresBindings(File.ReadAllText(header)))
                    {
                        yield return new KeyValuePair<string, string>(header, root.Value);
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        //  Scans over C++ sources (headers rtgen cannot parse)
        // -------------------------------------------------------------------

        private static IEnumerable<string> HeadersUnder(string root)
        {
            return Directory.GetFiles(root, "*.h", SearchOption.AllDirectories)
                .OrderBy(file => file, StringComparer.Ordinal);
        }

        private List<object> ScanErrorCodes(string coreRoot)
        {
            var errorHeaders = new List<string>();
            foreach (string root in IncludeRoots(coreRoot))
            {
                errorHeaders.AddRange(HeadersUnder(root)
                    .Where(file => Path.GetFileNameWithoutExtension(file).Contains("errors")));
            }

            var families = new Dictionary<string, long>();
            foreach (string header in errorHeaders)
            {
                foreach (Match match in ErrTypeRegex.Matches(File.ReadAllText(header)))
                {
                    families[match.Groups[1].Value] = Convert.ToInt64(match.Groups[2].Value, 16);
                }
            }

            var codes = new List<object>();
            var seen = new HashSet<string>();
            foreach (string header in errorHeaders)
            {
                string text = File.ReadAllText(header);
                string relHeader = RelativeHeader(header, coreRoot);
                foreach (Match match in StatusRegex.Matches(text))
                {
                    if (seen.Add(match.Groups[1].Value))
                    {
                        codes.Add(new OrderedDict
                        {
                            { "name", match.Groups[1].Value },
                            { "code", Convert.ToInt64(match.Groups[2].Value, 16) },
                            { "header", relHeader }
                        });
                    }
                }
                foreach (Match match in ErrCodeRegex.Matches(text))
                {
                    string name = match.Groups[1].Value;
                    long family;
                    if (!seen.Add(name) || !families.TryGetValue(match.Groups[2].Value, out family))
                    {
                        continue;
                    }
                    codes.Add(new OrderedDict
                    {
                        { "name", name },
                        { "code", 0x80000000L | (family << 16) | Convert.ToInt64(match.Groups[3].Value, 16) },
                        { "header", relHeader }
                    });
                }
            }
            return codes.OrderBy(c => (long)((OrderedDict)c).Get("code")).ToList();
        }

        /// <summary>Enums declared in headers rtgen never parses successfully: files with
        /// enums but no interfaces (e.g. sample_type.h) are rejected with "no classes
        /// found", so they are scanned directly.</summary>
        private List<OrderedDict> ScanSupplementalEnums(string coreRoot, HashSet<string> coveredSources)
        {
            var result = new List<OrderedDict>();
            foreach (string root in IncludeRoots(coreRoot))
            {
                foreach (string header in HeadersUnder(root))
                {
                    string fileName = Path.GetFileName(header);
                    if (coveredSources.Contains(fileName)
                        || Path.GetFileNameWithoutExtension(header).EndsWith("_impl"))
                    {
                        continue;
                    }

                    string text = CommentRegex.Replace(File.ReadAllText(header), "");
                    foreach (Match match in EnumRegex.Matches(text))
                    {
                        var options = new List<object>();
                        foreach (string rawPart in match.Groups[2].Value.Split(','))
                        {
                            string part = rawPart.Trim();
                            if (part.Length == 0 || !char.IsLetter(part[0]) && part[0] != '_')
                            {
                                continue;
                            }
                            var option = new OrderedDict();
                            int assign = part.IndexOf('=');
                            if (assign >= 0)
                            {
                                option.Add("name", part.Substring(0, assign).Trim());
                                option.Add("value", part.Substring(assign + 1).Trim());
                            }
                            else
                            {
                                option.Add("name", part);
                            }
                            options.Add(option);
                        }
                        if (options.Count == 0)
                        {
                            continue;
                        }
                        var enumeration = new OrderedDict
                        {
                            { "name", match.Groups[1].Value },
                            { "options", options },
                            { "header", RelativeHeader(header, coreRoot) }
                        };
                        ResolveEnumValues(enumeration);
                        result.Add(enumeration);
                    }
                }
            }
            return result;
        }

        // -------------------------------------------------------------------
        //  C bindings
        // -------------------------------------------------------------------

        /// <summary>Whether the C generator emits bindings for the header (as opposed to leaving
        /// it out, or to its C counterpart being written by hand).</summary>
        private static bool IsGeneratedForC(string header)
        {
            string reason;
            return !CBindingRules.IsUnbound(header, out reason) && !CBindingRules.IsHandWritten(header, out reason);
        }

        /// <summary>Gives the hand written C functions whose declaration is not the mechanical
        /// translation of the C++ one the arguments the C header declares.</summary>
        private static void ApplyHandWrittenArguments(Dictionary<string, OrderedDict> interfacesByName)
        {
            foreach (KeyValuePair<string, KeyValuePair<string, string>[]> entry in CBindingRules.HandWrittenArguments)
            {
                string[] parts = entry.Key.Split(new[] { "::" }, StringSplitOptions.None);
                OrderedDict iface;
                OrderedDict method = interfacesByName.TryGetValue(parts[0], out iface)
                    ? DictItems(iface.GetList("methods")).FirstOrDefault(m => m.GetString("name") == parts[1])
                    : null;
                if (method == null)
                {
                    Log.Warning($"{entry.Key}: declared as hand written but not found in the core headers.");
                    continue;
                }
                method.Set("arguments", entry.Value
                    .Select(a => BuildDeclaredArgument(a.Key, a.Value))
                    .ToList<object>());
            }
        }

        /// <summary>Renames everything from its name in the C++ API to its name in the C bindings,
        /// by the rules the C generator emits them with (<see cref="CBindingRules"/>).</summary>
        private static void SpellInC(List<OrderedDict> interfaces, IEnumerable<OrderedDict> enums,
                                     List<object> errorCodes)
        {
            var enumNames = new HashSet<string>(enums.Select(e => e.GetString("name")));

            Action<OrderedDict> spellType = type =>
            {
                if (type == null)
                {
                    return;
                }
                type.Set("name", CBindingRules.TypeName(type.GetString("name"), enumNames));
                foreach (string element in new[] { "key_type", "value_type", "sender_type", "args_type" })
                {
                    if (type.GetString(element) != null)
                    {
                        type.Set(element, CBindingRules.TypeName(type.GetString(element), enumNames));
                    }
                }
            };

            foreach (OrderedDict iface in interfaces)
            {
                foreach (string key in new[] { "methods", "factories", "functions" })
                {
                    foreach (OrderedDict member in DictItems(iface.GetList(key)))
                    {
                        spellType(member.GetDict("return_type"));
                        foreach (OrderedDict argument in DictItems(member.GetList("arguments")))
                        {
                            spellType(argument.GetDict("type"));
                        }
                    }
                }
                iface.Set("name", CBindingRules.InterfaceTypeName(iface.GetString("name")));
                if (iface.GetString("base") != null)
                {
                    iface.Set("base", CBindingRules.InterfaceTypeName(iface.GetString("base")));
                }
            }

            foreach (OrderedDict enumeration in enums)
            {
                string name = enumeration.GetString("name");
                foreach (OrderedDict option in DictItems(enumeration.GetList("options")))
                {
                    option.Set("name", CBindingRules.EnumOptionName(name, option.GetString("name")));
                }
                enumeration.Set("name", CBindingRules.EnumTypeName(name));
            }

            foreach (OrderedDict code in errorCodes.OfType<OrderedDict>())
            {
                code.Set("name", CBindingRules.StatusName(code.GetString("name")));
            }
        }

        private static readonly Regex AbiStructRegex = new Regex(
            @"typedef\s+struct\s+\w+\s*\{([^}]*)\}\s*(\w+)\s*;", RegexOptions.Singleline);
        private static readonly Regex AbiCallbackRegex = new Regex(
            @"typedef\s+([\w\s\*]+?)\s*\(\s*\*\s*(\w+)\s*\)\s*\(([^)]*)\)\s*;", RegexOptions.Singleline);
        private static readonly Regex AbiAliasRegex = new Regex(
            @"typedef\s+(?!struct\b|enum\b)([\w\s\*]+?)\s*\b(\w+)\s*;", RegexOptions.Singleline);
        private static readonly Regex AbiPackRegex = new Regex(
            @"#\s*pragma\s+pack\s*\(\s*(?:push\s*,\s*(\d+)|(pop))\s*\)");

        /// <summary>The types of the C ABI: the aliases of the fundamental types, the structs passed
        /// by value and the callbacks. They are the one hand written part of the C API that every
        /// signature depends on, so they are read off the header that declares them
        /// (bindings/c/include/ccommon.h) rather than repeated here.</summary>
        private static List<object> ScanAbiTypes(string coreRoot)
        {
            var types = new List<object>();
            string header = Path.Combine(coreRoot, "..", "bindings", "c", "include", "ccommon.h");
            if (!File.Exists(header))
            {
                Log.Warning($"{header} not found: the description will not define the C ABI types.");
                return types;
            }

            string text = CommentRegex.Replace(File.ReadAllText(header), "");
            var found = new SortedDictionary<int, OrderedDict>();

            foreach (Match match in AbiStructRegex.Matches(text))
            {
                var fields = new List<object>();
                foreach (string declaration in match.Groups[1].Value.Split(';'))
                {
                    string field = string.Join(" ", declaration.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                    if (field.Length == 0)
                    {
                        continue;
                    }
                    int nameStart = field.LastIndexOfAny(new[] { ' ', '*' }) + 1;
                    fields.Add(new OrderedDict
                    {
                        { "name", field.Substring(nameStart) },
                        { "type", CTypeDesc(field.Substring(0, nameStart)) }
                    });
                }

                var structure = new OrderedDict
                {
                    { "name", match.Groups[2].Value },
                    { "kind", "struct" },
                    { "fields", fields }
                };
                // The innermost #pragma pack in effect where the struct is declared.
                var packs = new Stack<string>();
                foreach (Match pack in AbiPackRegex.Matches(text.Substring(0, match.Index)))
                {
                    if (pack.Groups[2].Success)
                    {
                        if (packs.Count > 0)
                        {
                            packs.Pop();
                        }
                    }
                    else
                    {
                        packs.Push(pack.Groups[1].Value);
                    }
                }
                if (packs.Count > 0)
                {
                    structure.Add("pack", long.Parse(packs.Peek()));
                }
                found[match.Index] = structure;
            }

            // Without the structs, so that their fields are not taken for typedefs.
            string flat = AbiStructRegex.Replace(text, m => new string(' ', m.Length));

            foreach (Match match in AbiCallbackRegex.Matches(flat))
            {
                found[match.Index] = new OrderedDict
                {
                    { "name", match.Groups[2].Value },
                    { "kind", "callback" },
                    { "return_type", CTypeDesc(match.Groups[1].Value) },
                    {
                        "arguments", match.Groups[3].Value.Split(',')
                            .Where(a => a.Trim().Length > 0 && a.Trim() != "void")
                            .Select(a => (object) new OrderedDict { { "type", CTypeDesc(a) } })
                            .ToList()
                    }
                };
            }

            foreach (Match match in AbiAliasRegex.Matches(AbiCallbackRegex.Replace(flat, m => new string(' ', m.Length))))
            {
                found[match.Index] = new OrderedDict
                {
                    { "name", match.Groups[2].Value },
                    { "kind", "alias" },
                    { "type", CTypeDesc(match.Groups[1].Value) }
                };
            }

            types.AddRange(found.Values);
            return types;
        }

        /// <summary>Describes a type as it is spelled in a C declaration.</summary>
        private static OrderedDict CTypeDesc(string declaration)
        {
            int pointers = declaration.Count(c => c == '*');
            List<string> words = declaration.Replace("*", " ")
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries).ToList();
            bool isConst = words.Remove("const");

            var result = new OrderedDict { { "name", string.Join(" ", words) } };
            if (pointers > 0)
            {
                result.Add("pointers", (long) pointers);
            }
            if (isConst)
            {
                result.Add("is_const", true);
            }
            return result;
        }

        /// <summary>Describes an argument from its C++ declaration, in the shape the parsed
        /// arguments have.</summary>
        private static OrderedDict BuildDeclaredArgument(string declaration, string name)
        {
            OrderedDict type = CppTypeDesc(declaration);
            bool isConst = type.Get("is_const") != null;
            type.Remove("is_const");
            if (declaration.Contains("&"))
            {
                type.Add("is_reference", true);
            }

            var argument = new OrderedDict
            {
                { "name", name },
                { "type", type }
            };
            if (isConst)
            {
                argument.Add("is_const", true);
            }
            return argument;
        }

        private static OrderedDict BuildHandWrittenFactory(CBindingRules.HandWrittenFactory factory)
        {
            var arguments = new List<object> { BuildDeclaredArgument(factory.Interface + "**", "obj") };
            arguments.AddRange(factory.Arguments.Select(a => (object) BuildDeclaredArgument(a.Key, a.Value)));
            return new OrderedDict
            {
                { "name", factory.Name },
                { "return_type", new OrderedDict { { "name", "ErrCode" } } },
                { "interface", factory.Interface },
                { "arguments", arguments },
                { "doc", factory.Doc },
                { "library", "copendaq" },
                { "header", "bindings/c/include/ccoretypes/factories.h" }
            };
        }

        private static readonly Regex PlainFactoryRegex = new Regex(
            @"\bErrCode\s+PUBLIC_EXPORT\s+(create\w+)\s*\(\s*(I\w+)\s*\*\*\s*(\w+)\s*(?:,([^)]*))?\)\s*;");

        /// <summary>Factories declared as plain exported functions instead of through a factory
        /// macro (createJsonSerializer). They sit in headers without any interface, which the
        /// parser has nothing to make of, so they are read off the header text: an exported
        /// <c>ErrCode create...(IInterface** obj, ...)</c> constructs that interface.</summary>
        private List<OrderedDict> ScanPlainFactories(string coreRoot)
        {
            var result = new List<OrderedDict>();
            foreach (KeyValuePair<string, string> root in ModuleRoots(coreRoot))
            {
                foreach (string header in HeadersUnder(root.Key))
                {
                    string text = CommentRegex.Replace(
                        HeaderSanitizer.StripMacroDefinitions(File.ReadAllText(header)), "");
                    foreach (Match match in PlainFactoryRegex.Matches(text))
                    {
                        var arguments = new List<object>
                        {
                            new OrderedDict
                            {
                                { "name", match.Groups[3].Value },
                                { "type", new OrderedDict { { "name", match.Groups[2].Value }, { "pointers", 2L } } }
                            }
                        };
                        foreach (string rawArg in match.Groups[4].Value.Split(','))
                        {
                            string[] declaration = rawArg.Split(new[] { '=' }, 2);
                            string arg = string.Join(" ", declaration[0].Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                            if (arg.Length == 0)
                            {
                                continue;
                            }
                            int nameStart = arg.LastIndexOfAny(new[] { ' ', '*', '&' }) + 1;
                            var argument = new OrderedDict
                            {
                                { "name", arg.Substring(nameStart) },
                                { "type", CppTypeDesc(arg.Substring(0, nameStart)) }
                            };
                            if (declaration.Length > 1)
                            {
                                argument.Add("default_value", declaration[1].Trim());
                            }
                            arguments.Add(argument);
                        }

                        result.Add(new OrderedDict
                        {
                            { "name", match.Groups[1].Value },
                            { "return_type", new OrderedDict { { "name", "ErrCode" } } },
                            { "interface", match.Groups[2].Value },
                            { "arguments", arguments },
                            { "library", root.Value },
                            { "header", RelativeHeader(header, coreRoot) }
                        });
                    }
                }
            }
            return result;
        }

        private static readonly Regex NumericMacroRegex = new Regex(
            @"^[ \t]*#[ \t]*define[ \t]+(\w+)[ \t]+\(?[ \t]*(-?(?:0[xX][0-9A-Fa-f]+|\d+))[uUlL]*[ \t]*\)?[ \t]*(?://.*)?\r?$",
            RegexOptions.Multiline);

        /// <summary>Every macro in the public headers that is defined as a plain integer.</summary>
        private static Dictionary<string, long> ScanNumericMacros(string coreRoot)
        {
            var macros = new Dictionary<string, long>();
            foreach (string root in IncludeRoots(coreRoot))
            {
                foreach (string header in HeadersUnder(root))
                {
                    foreach (Match match in NumericMacroRegex.Matches(File.ReadAllText(header)))
                    {
                        long value;
                        if (TryParseInt(match.Groups[2].Value, out value))
                        {
                            macros[match.Groups[1].Value] = value;
                        }
                    }
                }
            }
            return macros;
        }

        /// <summary>Gives the options of an enum that is spelled with macros (LogLevel's
        /// <c>Trace = OPENDAQ_LOG_LEVEL_TRACE</c>) their real values. The parser does not evaluate
        /// macros and numbers such options consecutively instead, which is only right by coincidence.
        /// The enum is re-read from the header text and the macros are looked up.</summary>
        private static void ApplyMacroEnumValues(OrderedDict enumeration, string headerPath,
                                                 Dictionary<string, long> macros)
        {
            string text = CommentRegex.Replace(File.ReadAllText(headerPath), "");
            foreach (Match match in EnumRegex.Matches(text))
            {
                if (match.Groups[1].Value != enumeration.GetString("name"))
                {
                    continue;
                }

                var values = new Dictionary<string, long>();
                bool usesMacro = false;
                long nextValue = 0;
                foreach (string rawPart in match.Groups[2].Value.Split(','))
                {
                    string part = rawPart.Trim();
                    if (part.Length == 0)
                    {
                        continue;
                    }
                    int assign = part.IndexOf('=');
                    string name = assign >= 0 ? part.Substring(0, assign).Trim() : part;
                    if (assign >= 0)
                    {
                        string raw = part.Substring(assign + 1).Trim();
                        long value;
                        if (macros.TryGetValue(raw, out value))
                        {
                            usesMacro = true;
                        }
                        else if (!TryParseInt(raw, out value))
                        {
                            // An expression: leave the enum as the parser read it.
                            return;
                        }
                        nextValue = value;
                    }
                    values[name] = nextValue++;
                }

                if (!usesMacro)
                {
                    return;
                }
                foreach (OrderedDict option in DictItems(enumeration.GetList("options")))
                {
                    long value;
                    if (values.TryGetValue(option.GetString("name"), out value))
                    {
                        option.Remove("raw_value");
                        option.Set("value", value);
                    }
                }
                return;
            }
        }

        private static void ResolveEnumValues(OrderedDict enumeration)
        {
            long nextValue = 0;
            foreach (OrderedDict option in DictItems(enumeration.GetList("options")))
            {
                object raw = option.Get("value");
                if (raw == null)
                {
                    option.Set("value", nextValue++);
                    continue;
                }
                long parsed;
                if (TryParseInt(raw.ToString(), out parsed))
                {
                    option.Set("value", parsed);
                    nextValue = parsed + 1;
                }
                else
                {
                    option.Remove("value");
                    option.Set("raw_value", raw.ToString());
                }
            }
        }

        private static bool TryParseInt(string text, out long value)
        {
            text = text.Trim().TrimEnd('u', 'U', 'l', 'L');
            try
            {
                value = text.StartsWith("0x") || text.StartsWith("0X")
                    ? Convert.ToInt64(text.Substring(2), 16)
                    : Convert.ToInt64(text, 10);
                return true;
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
        }

        // -------------------------------------------------------------------
        //  Helpers
        // -------------------------------------------------------------------

        private string GetSourceOption()
        {
            // --source lands in ProgramOptions.InputFile, which is not part of
            // IGeneratorOptions (config generation does not parse a source file),
            // so it is read via reflection.
            var property = Options.GetType().GetProperty("InputFile");
            return property?.GetValue(Options, null) as string;
        }

        private static IEnumerable<OrderedDict> DictItems(List<object> list)
        {
            return list == null ? Enumerable.Empty<OrderedDict>() : list.OfType<OrderedDict>();
        }

        private static string RelativeHeader(string header, string coreRoot)
        {
            // Headers are enumerated relative to --source, so with a repository-
            // relative --source (e.g. "core") they are already repository-relative
            // like all other header provenance; only normalize separators.
            return header.Replace('\\', '/');
        }
    }
}
