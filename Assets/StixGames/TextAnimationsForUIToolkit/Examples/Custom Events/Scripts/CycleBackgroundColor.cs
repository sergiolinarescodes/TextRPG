using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit.Examples.Custom_Events.Scripts
{
    public class ChangeBackgroundColor : MonoBehaviour
    {
        public UIDocument uiDocument;

        public List<Color> colors;

        private VisualElement _background;

        private int _nextColor;

        private void OnEnable()
        {
            _background = uiDocument.rootVisualElement.Q("root");
            _nextColor = 0;
            CycleColor();
        }

        public void CycleColor()
        {
            _background.style.backgroundColor = colors[_nextColor];
            _nextColor = (_nextColor + 1) % colors.Count;
        }
    }
}
