using RTGen.Interfaces;
using RTGen.Types;

namespace RTGen.C
{
    /// <summary>
    /// Stands in for a parsed header when a whole source tree is generated at once. It carries the root to
    /// scan; the generator does the walking and parses each header it finds.
    /// </summary>
    public class CBatchFile : RTFile
    {
        public CBatchFile(string sourceDir, ILibraryInfo libraryInfo)
            : base(".", ",", libraryInfo)
        {
            SourceDir = sourceDir;
        }

        /// <summary>Root of the source tree to generate bindings for.</summary>
        public string SourceDir { get; }
    }

    /// <summary>
    /// Accepts a directory where rtgen would normally expect a single header, so that the whole tree can be
    /// generated from one invocation. It reports a custom file type, which tells rtgen not to require an
    /// interface declaration in the input.
    /// </summary>
    public class CBatchParser : IParser
    {
        public IRTFile Parse(string fileName, IParserOptions options)
        {
            return new CBatchFile(fileName, options.LibraryInfo ?? new LibraryInfo());
        }

        public ParsedFile FileType => ParsedFile.Custom;
    }
}
