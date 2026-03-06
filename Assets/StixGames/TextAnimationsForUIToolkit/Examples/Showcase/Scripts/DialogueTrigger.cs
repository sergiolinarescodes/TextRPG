using UnityEngine;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class DialogueTrigger : MonoBehaviour
    {
        public GameObject dialogueNotSeenInidicator;
        public GameObject virtualCamera;
        public ExampleDialogue dialogue;

        private DialogueUI _dialogueUI;

        private void Start()
        {
            _dialogueUI = gameObject.GetComponentInParent<DialogueUI>();
        }

        private void OnEnable()
        {
            dialogue.dialogueFinished += EndDialogue;
        }

        private void OnDisable()
        {
            dialogue.dialogueFinished -= EndDialogue;
        }

        public void StartDialogue()
        {
            dialogueNotSeenInidicator.SetActive(false);
            virtualCamera.SetActive(true);
            _dialogueUI.StartDialogue(dialogue);
        }

        private void EndDialogue()
        {
            virtualCamera.SetActive(false);
        }
    }
}
