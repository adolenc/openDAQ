using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RTGen.Shared
{
    /// <summary>Finds the factories a header declares only under a preprocessor condition.</summary>
    /// <remarks>
    /// The parser does not preserve conditionals, so to it a factory inside <c>#ifdef _WIN32</c> looks
    /// like any other. Such a factory only exists in the core when the condition holds, and whatever is
    /// generated for it has to be guarded by the same condition.
    /// </remarks>
    internal static class PreprocessorConditions
    {
        private static readonly Regex DirectiveRegex = new Regex(@"^\s*#\s*(ifdef|ifndef|if|elif|else|endif)\b(.*)$");
        private static readonly Regex FactoryMacroRegex = new Regex(@"\bOPENDAQ_DECLARE_CLASS_FACTORY[A-Z_]*\s*\(");
        private static readonly Regex TrailingCommentRegex = new Regex(@"\s*(//.*|/\*.*\*/\s*)$");

        /// <summary>Maps the name of every conditionally declared factory to the condition, written
        /// as a preprocessor expression (for example <c>defined(_WIN32)</c>).</summary>
        /// <param name="headerText">Unmodified text of the core header.</param>
        public static IDictionary<string, string> OfFactories(string headerText)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            // Every open conditional, as the expression that currently holds inside it.
            List<string> stack = new List<string>();
            // What has been tried so far in each open conditional, for #else and #elif.
            List<List<string>> tried = new List<List<string>>();

            string[] lines = headerText.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Match directive = DirectiveRegex.Match(lines[i]);
                if (directive.Success)
                {
                    string argument = TrailingCommentRegex.Replace(directive.Groups[2].Value, "").Trim();
                    switch (directive.Groups[1].Value)
                    {
                        case "ifdef":
                            Open(stack, tried, $"defined({argument})");
                            break;
                        case "ifndef":
                            Open(stack, tried, $"!defined({argument})");
                            break;
                        case "if":
                            Open(stack, tried, Parenthesize(argument));
                            break;
                        case "elif":
                        case "else":
                            if (stack.Count == 0)
                            {
                                break;
                            }
                            string noneBefore = string.Join(" && ", tried[tried.Count - 1].Select(c => "!" + Parenthesize(c)));
                            if (directive.Groups[1].Value == "elif")
                            {
                                tried[tried.Count - 1].Add(Parenthesize(argument));
                                stack[stack.Count - 1] = noneBefore + " && " + Parenthesize(argument);
                            }
                            else
                            {
                                stack[stack.Count - 1] = noneBefore;
                            }
                            break;
                        case "endif":
                            if (stack.Count > 0)
                            {
                                stack.RemoveAt(stack.Count - 1);
                                tried.RemoveAt(tried.Count - 1);
                            }
                            break;
                    }
                    continue;
                }

                if (stack.Count == 0)
                {
                    continue;
                }

                Match macro = FactoryMacroRegex.Match(lines[i]);
                if (!macro.Success)
                {
                    continue;
                }

                string condition = string.Join(" && ", stack);
                foreach (string name in FactoryNames(lines, i, macro.Index + macro.Length))
                {
                    result[name] = condition;
                }
            }

            return result;
        }

        private static void Open(List<string> stack, List<List<string>> tried, string condition)
        {
            stack.Add(condition);
            tried.Add(new List<string> { condition });
        }

        private static string Parenthesize(string expression)
        {
            bool simple = Regex.IsMatch(expression, @"^!?(defined\s*\(\s*\w+\s*\)|\w+|\(.*\))$");
            return simple ? expression : $"({expression})";
        }

        /// <summary>
        /// The names a factory declaration can give its create function: "create" followed by the factory
        /// name (the second macro argument), or any argument that already is a create function name, as
        /// the ..._AND_CREATEFUNC macros take one.
        /// </summary>
        private static IEnumerable<string> FactoryNames(string[] lines, int line, int column)
        {
            List<string> arguments = new List<string>();
            System.Text.StringBuilder current = new System.Text.StringBuilder();
            int depth = 1;

            for (int i = line; i < lines.Length && depth > 0; i++)
            {
                string text = i == line ? lines[i].Substring(column) : lines[i];
                foreach (char c in text)
                {
                    if (c == '(')
                    {
                        depth++;
                    }
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            break;
                        }
                    }

                    if (c == ',' && depth == 1)
                    {
                        arguments.Add(current.ToString().Trim());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }
            arguments.Add(current.ToString().Trim());

            if (arguments.Count > 1 && Regex.IsMatch(arguments[1], @"^\w+$"))
            {
                yield return "create" + arguments[1];
            }
            foreach (string argument in arguments.Where(a => Regex.IsMatch(a, @"^create\w+$")))
            {
                yield return argument;
            }
        }
    }
}
