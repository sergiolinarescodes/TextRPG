using UnityEngine;
using UnityEngine.UIElements;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class DialogueUI : MonoBehaviour
    {
        public UIDocument uiDocument;

        public CameraController cameraController;

        private VisualElement _infoLabel;

        private bool _isBlending;
        private ExampleDialogue _target;

        private void Awake()
        {
            _infoLabel = uiDocument.rootVisualElement.Q("info");
        }

        private void Update()
        {
            if (_isBlending)
            {
                if (!cameraController.IsBlending)
                {
                    _isBlending = false;
                    SetActive();
                }
            }
            else if (cameraController.IsBlending)
            {
                _isBlending = true;
            }
        }

        private void SetActive()
        {
            if (_target == null)
            {
                return;
            }

            _target.gameObject.SetActive(true);
        }

        public void StartDialogue(ExampleDialogue target)
        {
            if (_target != null)
            {
                return;
            }

            _infoLabel.visible = false;
            _target = target;
            _target.dialogueFinished += ExitDialogue;
        }

        private void ExitDialogue()
        {
            _target.dialogueFinished -= ExitDialogue;

            _infoLabel.visible = true;
            _target.gameObject.SetActive(false);
            _target = null;
        }
    }
}
