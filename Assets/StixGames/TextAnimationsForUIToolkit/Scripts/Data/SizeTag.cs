namespace TextAnimationsForUIToolkit.Data
{
    public abstract class SizeTag
    {
        public bool isRelative;

        public float GetFontSize(float baseSize)
        {
            var fontSize = GetFontSizeInner(baseSize);

            if (isRelative)
            {
                return baseSize + fontSize;
            }

            return fontSize;
        }

        protected abstract float GetFontSizeInner(float baseSize);
    }

    public class AbsoluteSizeTag : SizeTag
    {
        public AbsoluteSizeTag(float absoluteSize)
        {
            this.absoluteSize = absoluteSize;
        }

        public float absoluteSize { get; set; }

        protected override float GetFontSizeInner(float baseSize)
        {
            return absoluteSize;
        }
    }

    public class FractionalSizeTag : SizeTag
    {
        public FractionalSizeTag(float fraction)
        {
            this.fraction = fraction;
        }

        public float fraction { get; set; }

        protected override float GetFontSizeInner(float baseSize)
        {
            return baseSize * fraction;
        }
    }
}