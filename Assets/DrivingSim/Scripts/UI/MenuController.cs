using System.Collections.Generic;
using DrivingSim.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private SaveService saveService;
        private readonly List<GameObject> hiddenGameplayObjects = new List<GameObject>();
        private bool startupVisible;

        private void Start()
        {
            EnsureMainMenuButtons();
            if (mainMenu != null && mainMenu.activeSelf) ShowStartupMenu();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!startupVisible && Input.GetKeyDown(KeyCode.Escape)) SetPaused(Time.timeScale > 0f);
#endif
        }

        public void Play()
        {
            startupVisible = false;
            if (mainMenu != null) mainMenu.SetActive(false);
            foreach (GameObject item in hiddenGameplayObjects)
                if (item != null) item.SetActive(true);
            hiddenGameplayObjects.Clear();
            SetPaused(false);
        }
        public void OpenGarage()
        {
            saveService?.Save();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene("Garage");
        }
        public void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            if (pauseMenu != null) pauseMenu.SetActive(paused);
            AudioListener.pause = paused;
        }
        public void ToggleSettings(bool visible)
        {
            if (settingsPanel != null) settingsPanel.SetActive(visible);
        }
        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public void Quit()
        {
            saveService?.Save();
            Application.Quit();
        }

        private void EnsureMainMenuButtons()
        {
            if (mainMenu == null || mainMenu.transform.Find("Garage") != null) return;
            Button playButton = mainMenu.GetComponentInChildren<Button>(true);
            if (playButton == null) return;

            Text playLabel = playButton.GetComponentInChildren<Text>(true);
            if (playLabel != null) playLabel.text = "START";
            RectTransform playRect = playButton.GetComponent<RectTransform>();
            if (playRect != null) playRect.anchoredPosition = new Vector2(0f, 25f);

            GameObject garageObject = Instantiate(playButton.gameObject, mainMenu.transform);
            garageObject.name = "Garage";
            RectTransform garageRect = garageObject.GetComponent<RectTransform>();
            if (garageRect != null) garageRect.anchoredPosition = new Vector2(0f, -65f);
            Text garageLabel = garageObject.GetComponentInChildren<Text>(true);
            if (garageLabel != null) garageLabel.text = "GARAGE";
            Button garageButton = garageObject.GetComponent<Button>();
            garageButton.onClick = new Button.ButtonClickedEvent();
            garageButton.onClick.AddListener(OpenGarage);

            RectTransform menuRect = mainMenu.GetComponent<RectTransform>();
            if (menuRect != null && menuRect.sizeDelta.y < 420f)
                menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, 420f);
        }

        private void ShowStartupMenu()
        {
            startupVisible = true;
            hiddenGameplayObjects.Clear();
            foreach (Transform child in transform)
            {
                GameObject item = child.gameObject;
                if (item == mainMenu || item == pauseMenu || item == settingsPanel || !item.activeSelf) continue;
                hiddenGameplayObjects.Add(item);
                item.SetActive(false);
            }

            RectTransform menuRect = mainMenu.GetComponent<RectTransform>();
            if (menuRect != null)
            {
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.pivot = new Vector2(0.5f, 0.5f);
                menuRect.anchoredPosition = Vector2.zero;
                menuRect.sizeDelta = Vector2.zero;
            }
            Image background = mainMenu.GetComponent<Image>();
            if (background != null) background.color = new Color(0.015f, 0.02f, 0.035f, 1f);
            mainMenu.transform.SetAsLastSibling();
            mainMenu.SetActive(true);
            if (pauseMenu != null) pauseMenu.SetActive(false);
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
    }
}
