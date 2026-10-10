using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    /// <summary>Four-position automatic transmission selector for touch screens.</summary>
    public sealed class GearSelectorUI : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private static readonly float[] GearYPositions = { 75f, 25f, -25f, -75f };
        [SerializeField] private MobileInputState input;
        [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.28f, 0.02f, 0.5f);
        private Button park;
        private Button reverse;
        private Button neutral;
        private Button drive;
        private RectTransform lever;
        private float animationPulse;
        private MobileInputState.TransmissionGear lastGear = (MobileInputState.TransmissionGear)(-1);

        private void Awake()
        {
            EnsureAnimatedVisuals();
            FindButtons();
        }

        private void OnEnable()
        {
            EnsureAnimatedVisuals();
            FindButtons();
            AddListeners();
            Refresh(true);
        }

        private void OnDisable() => RemoveListeners();

        private void Update()
        {
            Refresh(false);
            AnimateLever();
        }

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

            int nearest = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < GearYPositions.Length; i++)
            {
                float distance = Mathf.Abs(localPoint.y - GearYPositions[i]);
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
            animationPulse = 1f;
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

        private void AnimateLever()
        {
            if (lever == null || input == null) return;
            int gearIndex = Mathf.Clamp((int)input.SelectedGear, 0, GearYPositions.Length - 1);
            Vector2 target = new Vector2(-35f, GearYPositions[gearIndex]);
            float blend = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            lever.anchoredPosition = Vector2.Lerp(lever.anchoredPosition, target, blend);
            float targetAngle = Mathf.Lerp(-7f, 7f, gearIndex / 3f);
            lever.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.LerpAngle(lever.localEulerAngles.z, targetAngle, blend));
            animationPulse = Mathf.MoveTowards(animationPulse, 0f, Time.unscaledDeltaTime * 4f);
            float scale = 1f + animationPulse * 0.08f;
            lever.localScale = new Vector3(scale, scale, 1f);
        }

        private void EnsureAnimatedVisuals()
        {
            RectTransform root = transform as RectTransform;
            if (root == null) return;
            root.sizeDelta = new Vector2(220f, 300f);
            root.anchoredPosition = new Vector2(-150f, 280f);

            Image rootArtwork = GetComponent<Image>();
            if (rootArtwork != null)
            {
                rootArtwork.sprite = null;
                rootArtwork.color = Color.clear;
                rootArtwork.raycastTarget = false;
            }
            Transform oldArtwork = transform.Find("Artwork");
            if (oldArtwork != null) oldArtwork.gameObject.SetActive(false);

            RectTransform plate = EnsureImage("AnimatedPlate", transform, Vector2.zero,
                new Vector2(170f, 250f), new Color(0.025f, 0.03f, 0.045f, 0.96f));
            plate.SetAsFirstSibling();
            EnsureImage("Track", plate, new Vector2(-35f, 0f), new Vector2(18f, 185f),
                new Color(0.005f, 0.007f, 0.012f, 1f));

            for (int i = 0; i < GearYPositions.Length; i++)
                EnsureImage("Gate_" + i, plate, new Vector2(-12f, GearYPositions[i]),
                    new Vector2(46f, 5f), new Color(0.2f, 0.23f, 0.28f, 0.9f));

            Transform leverTransform = plate.Find("Lever");
            if (leverTransform == null)
            {
                lever = EnsureImage("Lever", plate, new Vector2(-35f, GearYPositions[3]),
                    new Vector2(14f, 52f), new Color(0.48f, 0.52f, 0.58f, 1f));
                RectTransform knob = EnsureImage("Knob", lever, new Vector2(0f, 31f),
                    new Vector2(58f, 46f), new Color(0.08f, 0.1f, 0.14f, 1f));
                Shadow shadow = knob.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                shadow.effectDistance = new Vector2(3f, -4f);
            }
            else lever = leverTransform as RectTransform;

            string[] names = { "P", "R", "N", "D" };
            for (int i = 0; i < names.Length; i++) EnsureGearButton(names[i], GearYPositions[i]);

            Transform interaction = transform.Find("InteractionArea");
            if (interaction != null)
            {
                RectTransform interactionRect = interaction as RectTransform;
                interactionRect.anchorMin = interactionRect.anchorMax = new Vector2(0.5f, 0.5f);
                interactionRect.anchoredPosition = Vector2.zero;
                interactionRect.sizeDelta = root.sizeDelta;
                interaction.SetAsLastSibling();
            }
        }

        private void EnsureGearButton(string gearName, float y)
        {
            Transform child = transform.Find(gearName);
            GameObject buttonObject;
            if (child == null)
            {
                buttonObject = new GameObject(gearName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
            }
            else buttonObject = child.gameObject;

            RectTransform rect = buttonObject.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(55f, y);
            rect.sizeDelta = new Vector2(52f, 38f);
            Image image = buttonObject.GetComponent<Image>();
            if (image == null) image = buttonObject.AddComponent<Image>();
            Button button = buttonObject.GetComponent<Button>();
            if (button == null) button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            Text label = buttonObject.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect = labelObject.transform as RectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 22;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
            }
            label.text = gearName;
        }

        private static RectTransform EnsureImage(string objectName, Transform parent, Vector2 position,
            Vector2 size, Color color)
        {
            Transform existing = parent.Find(objectName);
            GameObject imageObject;
            if (existing == null)
            {
                imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(parent, false);
            }
            else imageObject = existing.gameObject;

            RectTransform rect = imageObject.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
