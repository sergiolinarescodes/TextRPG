using UnityEngine;

namespace TextAnimationsForUIToolkit.Utility
{
    /// <summary>
    /// Animation Math
    /// </summary>
    internal static class AMath
    {
        /// <summary>
        /// 2 * PI, a full circle in radians.
        /// </summary>
        public const float Tau = 2 * Mathf.PI;

        /// <summary>
        /// Replaces special float values (NaN, positive & negative infinity) with a replacement value.
        /// </summary>
        /// <returns></returns>
        public static  float FixFloat(float value, float replacement = 0f)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return replacement;
            }

            return value;
        }
    }
}
