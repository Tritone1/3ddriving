using UnityEngine;
using UnityEngine.EventSystems;

namespace DrivingSim.UI
{
    public sealed class SteeringWheelUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private MobileInputState input;
        [SerializeField] private RectTransform wheelGraphic;
        [SerializeField, Range(45f, 360f)] private float maximumRotation = 150f;
        [SerializeField, Min(0f)] private float returnSpeed = 8f;
        private float value;
        private bool dragging;

        private void Update()
        {
            if (!dragging) value = Mathf.MoveTowards(value, 0f, returnSpeed * Time.unscaledDeltaTime);
            ApplyValue();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            UpdateDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData) => UpdateDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => dragging = false;

        private void UpdateDrag(PointerEventData eventData)
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            float angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg - 90f;
            value = Mathf.Clamp(angle / maximumRotation, -1f, 1f);
            ApplyValue();
        }

        private void ApplyValue()
        {
            if (wheelGraphic != null) wheelGraphic.localRotation = Quaternion.Euler(0f, 0f, -value * maximumRotation);
            input?.SetWheelSteering(value);
        }
    }
}
