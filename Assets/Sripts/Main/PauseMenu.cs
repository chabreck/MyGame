using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject settingsSubPanel;
    [SerializeField] private GameObject confirmationPanel;

    [Header("Audio Source")]
    [SerializeField] private AudioSource uiAudioSource;

    [Header("Settings UI Elements")]
    [SerializeField] private SettingsController settingsController;

    [Header("General")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool _settingsHiddenSubscribed = false;

    private void Awake()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
            if (SettingsManager.Instance != null)
            {
                var sfxGroup = SettingsManager.Instance.GetSFXGroup();
                if (sfxGroup != null) uiAudioSource.outputAudioMixerGroup = sfxGroup;
            }
        }

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PauseStateChanged += OnPauseStateChanged;
            GameStateManager.Instance.ChoosingUpgradeChanged += OnChoosingUpgradeChanged;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
        RebindSceneUI();
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PauseStateChanged -= OnPauseStateChanged;
            GameStateManager.Instance.ChoosingUpgradeChanged -= OnChoosingUpgradeChanged;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneUI();
        OnPauseStateChanged(GameStateManager.Instance?.IsGamePaused ?? false);
    }
    
    private void RebindSceneUI()
    {
        if (pauseMenuUI == null)
        {
            pauseMenuUI = FindUIByNameOrTag("PauseMenuUI", "PauseMenu");
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        }

        if (settingsSubPanel == null)
        {
            settingsSubPanel = FindUIByNameOrTag("SettingsSubPanel", "SettingsPanel");
            if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
        }

        if (confirmationPanel == null)
        {
            confirmationPanel = FindUIByNameOrTag("ConfirmationPanel", null);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
        }
    }

    private GameObject FindUIByNameOrTag(string nameToFind, string tagToTry)
    {
        if (!string.IsNullOrEmpty(nameToFind))
        {
            var go = GameObject.Find(nameToFind);
            if (go != null) return go;
        }

        if (!string.IsNullOrEmpty(tagToTry))
        {
            try
            {
                var byTag = GameObject.FindWithTag(tagToTry);
                if (byTag != null) return byTag;
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(nameToFind))
        {
            var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allRoots)
            {
                var child = root.transform.Find(nameToFind);
                if (child != null) return child.gameObject;
            }
        }

        return null;
    }
    
    private void Update()
    {
        if (GameStateManager.Instance?.IsChoosingUpgrade ?? false) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool settingsVisible = (settingsSubPanel != null && settingsSubPanel.activeSelf)
                                   || (SettingsPanelManager.Instance != null && SettingsPanelManager.Instance.IsVisible());

            if (settingsVisible)
            {
                CloseSettingsSubPanel();
                return;
            }

            if (confirmationPanel != null && confirmationPanel.activeSelf)
            {
                CancelExitToMainMenu();
                return;
            }

            if (pauseMenuUI != null && pauseMenuUI.activeSelf)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        GameStateManager.Instance?.PauseGame();
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        settingsSubPanel?.SetActive(false);
        confirmationPanel?.SetActive(false);
        GameStateManager.Instance?.ResumeGame();
        PlayerPrefs.Save();
    }

    public void ShowExitConfirmation()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(true);
    }

    public void ConfirmExitToMainMenu()
    {
        StartCoroutine(LoadMainMenuAsync());
    }

    private IEnumerator LoadMainMenuAsync()
    {
#if !UNITY_WEBGL && !UNITY_IOS && !UNITY_ANDROID
        Time.timeScale = 1f;
#endif
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return null;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void CancelExitToMainMenu()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
    }

    public void OpenSettingsSubPanel()
    {

        SettingsPanelManager.EnsureInstanceExists();

        if (SettingsPanelManager.Instance != null)
        {
            if (!_settingsHiddenSubscribed)
            {
                SettingsPanelManager.Instance.OnHidden += OnSettingsHiddenFromPause;
                _settingsHiddenSubscribed = true;
            }

            SettingsPanelManager.Instance.ShowForPauseMenu();

            if (SettingsPanelManager.Instance.IsVisible())
            {
                if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
                GameStateManager.Instance?.PauseGame();
            }

            if (settingsController == null)
                settingsController = SettingsPanelManager.Instance.GetController();
            settingsController?.InitializeUI();
            settingsController?.ResetDropdowns(); // Сбрасываем Dropdown
        }
        else if (settingsSubPanel != null)
        {
            settingsSubPanel.SetActive(true);
            var inits = settingsSubPanel.GetComponentsInChildren<ISettingsUIInitializer>(true);
            foreach (var init in inits) init.InitializeUI();
            settingsController?.InitializeUI();
            settingsController?.ResetDropdowns(); // Сбрасываем Dropdown
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        }
        
        if (settingsController == null && SettingsPanelManager.Instance != null)
            settingsController = SettingsPanelManager.Instance.GetController();

        settingsController?.InitializeUI();
        settingsController?.ResetDropdowns(); // Сбрасываем Dropdown
    }

    private void OnSettingsHiddenFromPause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        GameStateManager.Instance?.PauseGame();

        if (SettingsPanelManager.Instance != null && _settingsHiddenSubscribed)
        {
            SettingsPanelManager.Instance.OnHidden -= OnSettingsHiddenFromPause;
            _settingsHiddenSubscribed = false;
        }
    }
    
    public void CloseSettingsSubPanel()
    {

        if (SettingsPanelManager.Instance != null)
        {
            SettingsPanelManager.Instance.Hide();
        }
        else
        {
            if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
            if (settingsController != null) settingsController.InitializeUI();
        }

        if (SettingsPanelManager.Instance != null && _settingsHiddenSubscribed)
        {
            SettingsPanelManager.Instance.OnHidden -= OnSettingsHiddenFromPause;
            _settingsHiddenSubscribed = false;
        }

        PlayerPrefs.Save();
    }

    private void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            if (pauseMenuUI != null && !pauseMenuUI.activeSelf) pauseMenuUI.SetActive(true);
        }
        else
        {
            if (pauseMenuUI != null && pauseMenuUI.activeSelf) pauseMenuUI.SetActive(false);
            settingsSubPanel?.SetActive(false);
            confirmationPanel?.SetActive(false);
        }
    }

    private void OnChoosingUpgradeChanged(bool isChoosing)
    {
        if (isChoosing)
        {
            if (pauseMenuUI != null && pauseMenuUI.activeSelf) pauseMenuUI.SetActive(false);
            settingsSubPanel?.SetActive(false);
            confirmationPanel?.SetActive(false);
        }
    }

    private void PlayUISound(AudioClip clip)
    {
        if (clip == null || uiAudioSource == null) return;
        float currentVolume = PlayerPrefs.GetFloat(SettingsManager.PREF_SFX, 0.75f);
        uiAudioSource.PlayOneShot(clip, currentVolume);
    }
}