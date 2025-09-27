using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject upgradePanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings References")]
    [SerializeField] private SettingsController settingsController;

    [Header("Level to load")]
    [SerializeField] private string levelSceneName = "LevelForest";

    private void Start()
    {
        playButton.onClick.AddListener(StartGame);
        upgradeButton.onClick.AddListener(OpenUpgradePanel);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(QuitGame);

        mainPanel.SetActive(true);
        settingsPanel?.SetActive(false);
        upgradePanel?.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool settingsVisible = (settingsPanel != null && settingsPanel.activeSelf)
                                   || (SettingsPanelManager.Instance != null && SettingsPanelManager.Instance.IsVisible());

            if (settingsVisible)
            {
                CloseSettings();
            }
            else if (upgradePanel != null && upgradePanel.activeSelf)
            {
                CloseUpgradePanel();
            }
        }
    }

    private void StartGame()
    {
        GameStateManager.Instance?.EnterChoosingUpgrade();
        SceneManager.LoadScene(levelSceneName);
    }

    public void OpenUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            mainPanel.SetActive(false);
        }
    }

    public void CloseUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
            mainPanel.SetActive(true);
        }
    }

    public void OpenSettings()
    {
        SettingsPanelManager.EnsureInstanceExists();

        if (SettingsPanelManager.Instance != null)
        {
            void OnSettingsHidden()
            {
                mainPanel.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                SettingsPanelManager.Instance.OnHidden -= OnSettingsHidden;
            }

            SettingsPanelManager.Instance.OnHidden -= OnSettingsHidden;
            SettingsPanelManager.Instance.OnHidden += OnSettingsHidden;

            SettingsPanelManager.Instance.ShowForMainMenu();

            if (SettingsPanelManager.Instance.IsVisible())
            {
                mainPanel.SetActive(false);
            }
        }
        else if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            var inits = settingsPanel.GetComponentsInChildren<ISettingsUIInitializer>(true);
            foreach (var init in inits) init.InitializeUI();
            settingsController?.InitializeUI();
            mainPanel.SetActive(false);
        }
        
        if (settingsController == null && SettingsPanelManager.Instance != null)
            settingsController = SettingsPanelManager.Instance.GetController();

        settingsController?.InitializeUI();
        settingsController?.ResetDropdowns();
    }

    public void CloseSettings()
    {
        if (SettingsPanelManager.Instance != null)
        {
            SettingsPanelManager.Instance.Hide();
        }
        else if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        mainPanel.SetActive(true);
        PlayerPrefs.Save();
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}