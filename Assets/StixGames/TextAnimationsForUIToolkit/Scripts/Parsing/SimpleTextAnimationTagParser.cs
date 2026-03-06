using System.Collections.Generic;
using JetBrains.Annotations;

namespace TextAnimationsForUIToolkit.Parsing
{
    public abstract class TextAnimationTagParser : ITagParser
    {
        public abstract IEnumerable<string> GetTagNames();

        public abstract IEnumerable<TextAnimation> CreateAnimations(
            string tagName,
            Parameters parameters
        );

        public TextAnimationSettings settings { get; set; }

        public virtual bool HasDynamicTags => false;
    }

    public abstract class SimpleTextAnimationTagParser : TextAnimationTagParser
    {
        /// <summary>
        /// <para>
        /// Returns an animation for the given parameters.
        /// </para>
        /// <para>
        /// If the parameters are wrong in some way, an exception can be thrown. The tag will be rendered as text.
        /// If OverwriteLastTag is true, null can be returned to remove the last tag, otherwise returning null
        /// will be treated like an exception and the tag will be included as text.
        /// ArgumentExceptions will be silently ignored, all other exceptions will create a log.
        /// </para>
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [CanBeNull]
        public virtual TextAnimation CreateAnimation(string tagName, Parameters parameters)
        {
            return null;
        }

        public override IEnumerable<TextAnimation> CreateAnimations(
            string tagName,
            Parameters parameters
        )
        {
            var animation = CreateAnimation(tagName, parameters);
            if (animation != null)
            {
                yield return animation;
            }
        }
    }
}
