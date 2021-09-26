using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Common;
using Lean.Touch;
using UnityEngine;

namespace Axie.Core
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float minFOV;
        [SerializeField] private float maxFOV;
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private float minimapMinFOV;
        [SerializeField] private float minimapMaxFOV;

        private float currentFOV;
        private float miniFOV;

        private LeanFinger dragFinger;

        private void Start()
        {
            LeanTouch.OnFingerDown += OnTouchDown;
            LeanTouch.OnFingerUp += OnTouchUp;
            LeanTouch.OnFingerUpdate += OnTouchUpdate;
            LeanTouch.OnGesture += OnGestures;
        }

        private void Update()
        {
            var finalDelta = LeanInput.GetMouseWheelDelta();
            if (finalDelta == 0)
                return;
            currentFOV += finalDelta;
            currentFOV = Mathf.Clamp(currentFOV, minFOV, maxFOV);

            miniFOV += finalDelta;
            miniFOV = Mathf.Clamp(miniFOV, minimapMinFOV, minimapMaxFOV);

            mainCamera.orthographicSize = currentFOV;
            minimapCamera.orthographicSize = miniFOV;
        }

        private void OnGestures(List<LeanFinger> fingers)
        {
            if (fingers.Count < 2 || fingers.Any(f => f.IsOverGui))
                return;

            var pinchScale = LeanGesture.GetPinchRatio();
            currentFOV *= pinchScale;
            currentFOV = Mathf.Clamp(currentFOV, minFOV, maxFOV);

            mainCamera.orthographicSize = currentFOV;
        }

        private void OnTouchUpdate(LeanFinger finger)
        {
            if (finger.IsOverGui)
                return;

            if (dragFinger != null && finger.Index != dragFinger.Index)
                return;

            if (LeanTouch.Fingers.Count > 1)
                return;

            var moveDelta = finger.GetWorldDelta(10, mainCamera);
            var pos = mainCamera.transform.position;
            pos += new Vector3(moveDelta.x * -1f, moveDelta.y * -1f, 0);
            mainCamera.transform.position = pos;
        }

        private void OnTouchUp(LeanFinger obj)
        {
            dragFinger = null;
        }

        private void OnTouchDown(LeanFinger finger)
        {
            if (finger.IsOverGui)
                return;

            dragFinger = finger;
        }
    }
}