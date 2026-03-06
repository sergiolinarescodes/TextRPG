using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.BuiltinTags;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit.Data
{
    /// <summary>
    /// Base class for text units that can be animated, colored, and styled.
    /// Shared between Letter and SpriteTag.
    /// </summary>
    public abstract class AnimatableTextUnit : TimedTextUnit
    {
        /// <summary>
        /// The animations assigned to this text unit.
        /// The list and animation objects are shared between multiple units.
        /// </summary>
        [IncludeInSnapshotTest]
        public List<TextAnimation> animations { get; set; }

        /// <summary>
        /// The rich text tags assigned to this text unit.
        /// The list and <c>RichTextTag</c> objects are shared between multiple units.
        /// </summary>
        [IncludeInSnapshotTest]
        public List<RichTextTag> richTextTags { get; set; }

        [CanBeNull]
        [IncludeInSnapshotTest]
        public SizeTag sizeTag { get; set; }

        /// <summary>
        /// Is true if the unit is inside an `&lt;a&gt;` link tag.
        /// </summary>
        [IncludeInSnapshotTest]
        public bool hasLink { get; internal set; }

        /// <summary>
        /// Color set by a `&lt;color&gt;` tag.
        /// </summary>
        [IncludeInSnapshotTest]
        public Color? color { get; set; }

        /// <summary>
        /// Opacity set by a `&lt;alpha&gt;` tag.
        /// </summary>
        [IncludeInSnapshotTest]
        public float? opacity { get; set; }

        /// <summary>
        /// The visual UI element representing this unit
        /// </summary>
        internal Label label { get; set; }

        #if UNITY_6000_3_OR_NEWER
        /// <summary>
        /// If the animated visual element has a material, each animated text unit gets its own copy of the material.
        /// The materials are individually animated using the "_AnimationTime" and "_FadeProgress" shader variables.
        /// </summary>
        public Material material { get; set; }

        /// <summary>
        /// Shader properties that are set for this text unit.
        /// </summary>
        public List<ShaderPropertyTag> shaderProperties { get; set; }
        #endif

        internal abstract bool allowAnimatingColor { get; }

        public float GetFontSize(float baseSize)
        {
            return sizeTag?.GetFontSize(baseSize) ?? baseSize;
        }

        /// <summary>
        /// Builds the string representation of this text unit, including applied rich text formatting.
        /// </summary>
        internal void BuildString(StringBuilder builder)
        {
            foreach (var tag in richTextTags)
            {
                tag.BuildOpenTag(builder);
            }

            BuildValue(builder);

            foreach (var tag in richTextTags.AsReadOnly().Reverse().Where(x => x.tag != "alpha"))
            {
                tag.BuildCloseTag(builder);
            }
        }

        protected abstract void BuildValue(StringBuilder builder);
    }
}