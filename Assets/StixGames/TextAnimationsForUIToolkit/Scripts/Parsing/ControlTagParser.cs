using System.Collections.Generic;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.Data;

namespace TextAnimationsForUIToolkit.Parsing
{
    public abstract class ControlTagParser : ITagParser
    {
        public abstract IEnumerable<string> GetTagNames();

        [CanBeNull]
        public abstract TextUnit OpenTag(string tagName, Parameters parameters);

        [CanBeNull]
        public abstract TextUnit CloseTag(string tagName);

        public TextAnimationSettings settings { get; set; }

        public virtual bool HasDynamicTags => false;
    }
}
