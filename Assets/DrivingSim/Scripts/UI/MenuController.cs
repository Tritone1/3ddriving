using DrivingSim.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrivingSim.UI
{
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private SaveService saveService;

        private void Start()
        {
#if UNITY_EDITOR
            // Device Simulator testing should begin immediately so a missed UI click
            // cannot leave physics frozen while keyboard input appears unresponsive.
            Play();
#else
            if (mainMenu != null && mainMenu.activeSelf) SetPaused(true);
#endif
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(Time.timeScale > 0f);
#endif
        }

        public void Play()
        {
            if (mainMenu != null) mainMenu.SetActive(false);
            SetPaused(false);
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
    }
}
