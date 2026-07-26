using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RTGen.C
{
    /// <summary>A single header that bindings are generated for.</summary>
    public class BindingTarget
    {
        /// <summary>Full path of the core header.</summary>
        public string HeaderPath { get; set; }

        /// <summary>Output library, relative to the bindings root (e.g. "ccoretypes", "copendaq/signal").</summary>
        public string Library { get; set; }

        /// <summary>Generated file name without extension.</summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Finds the interfaces to generate bindings for by scanning the source tree, so that no list of files
    /// has to be kept in sync with the repository.
    /// </summary>
    public static class CBatchGeneration
    {
        private static readonly Regex InterfaceOrFactory = new Regex(
            @"DECLARE_OPENDAQ_INTERFACE\w*\(\s*I\w+|OPENDAQ_DECLARE_(CLASS_)?FACTORY",
            RegexOptions.Compiled);

        /// <summary>
        /// Interfaces that are deliberately not bound, keyed by header file name, with the reason.
        /// Extend this when an interface cannot be expressed in the generated language.
        /// </summary>
        /// <remarks>
        /// Note that most "private" and "internal" interfaces bind perfectly well and are generated; only
        /// the ones listed here are a problem, so they are excluded individually rather than by name pattern.
        /// </remarks>
        private static readonly IDictionary<string, string> ExcludedHeaders = new Dictionary<string, string>
        {
            // Expose C++ only types (smart pointers, references, third party types) that have no C equivalent.
            ["logger_sink_base_private.h"]   = "returns spdlog SinkPtr",
            ["logger_thread_pool_private.h"] = "returns spdlog ThreadPoolPtr",
            ["task_internal.h"]              = "returns Taskflow and Task by value",
            ["data_rule_calc_private.h"]     = "takes a NumberPtr reference",
            ["streaming_private.h"]          = "takes a StringPtr",

            // Declared inside a preprocessor conditional, which the parser does not preserve, so the
            // generated code would reference a factory that is not always compiled into the core.
            ["logger_sink.h"]                = "WinDebug factory is guarded by #ifdef _WIN32",
            ["allocator.h"]                  = "MiMalloc factory is guarded by #ifdef OPENDAQ_MIMALLOC_SUPPORT",

            // Overloads collapse onto a single name in C.
            ["scaling_calc_private.h"]       = "two scaleData overloads would generate the same function"
        };

        /// <summary>Directories holding test support code, which is not part of the shipped API.</summary>
        private static readonly string[] NonShippingDirectories = { "mocks", "tests", "test" };

        /// <summary>Checks whether bindings should be generated for the header.</summary>
        /// <param name="headerPath">Full path of the core header.</param>
        /// <param name="reason">Why the header is excluded, when it is.</param>
        public static bool IsExcluded(string headerPath, out string reason)
        {
            return ExcludedHeaders.TryGetValue(Path.GetFileName(headerPath) ?? "", out reason);
        }

        /// <summary>Checks whether the header belongs to test support code rather than to the shipped API.</summary>
        private static bool IsNonShipping(string headerPath)
        {
            return headerPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                             .Any(part => NonShippingDirectories.Contains(part, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>Finds every header below <paramref name="sourceDir"/> that declares an interface or a factory.</summary>
        /// <param name="sourceDir">Root of the source tree to scan.</param>
        public static IList<BindingTarget> Discover(string sourceDir)
        {
            List<BindingTarget> targets = new List<BindingTarget>();

            foreach (string path in Directory.EnumerateFiles(sourceDir, "*.h", SearchOption.AllDirectories))
            {
                if (IsNonShipping(path) || !TryGetLibrary(path, out string library))
                {
                    continue;
                }

                if (!InterfaceOrFactory.IsMatch(File.ReadAllText(path)))
                {
                    continue;
                }

                targets.Add(new BindingTarget
                {
                    HeaderPath = path,
                    Library = library,
                    Name = Path.GetFileNameWithoutExtension(path)
                });
            }

            return targets.OrderBy(t => t.Library, StringComparer.Ordinal)
                          .ThenBy(t => t.Name, StringComparer.Ordinal)
                          .ToList();
        }

        /// <summary>
        /// Derives the output library from the location of the header, which is
        /// "&lt;anything&gt;/&lt;component&gt;/include/&lt;includeDir&gt;/&lt;name&gt;.h".
        /// </summary>
        private static bool TryGetLibrary(string headerPath, out string library)
        {
            library = null;

            string[] parts = headerPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            int include = Array.LastIndexOf(parts, "include");
            if (include <= 0 || include + 2 >= parts.Length)
            {
                return false;
            }

            string includeDir = parts[include + 1];
            string component = parts[include - 1];

            switch (includeDir)
            {
                // A few components (corecontainers) contribute to a library named after the include
                // directory rather than after themselves, so the include directory decides.
                case "coretypes":
                    library = "ccoretypes";
                    return true;
                case "coreobjects":
                    library = "ccoreobjects";
                    return true;
                case "opendaq":
                    library = "copendaq/" + component;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Deletes previously generated files that no longer correspond to an interface, which happens when
        /// a core header is renamed or removed. Only files carrying the generated banner are considered, so
        /// hand written sources are never touched.
        /// </summary>
        /// <param name="bindingsRoot">Root of the bindings, holding "include" and "src".</param>
        /// <param name="expected">Paths that the current run accounts for.</param>
        /// <returns>The files that were removed.</returns>
        public static IList<string> PruneOrphans(string bindingsRoot, ISet<string> expected)
        {
            List<string> removed = new List<string>();

            foreach (string subDir in new[] { "include", "src" })
            {
                string root = Path.Combine(bindingsRoot, subDir);
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(path);
                    if (extension != ".h" && extension != ".cpp")
                    {
                        continue;
                    }

                    if (expected.Contains(Path.GetFullPath(path)) || !IsGenerated(path))
                    {
                        continue;
                    }

                    File.Delete(path);
                    removed.Add(path);
                }
            }

            return removed;
        }

        /// <summary>Checks whether the file carries the banner that marks it as generated.</summary>
        private static bool IsGenerated(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                for (int i = 0; i < 3; i++)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        return false;
                    }

                    if (line.Contains("<auto-generated>"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Rewrites the umbrella header so that it lists every generated public header. This is specific to
        /// the C bindings, which have no other way of exposing the whole API from a single include.
        /// </summary>
        /// <param name="bindingsRoot">Root of the bindings, holding "include" and "src".</param>
        public static void WriteUmbrellaHeader(string bindingsRoot)
        {
            string includeRoot = Path.Combine(bindingsRoot, "include");
            string umbrella = Path.Combine(includeRoot, "copendaq.h");
            if (!File.Exists(umbrella))
            {
                return;
            }

            // Keep the existing licence banner.
            string[] existing = File.ReadAllLines(umbrella);
            IEnumerable<string> banner = existing.TakeWhile(l => !l.StartsWith("#pragma once"));

            List<string> headers = Directory
                .EnumerateFiles(includeRoot, "*.h", SearchOption.AllDirectories)
                .Select(p => GetRelativePath(includeRoot, p))
                .Where(rel => rel != "copendaq.h"
                              && rel != "copendaq_private.h"
                              && rel != "ccommon.h"
                              && !rel.StartsWith("private/", StringComparison.Ordinal))
                .OrderBy(rel => rel, StringComparer.Ordinal)
                .ToList();

            StringBuilder sb = new StringBuilder();
            foreach (string line in banner)
            {
                sb.AppendLine(line);
            }

            sb.AppendLine("#pragma once");
            sb.AppendLine();
            sb.AppendLine("#ifdef __cplusplus");
            sb.AppendLine("extern \"C\"");
            sb.AppendLine("{");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("#include <ccommon.h>");

            string previousDir = null;
            foreach (string rel in headers)
            {
                string dir = Path.GetDirectoryName(rel);
                if (dir != previousDir)
                {
                    sb.AppendLine();
                    previousDir = dir;
                }
                sb.AppendLine($"#include <{rel}>");
            }

            sb.AppendLine();
            sb.AppendLine("#ifdef __cplusplus");
            sb.AppendLine("}");
            sb.AppendLine("#endif");

            File.WriteAllText(umbrella, sb.ToString());
        }

        private static string GetRelativePath(string root, string path)
        {
            return path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}
