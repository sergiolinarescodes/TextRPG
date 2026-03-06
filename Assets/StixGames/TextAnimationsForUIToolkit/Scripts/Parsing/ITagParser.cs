using System.Collections.Generic;

namespace TextAnimationsForUIToolkit.Parsing
{
    public interface ITagParser
    {
        IEnumerable<string> GetTagNames();

        /// <summary>
        /// Settings used by the parser.
        ///
        /// At the moment settings will not be set for global parsers.
        /// If this is a big limitation for you, please contact me and I can look into fixing this problem for you.
        /// </summary>
        TextAnimationSettings settings { get; set; }

        bool HasDynamicTags { get; }
    }
}
