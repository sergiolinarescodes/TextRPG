using System;
using UnityEngine;

namespace TextAnimationsForUIToolkit
{
    public class AnimationResult
    {
        public float horizontalOffset { get; set; }

        /// <summary>
        /// The vertical offset in font units.
        /// </summary>
        public float verticalOffset { get; set; }

        public Color? color { get; set; }
        public float? opacity { get; set; }

        public float? rotation { get; set; }

        /// <summary>
        /// Size is meant for animating text, not for changing the font size of text.
        /// </summary>
        public float scaleOffset { get; set; }

        [IncludeInSnapshotTest]
        public bool isInvisible { get; private set; }

        public void MultiplyOpacity(float multiplier)
        {
            if (multiplier is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            }

            opacity ??= 1.0f;

            opacity *= multiplier;
        }

        public void AddVerticalOffset(float offset)
        {
            verticalOffset += offset;
        }

        public void AddHorizontalOffset(float offset)
        {
            horizontalOffset += offset;
        }

        public void MultiplyColor(Color color)
        {
            this.color ??= Color.white;
            this.color = this.color.Value * color;
        }

        public void AddRotation(float rotation)
        {
            this.rotation ??= 0.0f;
            this.rotation += rotation;
        }

        public void AddScaleOffset(float offset)
        {
            scaleOffset += offset;
        }

        public void Clear()
        {
            color = null;
            opacity = null;
            isInvisible = false;
            horizontalOffset = 0;
            verticalOffset = 0;
            rotation = null;
            scaleOffset = 0;
        }

        public void SetInvisible()
        {
            opacity = 0;
            isInvisible = true;
        }
    }
}
