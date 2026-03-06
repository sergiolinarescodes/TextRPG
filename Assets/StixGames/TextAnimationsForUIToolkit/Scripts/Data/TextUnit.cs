namespace TextAnimationsForUIToolkit.Data
{
    public abstract class TextUnit { }

    public abstract class TimedTextUnit : TextUnit
    {
        /// <summary>
        /// The time when the unit starts to appear.
        ///
        /// When setting this value on a <c>Letter</c>, make sure to set the <c>wordAppearanceTime</c> accordingly.
        /// </summary>
        [IncludeInSnapshotTest]
        public float appearanceTime { get; set; } = float.NegativeInfinity;

        /// <summary>
        /// The time when the unit has fully disappeared.
        ///
        /// When setting this value on a <c>Letter</c>, make sure to set the <c>wordVanishingTime</c> accordingly.
        /// </summary>
        [IncludeInSnapshotTest]
        public float vanishingTime { get; set; } = float.PositiveInfinity;
    }
}
