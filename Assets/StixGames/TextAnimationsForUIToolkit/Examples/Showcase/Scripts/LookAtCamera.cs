using UnityEngine;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class LookAtCamera : MonoBehaviour
    {
        private void Update()
        {
            if (Camera.main == null)
            {
                return;
            }

            var towardsCamera = Camera.main.transform.position - transform.position;
            towardsCamera.y = 0;

            transform.rotation = Quaternion.LookRotation(towardsCamera);
        }
    }
}
