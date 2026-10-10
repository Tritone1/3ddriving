using System.Collections.Generic;
using DrivingSim.Garage;
using DrivingSim.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    public sealed class GarageView : MonoBehaviour
    {
        [SerializeField] private GarageController garage;
        [SerializeField] private EconomyService economy;
        [SerializeField] private Text carName;
        [SerializeField] private Text ownership;
        [SerializeField] private Text stats;
        [SerializeField] private Text message;
        [SerializeField] private Text money;
        [SerializeField] private Text buyButtonLabel;
        [SerializeField] private GameObject modifyPanel;

        private void OnEnable()
        {
            if (garage != null)
            {
                garage.SelectionChanged += Refresh;
                garage.TransactionFailed += ShowMessage;
            }
            if (economy != null) economy.BalanceChanged += HandleBalanceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (garage != null)
            {
                garage.SelectionChanged -= Refresh;
                garage.TransactionFailed -= ShowMessage;
            }
            if (economy != null) economy.BalanceChanged -= HandleBalanceChanged;
        }

        public void Next() => garage?.NextCar();
        public void Previous() => garage?.PreviousCar();
        public void BuyOrSelect()
        {
            if (garage == null) return;
            if (garage.IsOwned) garage.SelectCurrentCar(); else garage.BuyCurrentCar();
            Refresh();
        }
        public void BuyUpgrade(UpgradeData upgrade)
        {
            garage?.BuyUpgrade(upgrade);
            Refresh(upgrade);
        }
        public void SetPaintRed() => garage?.SetPaint(new Color(0.75f, 0.05f, 0.04f));
        public void SetPaintBlue() => garage?.SetPaint(new Color(0.04f, 0.18f, 0.75f));
        public void SetPaintWhite() => garage?.SetPaint(Color.white);
        public void ToggleModifyPanel()
        {
            if (modifyPanel == null || garage == null) return;
            if (!garage.IsOwned)
            {
                ShowMessage("Buy this car before modifying it.");
                return;
            }
            garage.SelectCurrentCar();
            modifyPanel.SetActive(!modifyPanel.activeSelf);
        }
        public void BackToMainMenu()
        {
            // The car being viewed is the player's intended driving car. Commit it
            // before changing scenes so the map always spawns the same selection.
            if (garage != null && garage.IsOwned) garage.SelectCurrentCar();
            Time.timeScale = 1f;
            SceneManager.LoadScene("Demo");
        }

        private void Refresh() => Refresh(null);
        private void Refresh(UpgradeData comparison)
        {
            if (garage == null || garage.CurrentCar == null) return;
            if (carName != null) carName.text = garage.CurrentCar.DisplayName;
            if (ownership != null) ownership.text = garage.IsSelected ? "SELECTED" : garage.IsOwned ? "OWNED" : $"${garage.CurrentCar.Price:N0}";
            if (buyButtonLabel != null) buyButtonLabel.text = garage.IsSelected ? "SELECTED" : garage.IsOwned ? "SELECT" : "BUY";
            if (money != null) money.text = $"${(economy != null ? economy.Balance : 0):N0}";
            CarStatSnapshot now = garage.GetCurrentStats();
            CarStatSnapshot after = comparison == null ? now : garage.GetStatsAfter(comparison);
            if (stats != null) stats.text = $"Power {now.Power:0.00} → {after.Power:0.00}\nGrip {now.Grip:0.00} → {after.Grip:0.00}\nBrakes {now.Brakes:0.00} → {after.Brakes:0.00}\nTank {now.FuelCapacity:0} → {after.FuelCapacity:0} L";
        }

        private void HandleBalanceChanged(int balance)
        {
            if (money != null) money.text = $"${balance:N0}";
        }

        public static GarageView CreateRuntime(GarageController controller, EconomyService wallet,
            IReadOnlyList<UpgradeData> upgrades)
        {
            GameObject canvasObject = new GameObject("GarageUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.SetActive(false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            CreateText(canvasObject.transform, "Heading", "GARAGE", new Vector2(0.5f, 1f), new Vector2(0f, -55f), 40, TextAnchor.MiddleCenter);
            Text moneyText = CreateText(canvasObject.transform, "Money", "$1,000", Vector2.one, new Vector2(-30f, -28f), 28, TextAnchor.MiddleRight);
            moneyText.rectTransform.pivot = Vector2.one;
            Text nameText = CreateText(canvasObject.transform, "CarName", "CAR", new Vector2(0.5f, 0f), new Vector2(0f, 265f), 32, TextAnchor.MiddleCenter);
            Text ownershipText = CreateText(canvasObject.transform, "Ownership", "", new Vector2(0.5f, 0f), new Vector2(0f, 220f), 21, TextAnchor.MiddleCenter);
            Text statsText = CreateText(canvasObject.transform, "Stats", "", new Vector2(0f, 0.5f), new Vector2(235f, 20f), 22, TextAnchor.MiddleLeft);
            statsText.rectTransform.sizeDelta = new Vector2(430f, 260f);
            Text messageText = CreateText(canvasObject.transform, "Message", "", new Vector2(0.5f, 0f), new Vector2(0f, 55f), 20, TextAnchor.MiddleCenter);

            Button previousButton = CreateButton(canvasObject.transform, "PreviousCar", "<", new Vector2(0f, 0.5f), new Vector2(75f, 0f), new Vector2(80f, 80f));
            Button nextButton = CreateButton(canvasObject.transform, "NextCar", ">", new Vector2(1f, 0.5f), new Vector2(-75f, 0f), new Vector2(80f, 80f));
            Button buyButton = CreateButton(canvasObject.transform, "Buy", "BUY", new Vector2(0.5f, 0f), new Vector2(-125f, 135f), new Vector2(220f, 65f));
            Button modifyButton = CreateButton(canvasObject.transform, "Modify", "MODIFY", new Vector2(0.5f, 0f), new Vector2(125f, 135f), new Vector2(220f, 65f));
            Button backButton = CreateButton(canvasObject.transform, "Back", "MAIN MENU", new Vector2(0f, 1f), new Vector2(110f, -55f), new Vector2(180f, 60f));

            GameObject panel = CreateImage(canvasObject.transform, "ModifyPanel", new Vector2(1f, 0.5f), new Vector2(-310f, 0f), new Vector2(500f, 620f), new Color(0.02f, 0.03f, 0.05f, 0.94f)).gameObject;
            CreateText(panel.transform, "Title", "MODIFY CAR", new Vector2(0.5f, 1f), new Vector2(0f, -45f), 28, TextAnchor.MiddleCenter);

            GarageView view = canvasObject.AddComponent<GarageView>();
            view.garage = controller;
            view.economy = wallet;
            view.carName = nameText;
            view.ownership = ownershipText;
            view.stats = statsText;
            view.message = messageText;
            view.money = moneyText;
            view.buyButtonLabel = buyButton.GetComponentInChildren<Text>();
            view.modifyPanel = panel;

            previousButton.onClick.AddListener(view.Previous);
            nextButton.onClick.AddListener(view.Next);
            buyButton.onClick.AddListener(view.BuyOrSelect);
            modifyButton.onClick.AddListener(view.ToggleModifyPanel);
            backButton.onClick.AddListener(view.BackToMainMenu);

            float y = 205f;
            if (upgrades != null)
            {
                foreach (UpgradeData upgrade in upgrades)
                {
                    UpgradeData selectedUpgrade = upgrade;
                    Button upgradeButton = CreateButton(panel.transform, "Upgrade_" + upgrade.Id,
                        $"{upgrade.DisplayName.ToUpperInvariant()}  ${upgrade.PriceForNextLevel(0):N0}",
                        new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(390f, 58f));
                    upgradeButton.onClick.AddListener(() => view.BuyUpgrade(selectedUpgrade));
                    y -= 68f;
                }
            }

            CreateText(panel.transform, "PaintTitle", "PAINT", new Vector2(0.5f, 0.5f), new Vector2(0f, -165f), 20, TextAnchor.MiddleCenter);
            CreateButton(panel.transform, "RedPaint", "RED", new Vector2(0.5f, 0f), new Vector2(-130f, 55f), new Vector2(110f, 52f)).onClick.AddListener(view.SetPaintRed);
            CreateButton(panel.transform, "BluePaint", "BLUE", new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(110f, 52f)).onClick.AddListener(view.SetPaintBlue);
            CreateButton(panel.transform, "WhitePaint", "WHITE", new Vector2(0.5f, 0f), new Vector2(130f, 55f), new Vector2(110f, 52f)).onClick.AddListener(view.SetPaintWhite);
            panel.SetActive(false);

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            canvasObject.SetActive(true);
            return view;
        }

        private static Text CreateText(Transform parent, string objectName, string value, Vector2 anchor,
            Vector2 position, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(420f, 80f);
            rect.anchoredPosition = position;
            Text textComponent = textObject.GetComponent<Text>();
            textComponent.text = value;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            return textComponent;
        }

        private static Image CreateImage(Transform parent, string objectName, Vector2 anchor,
            Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Button CreateButton(Transform parent, string objectName, string label,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(parent, objectName, anchor, position, size, new Color(0.12f, 0.35f, 0.68f, 0.95f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text labelText = CreateText(image.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, 20, TextAnchor.MiddleCenter);
            labelText.raycastTarget = false;
            return button;
        }

        private void ShowMessage(string value)
        {
            if (message != null) message.text = value;
        }
    }
}
