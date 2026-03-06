using System.Text;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Data
{
    /// <summary>
    /// Represents an inline sprite within the text, parsed from a <c>&lt;sprite&gt;</c> tag.
    /// </summary>
    public class SpriteTag : AnimatableTextUnit
    {
        /// <summary>
        /// The name of the sprite asset to display. Mutually exclusive with Index.
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// The index of the sprite in the sprite asset. Mutually exclusive with Name.
        /// </summary>
        public int? index { get; internal set; }

        /// <summary>
        /// The color specified directly on the sprite tag.
        /// This is separate from the inherited color from surrounding &lt;color&gt; tags.
        /// </summary>
        public Color? spriteColor { get; internal set; }

        /// <summary>
        /// Whether the sprite should be tinted by the current text color.
        /// If true, the Color property from the base class is ignored.
        /// </summary>
        public bool tint { get; internal set; }

        internal override bool allowAnimatingColor => tint;

        public override string ToString()
        {
            var result = "<sprite";
            if (name != null)
            {
                result += $" name=\"{name}\"";
            }

            if (index.HasValue)
            {
                result += $" index={index.Value}";
            }

            result += ">";
            return result;
        }

        /// <summary>
        /// Builds the sprite tag representation.
        /// </summary>
        protected override void BuildValue(StringBuilder builder)
        {
            builder.Append("<sprite tint=1");
            if (name != null)
            {
                builder.Append(" name=\"");
                builder.Append(name);
                builder.Append("\"");
            }

            if (index.HasValue)
            {
                builder.Append(" index=");
                builder.Append(index.Value);
            }

            builder.Append('>');
        }
    }
}