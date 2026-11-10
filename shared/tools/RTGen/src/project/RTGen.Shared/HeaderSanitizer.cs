using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RTGen.Cpp;
using RTGen.Interfaces;

// This file is compiled into every generator that walks the whole core source tree (RTGen.Json and
// RTGen.C), so that they agree on which headers there are and on what each of them declares.
namespace RTGen.Shared
{
    /// <summary>A freestanding extern "C" PUBLIC_EXPORT function, which the grammar cannot parse and
    /// which is therefore lifted out of the header text before parsing.</summary>
    internal class ExportedFunction
    {
        /// <summary>Name of the exported symbol.</summary>
        public string Name;

        /// <summary>Return type as declared in C++, for example "daq::ErrCode".</summary>
        public string ReturnType;

        /// <summary>Arguments in declaration order: the C++ type declaration and the name.</summary>
        public List<KeyValuePair<string, string>> Arguments = new List<KeyValuePair<string, string>>();
    }

    /// <summary>Parses core headers that rtgen's grammar rejects verbatim, by retrying with a copy
    /// that has the offending constructs removed.</summary>
    internal static class HeaderSanitizer
    {
        /// <summary>Interface injected into factory-only headers. It never belongs in any output.</summary>
        public const string PlaceholderInterface = "IRtGenSanitizerPlaceholder";

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

        /// <summary>Whether the header declares interfaces or class factories. Everything else (smart
        /// pointers, implementations, factory helpers) has no ABI declarations to parse. Matched on
        /// define-stripped text, so that the factory macro machinery itself (factory.h) is not
        /// mistaken for declarations.</summary>
        public static bool DeclaresBindings(string headerText)
        {
            return DeclRegex.IsMatch(StripMacroDefinitions(headerText));
        }

        /// <summary>Parses the header as-is; when the grammar rejects it, retries with a
        /// sanitized copy that strips the constructs rtgen's parser cannot handle (macro
        /// bodies, function-pointer typedefs, using-aliases, inline template helpers) and
        /// injects a placeholder interface into factory-only headers (the parser crashes
        /// on factories when the file declares no interface). Freestanding extern "C"
        /// PUBLIC_EXPORT declarations are handed back in <paramref name="exportedFunctions"/>,
        /// since the grammar cannot parse them either.</summary>
        public static IRTFile Parse(CppParser parser, IParserOptions parserOptions, string path,
                                    List<ExportedFunction> exportedFunctions)
        {
            try
            {
                return parser.Parse(path, parserOptions);
            }
            catch (Exception)
            {
                var exported = new List<ExportedFunction>();
                string sanitized = Sanitize(File.ReadAllText(path), exported);

                string tempDir = Path.Combine(Path.GetTempPath(), "rtgen-sanitized");
                Directory.CreateDirectory(tempDir);
                string tempPath = Path.Combine(tempDir, Path.GetFileName(path));
                File.WriteAllText(tempPath, sanitized, new UTF8Encoding(false));

                IRTFile rtFile = parser.Parse(tempPath, parserOptions);
                // Contribute the extracted exports only when the retry actually parsed.
                exportedFunctions.AddRange(exported);
                return rtFile;
            }
        }

        private static string Sanitize(string text, List<ExportedFunction> exported)
        {
            text = StripMacroDefinitions(text);
            text = ExportedFunctionRegex.Replace(text, match =>
            {
                exported.Add(BuildExportedFunction(match));
                return "";
            });
            text = FunctionPointerTypedefRegex.Replace(text, "");
            text = UsingAliasRegex.Replace(text, "");
            text = StripTemplates(text);

            // Factory-only header: the parser needs a current interface for the factory's
            // out-parameter namespace; give it a throwaway one.
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

        private static ExportedFunction BuildExportedFunction(Match match)
        {
            var function = new ExportedFunction
            {
                Name = match.Groups[2].Value,
                ReturnType = match.Groups[1].Value
            };
            foreach (string rawArg in match.Groups[3].Value.Split(','))
            {
                string arg = string.Join(" ", rawArg.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                if (arg.Length == 0)
                {
                    continue;
                }
                int nameStart = arg.LastIndexOfAny(new[] { ' ', '*', '&' }) + 1;
                function.Arguments.Add(new KeyValuePair<string, string>(
                    arg.Substring(0, nameStart), arg.Substring(nameStart)));
            }
            return function;
        }

        /// <summary>Blanks #define directives including their line continuations. Macro
        /// bodies routinely contain tokens the grammar cannot lex (->, #stringification,
        /// class definitions).</summary>
        public static string StripMacroDefinitions(string text)
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

        /// <summary>Blanks template declarations/definitions (inline smart-pointer helper
        /// functions next to factory macros); they are C++ convenience sugar with no ABI
        /// of their own. Newlines are preserved so parser positions stay meaningful.</summary>
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
    }
}
