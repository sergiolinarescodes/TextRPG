using System.Text;

namespace TextAnimationsForUIToolkit.BuiltinTags
{
    public class RichTextTag
    {
        public string tag { get; }
        public Parameters parameters { get; }

        public RichTextTag(string tag, Parameters parameters)
        {
            this.tag = tag;
            this.parameters = parameters;
        }

        internal void BuildOpenTag(StringBuilder builder)
        {
            builder.Append('<');
            builder.Append(tag);
            parameters.BuildString(builder);
            builder.Append('>');
        }

        internal void BuildCloseTag(StringBuilder builder)
        {
            builder.Append("</");
            builder.Append(tag);
            builder.Append('>');
        }
    }
}
