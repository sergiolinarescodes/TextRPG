using System;
using TextAnimationsForUIToolkit;
using TextAnimationsForUIToolkit.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class ExampleDialogue : MonoBehaviour
    {
        [Serializable]
        public class DialogueLine
        {
            [TextArea]
            public string text;
            public bool isLeftSide;
            public bool useTextVanishing;
        }

        [SerializeField]
        private UIDocument uiDocument;

        [SerializeField]
        private TextAnimationSettings settings;

        [SerializeField]
        private string dialogueParent;

        [SerializeField]
        private string speechBubble;

        [SerializeField]
        private string isLeftClass;

        [SerializeField]
        private string nextButton;

        [SerializeField]
        private Transform leftCharacterPosition;

        [SerializeField]
        private Transform rightCharacterPosition;

        [SerializeField]
        private DialogueLine[] dialogue;

        private VisualElement _dialogueParent;
        private IAnimatedTextElement _speechBubble;
        private Button _nextButton;

        private int _dialogueIndex;

        private bool _isLeft;

        public event Action dialogueFinished;

        private void OnEnable()
        {
            var animatedTextElement = TextAnimationUtility.GetAnimatedTextElement(
                uiDocument,
                speechBubble,
                this
            );

            if (animatedTextElement == null)
            {
                return;
            }

            _dialogueParent = uiDocument.rootVisualElement.Q(dialogueParent);
            _speechBubble = animatedTextElement;
            _nextButton = uiDocument.rootVisualElement.Q<Button>(nextButton);

            if (_nextButton == null)
            {
                return;
            }

            Reset();
            _nextButton.clicked += OnClicked;
            _speechBubble.textAppearanceFinished += OnTextAppearanceFinished;

            StartNextDialogueLine();
        }

        public void Reset()
        {
            _dialogueIndex = 0;
        }

        private void OnDisable()
        {
            _dialogueParent.visible = false;
            _nextButton.clicked -= OnClicked;
            _speechBubble.textAppearanceFinished -= OnTextAppearanceFinished;
        }

        private void OnClicked()
        {
            if (_speechBubble.isAppearing)
            {
                _speechBubble.Skip();
                return;
            }

            StartNextDialogueLine();
        }

        private void OnTextAppearanceFinished(TextAppearanceFinishedEvent obj)
        {
            if (_dialogueIndex < dialogue.Length)
            {
                _nextButton.text = "Next";
            }
            else
            {
                _nextButton.text = "Exit";
            }
        }

        private void StartNextDialogueLine()
        {
            if (_dialogueIndex >= dialogue.Length)
            {
                dialogueFinished?.Invoke();
                return;
            }

            _dialogueParent.visible = false;
            _nextButton.text = "Skip";

            var dialogueLine = dialogue[_dialogueIndex];
            if (dialogueLine.isLeftSide)
            {
                ((VisualElement)_speechBubble).AddToClassList(isLeftClass);
                _isLeft = true;
            }
            else
            {
                ((VisualElement)_speechBubble).RemoveFromClassList(isLeftClass);
                _isLeft = false;
            }

            settings.enableTextVanishing = dialogueLine.useTextVanishing;

            _speechBubble.text = dialogueLine.text;

            _dialogueIndex++;
        }

        private void Update()
        {
            var position = _isLeft
                ? leftCharacterPosition.position
                : rightCharacterPosition.position;

            MoveCenterToWorldPosition(
                position,
                _dialogueParent,
                new Vector2(0, -0.5f),
                Vector2.zero
            );
        }

        /// <summary>
        /// Move the element so its center is at the target position. Optionally offset it by its width and height.
        /// </summary>
        public void MoveCenterToWorldPosition(
            Vector3 targetPosition,
            VisualElement element,
            Vector2 normalizedOffset = default,
            Vector2 absoluteOffset = default
        )
        {
            var screenPoint = RuntimePanelUtils.CameraTransformWorldToPanel(
                element.panel,
                targetPosition,
                Camera.main
            );

            var worldSize = element.worldBound;

            if (worldSize.width < 1)
            {
                return;
            }

            element.style.position = Position.Absolute;
            element.style.left =
                screenPoint.x
                - worldSize.width * 0.5f
                + worldSize.width * normalizedOffset.x
                + absoluteOffset.x;
            element.style.top =
                screenPoint.y
                - worldSize.height * 0.5f
                + worldSize.height * normalizedOffset.y
                + absoluteOffset.y;
            element.style.right = StyleKeyword.Auto;
            element.style.bottom = StyleKeyword.Auto;

            element.visible = true;
        }
    }
}
