using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    /// <summary>Four-position automatic transmission selector for touch screens.</summary>
    public sealed class GearSelectorUI : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private MobileInputState input;
        [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.28f, 0.02f, 0.5f);
        private Button park;
        private Button reverse;
        private Button neutral;
        private Button drive;
        private MobileInputState.TransmissionGear lastGear = (MobileInputState.TransmissionGear)(-1);

        private void Awake() => FindButtons();

        private void OnEnable()
        {
            FindButtons();
            AddListeners();
            Refresh(true);
        }

        private void OnDisable() => RemoveListeners();

        private void Update() => Refresh(false);

        public void OnPointerDown(PointerEventData eventData) => SelectFromPointer(eventData);
        public void OnDrag(PointerEventData eventData) => SelectFromPointer(eventData);

        public void Configure(MobileInputState controls)
        {
            input = controls;
            Refresh(true);
        }

        private void FindButtons()
        {
            park = FindButton("P");
            reverse = FindButton("R");
            neutral = FindButton("N");
            drive = FindButton("D");
        }

        private Button FindButton(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void AddListeners()
        {
            RemoveListeners();
            park?.onClick.AddListener(SelectPark);
            reverse?.onClick.AddListener(SelectReverse);
            neutral?.onClick.AddListener(SelectNeutral);
            drive?.onClick.AddListener(SelectDrive);
        }

        private void RemoveListeners()
        {
            park?.onClick.RemoveListener(SelectPark);
            reverse?.onClick.RemoveListener(SelectReverse);
            neutral?.onClick.RemoveListener(SelectNeutral);
            drive?.onClick.RemoveListener(SelectDrive);
        }

        private void SelectPark() => Select(MobileInputState.TransmissionGear.Park);
        private void SelectReverse() => Select(MobileInputState.TransmissionGear.Reverse);
        private void SelectNeutral() => Select(MobileInputState.TransmissionGear.Neutral);
        private void SelectDrive() => Select(MobileInputState.TransmissionGear.Drive);

        private void Select(MobileInputState.TransmissionGear gear)
        {
            input?.SetGear((int)gear);
            Refresh(true);
        }

        private void SelectFromPointer(PointerEventData eventData)
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) return;

            // Coordinates match the four printed positions on AutomaticShifter.png.
            float[] positions = { 17f, -20f, -58f, -96f };
            int nearest = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < positions.Length; i++)
            {
                float distance = Mathf.Abs(localPoint.y - positions[i]);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = i;
            }
            Select((MobileInputState.TransmissionGear)nearest);
        }

        private void Refresh(bool force)
        {
            if (input == null) return;
            MobileInputState.TransmissionGear gear = input.SelectedGear;
            if (!force && gear == lastGear) return;
            lastGear = gear;
            SetButtonColor(park, gear == MobileInputState.TransmissionGear.Park);
            SetButtonColor(reverse, gear == MobileInputState.TransmissionGear.Reverse);
            SetButtonColor(neutral, gear == MobileInputState.TransmissionGear.Neutral);
            SetButtonColor(drive, gear == MobileInputState.TransmissionGear.Drive);
        }

        private void SetButtonColor(Button button, bool selected)
        {
            if (button == null || button.targetGraphic == null) return;
            button.targetGraphic.color = selected ? selectedColor : normalColor;
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = selected ? Color.black : Color.white;
        }
    }
}
