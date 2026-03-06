using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    public static class TextAnimationUtility
    {
        /// <summary>
        /// A helper function that looks for a visual element with the name <c>animatedTextName</c>,
        /// then returns it as a <c>IAnimatedTextElement</c>.
        /// <p>If you need a specific type of text element, use <c>VisualElement</c>'s <c>Q&lt;AnimatedLabel&gt;(name)</c> instead.</p>
        /// </summary>
        /// <param name="uiDocument">The target UI Document.</param>
        /// <param name="animatedTextName">The name of the target animated text element.</param>
        /// <param name="context">The context of this function call, used for providing context to text messages.</param>
        /// <returns></returns>
        public static IAnimatedTextElement GetAnimatedTextElement(
            UIDocument uiDocument,
            string animatedTextName,
            Object context = null
        )
        {
            if (uiDocument == null)
            {
                Debug.LogError(
                    "UIDocument is null, the audio emitter will not emit any sounds.",
                    context
                );
                return null;
            }

            var target = uiDocument.rootVisualElement.Q(animatedTextName);

            if (target == null)
            {
                Debug.LogError($"No visual element with ID {animatedTextName} exists.", context);
                return null;
            }

            if (target is not IAnimatedTextElement animatedTextElement)
            {
                Debug.LogError(
                    $"The visual element with ID {animatedTextName} is not an animated text element.",
                    context
                );
                return null;
            }

            return animatedTextElement;
        }
    }
}
