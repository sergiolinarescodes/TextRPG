using UnityEngine;
using UnityEngine.InputSystem;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class DialogueRaycast : MonoBehaviour
    {
        public InputActionReference clickAction;
        public InputActionReference cursorPosition;

        private void OnEnable()
        {
            clickAction.action.Enable();
            cursorPosition.action.Enable();

            clickAction.action.performed += OnClickPerformed;
        }

        private void OnDestroy()
        {
            clickAction.action.Disable();
            cursorPosition.action.Disable();
        }

        private void OnClickPerformed(InputAction.CallbackContext obj)
        {
            if (Camera.main == null)
            {
                return;
            }

            var screenPosition = cursorPosition.action.ReadValue<Vector2>();
            var ray = Camera.main.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out var hit))
            {
                return;
            }

            var trigger = hit.collider.GetComponentInParent<DialogueTrigger>();
            if (trigger == null)
            {
                return;
            }

            trigger.StartDialogue();
        }
    }
}
