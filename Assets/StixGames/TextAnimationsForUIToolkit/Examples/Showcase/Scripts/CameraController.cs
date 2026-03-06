using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [Min(0.01f)]
        public float blendDuration;

        public bool IsBlending => _blendProgress < 1;

        private float _blendProgress;
        private static readonly List<ShowcaseVirtualCamera> _cameras = new();

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private ShowcaseVirtualCamera _currentCamera;

        public static void RegisterVirtualCamera(ShowcaseVirtualCamera virtualCamera)
        {
            _cameras.Add(virtualCamera);
        }

        public static void UnregisterVirtualCamera(ShowcaseVirtualCamera virtualCamera)
        {
            _cameras.Remove(virtualCamera);
        }

        private void Update()
        {
            var priorityCamera = _cameras.OrderByDescending(x => x.priority).FirstOrDefault();
            if (priorityCamera == null)
            {
                return;
            }

            if (priorityCamera != _currentCamera)
            {
                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                _currentCamera = priorityCamera;
                _blendProgress = 0;
            }

            var isAtGoal = true;
            if (Vector3.Distance(priorityCamera.transform.position, transform.position) < 0.01f)
            {
                transform.position = priorityCamera.transform.position;
            }
            else
            {
                isAtGoal = false;
            }

            if (Quaternion.Angle(priorityCamera.transform.rotation, transform.rotation) < 0.1f)
            {
                transform.rotation = priorityCamera.transform.rotation;
            }
            else
            {
                isAtGoal = false;
            }

            if (isAtGoal)
            {
                _blendProgress = 1;
                return;
            }

            _blendProgress += Time.deltaTime / blendDuration;
            _blendProgress = Mathf.Clamp01(_blendProgress);

            var smoothProgress = Mathf.SmoothStep(0, 1, _blendProgress);
            transform.position = Vector3.Lerp(_lastPosition, priorityCamera.transform.position, smoothProgress);
            transform.rotation = Quaternion.Slerp(_lastRotation, priorityCamera.transform.rotation, smoothProgress);
        }
    }
}