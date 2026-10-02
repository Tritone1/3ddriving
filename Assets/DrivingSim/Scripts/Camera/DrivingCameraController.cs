using UnityEngine;
using UnityEngine.EventSystems;

namespace DrivingSim.CameraSystem
{
    public sealed class DrivingCameraController : MonoBehaviour
    {
        public enum ViewMode { Chase, Hood }

        [SerializeField] private Transform target;
        [SerializeField] private Transform chaseAnchor;
        [SerializeField] private Transform hoodAnchor;
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.12f;
        [SerializeField, Min(0f)] private float rotationSharpness = 10f;
        [Header("Chase Orbit")]
        [SerializeField, Min(1f)] private float chaseDistance = 4.3f;
        [SerializeField, Min(0f)] private float focusHeight = 1.05f;
        [SerializeField, Range(0f, 80f)] private float defaultPitch = 14f;
        [SerializeField, Range(0f, 80f)] private float minimumPitch = 5f;
        [SerializeField, Range(0f, 85f)] private float maximumPitch = 65f;
        [SerializeField, Min(1f)] private float minimumDistance = 3.2f;
        [SerializeField, Min(1f)] private float maximumDistance = 8f;
        [SerializeField, Range(0.05f, 1f)] private float dragSensitivity = 0.35f;
        [SerializeField, Range(0.001f, 0.05f)] private float pinchZoomSensitivity = 0.012f;
        [SerializeField, Range(30f, 90f)] private float chaseFieldOfView = 52f;
        [SerializeField, Range(30f, 90f)] private float hoodFieldOfView = 65f;
        [SerializeField] private ViewMode initialView;

        private Vector3 velocity;
        private UnityEngine.Camera controlledCamera;
        private float orbitYaw;
        private float orbitPitch;
        private float orbitDistance;
        private int orbitFingerId = -1;
        private bool mouseOrbitActive;

        public ViewMode CurrentView { get; private set; }

        private void Awake()
        {
            CurrentView = initialView;
            controlledCamera = GetComponent<UnityEngine.Camera>();
            orbitPitch = Mathf.Clamp(defaultPitch, minimumPitch, maximumPitch);
            orbitDistance = Mathf.Clamp(chaseDistance, minimumDistance, maximumDistance);
        }

        private void LateUpdate()
        {
            Transform anchor = CurrentView == ViewMode.Hood ? hoodAnchor : chaseAnchor;
            if (anchor == null) anchor = target;
            if (anchor == null) return;

            if (CurrentView == ViewMode.Hood)
            {
                SetFieldOfView(hoodFieldOfView);
                transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                return;
            }

            ReadOrbitInput();
            SetFieldOfView(chaseFieldOfView);

            // Orbit relative to the car's heading, while keeping a level horizon even
            // when the vehicle pitches or rolls over uneven terrain.
            float vehicleYaw = target != null ? target.eulerAngles.y : anchor.eulerAngles.y;
            Quaternion desiredRotation = Quaternion.Euler(orbitPitch, vehicleYaw + orbitYaw, 0f);
            Vector3 pivot = (target != null ? target.position : anchor.position) + Vector3.up * focusHeight;
            Vector3 desiredPosition = pivot - desiredRotation * Vector3.forward * orbitDistance;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, positionSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime));
        }

        public void SetTarget(Transform carRoot, Transform chase, Transform hood)
        {
            target = carRoot;
            chaseAnchor = chase;
            hoodAnchor = hood;
            orbitYaw = 0f;
            orbitPitch = Mathf.Clamp(defaultPitch, minimumPitch, maximumPitch);
            orbitDistance = Mathf.Clamp(chaseDistance, minimumDistance, maximumDistance);
        }

        public void ToggleView() => CurrentView = CurrentView == ViewMode.Chase ? ViewMode.Hood : ViewMode.Chase;

        public void ResetOrbit()
        {
            orbitYaw = 0f;
            orbitPitch = Mathf.Clamp(defaultPitch, minimumPitch, maximumPitch);
            orbitDistance = Mathf.Clamp(chaseDistance, minimumDistance, maximumDistance);
        }

        private void ReadOrbitInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount >= 2)
            {
                Touch first = Input.GetTouch(0);
                Touch second = Input.GetTouch(1);
                if (!IsPointerOverUi(first.fingerId) && !IsPointerOverUi(second.fingerId))
                {
                    Vector2 previousFirst = first.position - first.deltaPosition;
                    Vector2 previousSecond = second.position - second.deltaPosition;
                    float previousDistance = Vector2.Distance(previousFirst, previousSecond);
                    float currentDistance = Vector2.Distance(first.position, second.position);
                    orbitDistance = Mathf.Clamp(orbitDistance -
                        (currentDistance - previousDistance) * pinchZoomSensitivity,
                        minimumDistance, maximumDistance);
                }

                orbitFingerId = -1;
                return;
            }

            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (touch.phase == TouchPhase.Began && orbitFingerId < 0 && !IsPointerOverUi(touch.fingerId))
                    orbitFingerId = touch.fingerId;

                if (touch.fingerId != orbitFingerId) continue;
                if (touch.phase == TouchPhase.Moved) ApplyOrbit(touch.deltaPosition);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) orbitFingerId = -1;
            }

            if (Input.touchCount == 0)
            {
                orbitFingerId = -1;
                if (Input.GetMouseButtonDown(0)) mouseOrbitActive = !IsPointerOverUi();
                if (Input.GetMouseButtonUp(0)) mouseOrbitActive = false;
                if (mouseOrbitActive && Input.GetMouseButton(0))
                    ApplyOrbit(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 12f);

                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f)
                    orbitDistance = Mathf.Clamp(orbitDistance - wheel * 0.45f, minimumDistance, maximumDistance);
            }
#endif
        }

        private void ApplyOrbit(Vector2 delta)
        {
            orbitYaw = Mathf.Repeat(orbitYaw + delta.x * dragSensitivity + 180f, 360f) - 180f;
            orbitPitch = Mathf.Clamp(orbitPitch - delta.y * dragSensitivity, minimumPitch, maximumPitch);
        }

        private static bool IsPointerOverUi(int pointerId = -1)
        {
            if (EventSystem.current == null) return false;
            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private void SetFieldOfView(float targetFieldOfView)
        {
            if (controlledCamera == null) return;
            controlledCamera.fieldOfView = Mathf.Lerp(controlledCamera.fieldOfView, targetFieldOfView,
                1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }
    }
}
