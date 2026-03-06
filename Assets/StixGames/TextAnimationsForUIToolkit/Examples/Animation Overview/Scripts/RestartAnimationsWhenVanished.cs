using System.Collections;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit.Examples.Animation_Overview.Scripts
{
    public class RestartAnimationsWhenVanished : MonoBehaviour
    {
        public UIDocument uiDocument;
        public string parent;
        public float pauseBetweenRepetitions = 0.5f;

        private List<AnimatedLabel> _labels;
        private int _receivedEvents;

        private void OnEnable()
        {
            _labels = uiDocument.rootVisualElement.Q(parent).Query<AnimatedLabel>().ToList();
            _receivedEvents = 0;

            foreach (var label in _labels)
            {
                label.textVanishingFinished += OnVanishingFinished;
            }

            ResetAllLabels();
        }

        private void OnDisable()
        {
            foreach (var label in _labels)
            {
                label.textVanishingFinished -= OnVanishingFinished;
            }
        }

        private void OnVanishingFinished(TextVanishingFinishedEvent obj)
        {
            _receivedEvents++;

            if (_receivedEvents < _labels.Count)
            {
                return;
            }

            StartCoroutine(DelayedReset());
        }

        private IEnumerator DelayedReset()
        {
            yield return new WaitForSeconds(pauseBetweenRepetitions);
            _receivedEvents = 0;
            ResetAllLabels();
        }

        private void ResetAllLabels()
        {
            foreach (var label in _labels)
            {
                label.SetTime(0, true);
            }
        }
    }
}
