using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    internal class ResolvedStyleData
    {
        public float fontSize { get; set; }
        public TextAnchor unityTextAlign { get; set; }
        public WhiteSpace whiteSpace { get; set; }
        public int fontAscent { get; set; }
        public int fontBaseSize { get; set; }

        #if UNITY_6000_3_OR_NEWER
        public Material unityMaterial { get; set; }
        #endif

        public ResolvedStyleData() { }

        public static ResolvedStyleData TestData(float fontSize, int fontAscent, int fontBaseSize)
        {
            return new ResolvedStyleData
            {
                fontSize = fontSize,
                fontAscent = fontAscent,
                fontBaseSize = fontBaseSize,
            };
        }
    }
}
