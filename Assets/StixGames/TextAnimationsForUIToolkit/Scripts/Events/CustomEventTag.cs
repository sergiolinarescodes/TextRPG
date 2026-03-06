using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;

namespace TextAnimationsForUIToolkit.TypewriterTags
{
    /// <summary>
    /// Emits a custom event.
    /// </summary>
    internal class CustomEventTag : TimedTextUnit
    {
        public string name { get; }

        public CustomEventTag(string name)
        {
            this.name = name;
        }
    }

    internal class CustomEventParser : ControlTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "event";
        }

        public override TextUnit OpenTag(string tagName, Parameters parameters)
        {
            if (parameters.mainParameter == null)
            {
                throw new ArgumentException("Main parameter is missing");
            }

            return new CustomEventTag(parameters.mainParameter);
        }

        public override TextUnit CloseTag(string tagName)
        {
            return null;
        }
    }
}
