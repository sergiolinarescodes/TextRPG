using UnityEngine;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class ShowcaseVirtualCamera : MonoBehaviour
    {
        public int priority = 0;

        private void OnEnable()
        {
            CameraController.RegisterVirtualCamera(this);
        }

        private void OnDisable()
        {
            CameraController.UnregisterVirtualCamera(this);
        }
    }
}