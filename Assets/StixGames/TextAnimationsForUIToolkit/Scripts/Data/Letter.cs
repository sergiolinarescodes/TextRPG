using System.Collections.Generic;
using System.Text;

namespace TextAnimationsForUIToolkit.Data
{
    public class Letter : AnimatableTextUnit
    {
        /// <summary>
        /// The letter to be displayed.
        /// </summary>
        public char letter { get; internal set; }

        [IncludeInSnapshotTest]
        public bool isWordStart { get; internal set; }

        [IncludeInSnapshotTest]
        public bool isWordEnd { get; internal set; }

        /// <summary>
        /// The entire word being written, which can include punctuation.
        /// </summary>
        public string wordText { get; internal set; }

        /// <summary>
        /// The time the entire word has appeared.
        /// </summary>
        [IncludeInSnapshotTest]
        public float wordAppearanceTime { get; internal set; } = float.NegativeInfinity;

        /// <summary>
        /// The time the entire word has disappeared.
        /// </summary>
        [IncludeInSnapshotTest]
        public float wordVanishingTime { get; internal set; } = float.PositiveInfinity;

        /// <summary>
        /// All letters belonging to the same word as this
        /// </summary>
        internal IReadOnlyList<Letter> word { get; set; }

        internal override bool allowAnimatingColor => true;

        public override string ToString()
        {
            return $"{letter}";
        }

        protected override void BuildValue(StringBuilder builder)
        {
            builder.Append(letter);
        }
    }
}
