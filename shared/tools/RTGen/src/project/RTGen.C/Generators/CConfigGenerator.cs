using RTGen.Cpp;
using RTGen.Exceptions;
using RTGen.Interfaces;
using RTGen.Types;
using RTGen.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RTGen.C.Generators
{
    /// <summary>Discovery-driven C bindings generator (invoked as
    /// `rtgen --language=c --config --source=core`). It discovers every core header that
    /// declares an interface or a class factory, parses each with the C++ parser in-process
    /// (re-parsing headers the grammar rejects from a sanitized copy), and drives the
    /// per-interface <see cref="CGenerator"/> to emit
    /// <c>include/&lt;clib&gt;/&lt;name&gt;.h</c> and <c>src/&lt;clib&gt;/&lt;name&gt;.cpp</c>.
    ///
    /// The exceptions the previous hand-written run_rtgen.sh list carried (skip-list,
    /// factory-only headers) are encoded here; the name-collision renames and forbidden
    /// types live in <see cref="CGenerator"/>.
    ///
    /// Options: --source points at the core sources root, --outputDir is the bindings/c root
    /// (headers land under include/, sources under src/).</summary>
    class CConfigGenerator : IConfigGenerator
    {
        private static readonly Regex DeclRegex = new Regex(
            @"^\s*(DECLARE_(TEMPLATED_)?OPENDAQ_(CUSTOM_)?INTERFACE[A-Z_]*|OPENDAQ_DECLARE_CLASS_FACTORY[A-Z_]*)\s*\(",
            RegexOptions.Multiline);
        private static readonly Regex ExportedFunctionRegex = new Regex(
            @"extern\s+""C""\s+([\w:]+)\s+PUBLIC_EXPORT\s+(\w+)\s*\(([^)]*)\)\s*;",
            RegexOptions.Singleline);
        private static readonly Regex FunctionPointerTypedefRegex = new Regex(
            @"typedef\s+[\w:]+\s*\(\s*\*\s*\w+\s*\)\s*\([^)]*\)\s*;");
        private static readonly Regex UsingAliasRegex = new Regex(
            @"^\s*using\s+\w+\s*=[^;]*;", RegexOptions.Multiline);
        private const string PlaceholderInterface = "IRtGenSanitizerPlaceholder";

        /// <summary>Discovered interface headers whose C binding is hand-maintained (not
        /// generated). <c>baseobject</c> is hand-written as <c>base_object.h</c>
        /// (<c>IBaseObject</c> maps to the <c>daqBaseObject</c>/<c>void</c> alias).</summary>
        private static readonly HashSet<string> SkipHeaders = new HashSet<string>
        {
            // Hand-written as base_object.h (IBaseObject -> the daqBaseObject/void alias).
            "baseobject",

            // Excluded from the C bindings build (kept out of the previous CMake lists too):
            // the element-type interfaces pass an IntfID out-parameter the generator cannot
            // marshal, and these internal interfaces are not part of the exposed C surface.
            "dict_element_type",
            "list_element_type",
            "type_manager_private",
            "permissions_internal",
            "property_object_class_internal",
            "user_internal",

            // Interface headers whose core C++ header pulls in third-party dependencies
            // (taskflow / spdlog) that are not on the C bindings include path.
            "task_internal",
            "streaming_private",
            "logger_sink_base_private",
            "logger_thread_pool_private",

            // Overloaded methods / mismatched out-parameter types that cannot be expressed
            // in C; excluded from the curated bindings.
            "scaling_calc_private",
            "data_rule_calc_private",
            "component_deserialize_context",
            "wrapped_data_packet",
        };

        private IGeneratorOptions _options;

        public IVersionInfo Version => new VersionInfo
        {
            Major = 0,
            Minor = 7,
            Patch = 0
        };

        public IGeneratorOptions Options
        {
            get => _options;
            set
            {
                _options = value;
                if (_options.LibraryInfo == null)
                {
                    _options.LibraryInfo = new LibraryInfo();
                }
            }
        }

        public void GenerateConfigFile(string templatePath)
        {
            string coreRoot = GetSourceOption();
            if (string.IsNullOrEmpty(coreRoot))
            {
                throw new GeneratorException(
                    "--source=<core dir> is required (the directory to discover headers in).");
            }

            string outputRoot = string.IsNullOrEmpty(Options.OutputDir) ? "." : Options.OutputDir;

            // templatePath resolves to c.config.template; the real per-file templates
            // (c.template / c.header.template / *.method.template) sit next to it.
            string templatesDir = Path.GetDirectoryName(Path.GetFullPath(templatePath));
            string cTemplate = Path.Combine(templatesDir, "c.template");

            var parser = new CppParser();
            var parserOptions = (IParserOptions)Options;
            int generatedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;

            foreach (KeyValuePair<string, string> discovered in DiscoverHeaders(coreRoot))
            {
                string path = discovered.Key;
                string clib = discovered.Value;
                string baseName = Path.GetFileNameWithoutExtension(path);

                if (SkipHeaders.Contains(baseName))
                {
                    skippedCount++;
                    continue;
                }

                IRTFile rtFile;
                try
                {
                    parserOptions.LibraryInfo.Name = clib;
                    rtFile = ParseHeader(parser, parserOptions, path);
                    rtFile.SourceFileName = Path.GetFileName(path);
                }
                catch (Exception e)
                {
                    failedCount++;
                    Log.Warning($"FAILED {path}: {e.Message}");
                    continue;
                }

                // Nothing bindable: header with no interface, or a factory-only header where
                // the sanitizer injected only the throwaway placeholder interface.
                if (rtFile.CurrentClass == null
                    || rtFile.CurrentClass.Type.Name == PlaceholderInterface)
                {
                    skippedCount++;
                    continue;
                }

                // C has no function overloading, so interfaces with overloaded methods
                // (IScalingCalcPrivate, IDataRuleCalcPrivate) cannot be bound mechanically.
                if (HasOverloadedMethods(rtFile.CurrentClass))
                {
                    Log.Warning($"Skipping {clib}/{baseName}: overloaded methods "
                                + "cannot be expressed in C.");
                    skippedCount++;
                    continue;
                }

                try
                {
                    EmitFile(rtFile, clib, baseName, Path.Combine(outputRoot, "include", clib), ".h", cTemplate);
                    EmitFile(rtFile, clib, baseName, Path.Combine(outputRoot, "src", clib), ".cpp", cTemplate);
                    generatedCount++;
                }
                catch (Exception e)
                {
                    failedCount++;
                    Log.Warning($"FAILED generating {clib}/{baseName}: {e.Message}");
                }
            }

            if (generatedCount == 0)
            {
                throw new GeneratorException(
                    $"No parseable interface headers found under \"{coreRoot}\".");
            }

            Log.Info($"Generated C bindings for {generatedCount} interfaces "
                     + $"({failedCount} failures, {skippedCount} skipped) under {outputRoot}");
        }

        /// <summary>Drives a fresh <see cref="CGenerator"/> exactly as the CLI would for one
        /// source file: it re-points Options at the target directory/extension (the CGenerator
        /// Options setter reads the extension to pick header vs source mode) and generates.</summary>
        private void EmitFile(IRTFile rtFile, string clib, string baseName,
                              string outputDir, string extension, string cTemplate)
        {
            Directory.CreateDirectory(outputDir);

            // OutputDir is read-only on IGeneratorOptions; set it on the concrete
            // ProgramOptions via reflection (same approach as reading --source).
            Options.GetType().GetProperty("OutputDir")?.SetValue(Options, outputDir, null);
            Options.Filename = baseName;
            Options.GeneratedExtension = extension;
            Options.LibraryInfo.Name = clib;

            var generator = new CGenerator
            {
                Options = Options,
                RtFile = rtFile
            };
            generator.GenerateFile(cTemplate);
        }

        // -------------------------------------------------------------------
        //  Parsing with sanitize-and-retry
        // -------------------------------------------------------------------

        private static IRTFile ParseHeader(CppParser parser, IParserOptions parserOptions, string path)
        {
            try
            {
                return parser.Parse(path, parserOptions);
            }
            catch (Exception)
            {
                string sanitized = SanitizeHeaderSource(File.ReadAllText(path));

                string tempDir = Path.Combine(Path.GetTempPath(), "rtgen-c-sanitized");
                Directory.CreateDirectory(tempDir);
                string tempPath = Path.Combine(tempDir, Path.GetFileName(path));
                File.WriteAllText(tempPath, sanitized, new UTF8Encoding(false));

                return parser.Parse(tempPath, parserOptions);
            }
        }

        /// <summary>Strips the constructs rtgen's grammar cannot handle (macro bodies,
        /// freestanding extern "C" exports, function-pointer typedefs, using-aliases, inline
        /// template helpers) and injects a placeholder interface into factory-only headers so
        /// the parser has a current interface for the factory's out-parameter namespace. The C
        /// generator drops all of these anyway (the free functions and error helpers are
        /// hand-written).</summary>
        private static string SanitizeHeaderSource(string text)
        {
            text = StripMacroDefinitions(text);
            text = ExportedFunctionRegex.Replace(text, "");
            text = FunctionPointerTypedefRegex.Replace(text, "");
            text = UsingAliasRegex.Replace(text, "");
            text = StripTemplates(text);

            if (!text.Contains("DECLARE_OPENDAQ_INTERFACE") && DeclRegex.IsMatch(text))
            {
                int marker = text.IndexOf("BEGIN_NAMESPACE_OPENDAQ", StringComparison.Ordinal);
                if (marker >= 0)
                {
                    int lineEnd = text.IndexOf('\n', marker);
                    if (lineEnd >= 0)
                    {
                        text = text.Insert(lineEnd + 1,
                            $"DECLARE_OPENDAQ_INTERFACE({PlaceholderInterface}, IBaseObject)\n{{\n}};\n");
                    }
                }
            }
            return text;
        }

        private static string StripMacroDefinitions(string text)
        {
            string[] lines = text.Split('\n');
            bool inDefine = false;
            for (int i = 0; i < lines.Length; i++)
            {
                bool blank = inDefine || lines[i].TrimStart().StartsWith("#define");
                if (blank)
                {
                    inDefine = lines[i].TrimEnd().EndsWith("\\");
                    lines[i] = "";
                }
            }
            return string.Join("\n", lines);
        }

        private static string StripTemplates(string text)
        {
            var sb = new StringBuilder(text);
            int searchFrom = 0;
            while (true)
            {
                int start = FindWord(sb, "template", searchFrom);
                if (start < 0)
                {
                    return sb.ToString();
                }

                int pos = start + "template".Length;
                while (pos < sb.Length && char.IsWhiteSpace(sb[pos]))
                {
                    pos++;
                }
                if (pos >= sb.Length || sb[pos] != '<')
                {
                    searchFrom = start + 1;
                    continue;
                }

                int angleDepth = 0;
                int braceDepth = 0;
                bool sawBrace = false;
                int end = -1;
                for (; pos < sb.Length; pos++)
                {
                    char c = sb[pos];
                    if (c == '<' && braceDepth == 0) angleDepth++;
                    else if (c == '>' && braceDepth == 0 && angleDepth > 0) angleDepth--;
                    else if (c == '{') { braceDepth++; sawBrace = true; }
                    else if (c == '}')
                    {
                        braceDepth--;
                        if (sawBrace && braceDepth == 0) { end = pos + 1; break; }
                    }
                    else if (c == ';' && braceDepth == 0 && angleDepth == 0 && !sawBrace)
                    {
                        end = pos + 1;
                        break;
                    }
                }
                if (end < 0)
                {
                    return sb.ToString();
                }
                while (end < sb.Length && (char.IsWhiteSpace(sb[end]) || sb[end] == ';'))
                {
                    if (sb[end] == ';') { end++; break; }
                    end++;
                }
                for (int i = start; i < end; i++)
                {
                    if (sb[i] != '\n')
                    {
                        sb[i] = ' ';
                    }
                }
                searchFrom = start;
            }
        }

        private static int FindWord(StringBuilder sb, string word, int from)
        {
            string text = sb.ToString();
            for (int index = text.IndexOf(word, from, StringComparison.Ordinal);
                 index >= 0;
                 index = text.IndexOf(word, index + 1, StringComparison.Ordinal))
            {
                bool startOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]) && text[index - 1] != '_';
                int after = index + word.Length;
                bool endOk = after >= text.Length || !char.IsLetterOrDigit(text[after]) && text[after] != '_';
                if (startOk && endOk)
                {
                    return index;
                }
            }
            return -1;
        }

        // -------------------------------------------------------------------
        //  Header discovery
        // -------------------------------------------------------------------

        /// <summary>The public include roots under the core sources directory, paired with the
        /// C library directory their bindings live in. corecontainers installs its headers into
        /// the coretypes include directory and shares the ccoretypes binding library; each
        /// opendaq subsystem gets its own copendaq/&lt;subsystem&gt; directory.</summary>
        private static IEnumerable<KeyValuePair<string, string>> ModuleRoots(string coreRoot)
        {
            var roots = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(
                    Path.Combine(coreRoot, "coretypes", "include", "coretypes"), "ccoretypes"),
                new KeyValuePair<string, string>(
                    Path.Combine(coreRoot, "corecontainers", "include", "coretypes"), "ccoretypes"),
                new KeyValuePair<string, string>(
                    Path.Combine(coreRoot, "coreobjects", "include", "coreobjects"), "ccoreobjects"),
            };
            string opendaqRoot = Path.Combine(coreRoot, "opendaq");
            if (Directory.Exists(opendaqRoot))
            {
                roots.AddRange(Directory.GetDirectories(opendaqRoot)
                    .OrderBy(dir => dir, StringComparer.Ordinal)
                    .Select(dir => new KeyValuePair<string, string>(
                        Path.Combine(dir, "include", "opendaq"),
                        "copendaq/" + Path.GetFileName(dir))));
            }
            return roots.Where(root => Directory.Exists(root.Key));
        }

        /// <summary>Headers that declare interfaces or class factories, paired with their C
        /// library directory. Everything else (smart pointers, impls, factory helpers) has no
        /// ABI declarations to bind.</summary>
        private static IEnumerable<KeyValuePair<string, string>> DiscoverHeaders(string coreRoot)
        {
            foreach (KeyValuePair<string, string> root in ModuleRoots(coreRoot))
            {
                foreach (string header in HeadersUnder(root.Key))
                {
                    // Match on define-stripped text so the factory macro machinery
                    // (factory.h) is not mistaken for declarations.
                    if (DeclRegex.IsMatch(StripMacroDefinitions(File.ReadAllText(header))))
                    {
                        yield return new KeyValuePair<string, string>(header, root.Value);
                    }
                }
            }
        }

        private static IEnumerable<string> HeadersUnder(string root)
        {
            return Directory.GetFiles(root, "*.h", SearchOption.AllDirectories)
                .OrderBy(file => file, StringComparer.Ordinal);
        }

        // -------------------------------------------------------------------
        //  Helpers
        // -------------------------------------------------------------------

        private static bool HasOverloadedMethods(IRTInterface iface)
        {
            var seen = new HashSet<string>();
            foreach (IMethod method in iface.Methods)
            {
                if (!seen.Add(method.Name))
                {
                    return true;
                }
            }
            return false;
        }

        private string GetSourceOption()
        {
            // --source lands in ProgramOptions.InputFile, which is not part of
            // IGeneratorOptions (config generation does not parse a single source file),
            // so it is read via reflection.
            var property = Options.GetType().GetProperty("InputFile");
            return property?.GetValue(Options, null) as string;
        }
    }
}
