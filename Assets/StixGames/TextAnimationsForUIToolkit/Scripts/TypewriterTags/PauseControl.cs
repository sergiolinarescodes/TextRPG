using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;

namespace TextAnimationsForUIToolkit.TypewriterTags
{
    internal class PauseTypewriterTag : TypewriterTag
    {
        /// <summary>
        /// The pause in seconds
        /// </summary>
        public float pause { get; }

        public PauseTypewriterTag(float pause)
        {
            this.pause = pause;
        }
    }

    internal class PauseControlTagParser : ControlTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "pause";
        }

        public override TextUnit OpenTag(string tagName, Parameters parameters)
        {
            if (!parameters.TryGetMainFloatValue(out var pause))
            {
                throw new ArgumentException("Main parameter must be a float");
            }

            return new PauseTypewriterTag(pause);
        }

        public override TextUnit CloseTag(string tagName)
        {
            return null;
        }
    }
}
