using System.Collections.Generic;
using System.Linq;
using System.Text;
using TextAnimationsForUIToolkit.BuiltinTags;

namespace TextAnimationsForUIToolkit.Data
{
    public class Whitespace : TextUnit
    {
        /// <summary>
        /// The letter to be displayed.
        ///
        /// Can also include tags, but it should never render as more than a single letter.
        /// </summary>
        [IncludeInSnapshotTest]
        public string text { get; internal set; }

        /// <summary>
        /// The rich text tags assigned to this whitespace. The list and <c>RichTextTag</c> objects are shared between multiple units.
        /// </summary>
        [IncludeInSnapshotTest]
        public List<RichTextTag> richTextTags { get; set; }

        [IncludeInSnapshotTest]
        public bool hasLink { get; internal set; }

        public override string ToString()
        {
            return text;
        }

        internal void BuildWhitespaceString(StringBuilder builder)
        {
            foreach (var tag in richTextTags.Where(x => !IsIgnoredTag(x.tag)))
            {
                tag.BuildOpenTag(builder);
            }

            builder.Append(text);

            foreach (
                var tag in richTextTags.AsReadOnly().Reverse().Where(x => !IsIgnoredTag(x.tag))
            )
            {
                tag.BuildCloseTag(builder);
            }
        }

        private bool IsIgnoredTag(string tag)
        {
            return tag is "color" or "opacity";
        }
    }
}
