using TextAnimationsForUIToolkit.Animations;
using TextAnimationsForUIToolkit.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit.Examples.Dynamic_Animations.Scripts
{
    public class ButtonAmplitudeAnimator : MonoBehaviour
    {
        public UIDocument document;
        public string targetButton;

        public bool resetTimeOnClick;
        public float targetTime;

        public AnimationCurve amplitudeAfterClick;

        private AnimatedButton animatedButton;
        private float lastButtonClickTime = -1;

        private void OnEnable()
        {
            animatedButton = document.rootVisualElement.Q<AnimatedButton>(targetButton);
            animatedButton.clicked += OnButtonClicked;
        }

        private void OnDisable()
        {
            animatedButton.clicked -= OnButtonClicked;
        }

        private void OnButtonClicked()
        {
            lastButtonClickTime = Time.time;

            if (resetTimeOnClick)
            {
                animatedButton.SetTime(targetTime);
            }
        }

        private void Update()
        {
            if (
                animatedButton == null
                || animatedButton.textUnits.Count == 0
                || lastButtonClickTime < 0
            )
            {
                return;
            }

            // The animation object is shared between the letters, so it's fine to only change the first letter.
            var firstLetter = (Letter)animatedButton.textUnits[0];
            var animation = (IAmplitude)firstLetter.animations[0];

            var newAmplitude = amplitudeAfterClick.Evaluate(Time.time - lastButtonClickTime);
            animation.amplitude = newAmplitude;
        }
    }
}
