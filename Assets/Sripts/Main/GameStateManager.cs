using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public bool IsGamePaused { get; private set; }
    public bool IsChoosingUpgrade { get; private set; }

    public event Action<bool> PauseStateChanged;
    public event Action<bool> ChoosingUpgradeChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGameState();
        StartCoroutine(DelayedBroadcastOneFrame());
    }

    void ResetGameState()
    {
        IsGamePaused = false;
        IsChoosingUpgrade = false;
        Time.timeScale = 1f;
        var hasMainMenu = FindObjectOfType<MainMenu>() != null;
        Cursor.visible = hasMainMenu;
        Cursor.lockState = hasMainMenu ? CursorLockMode.None : CursorLockMode.Confined;

    }

    IEnumerator DelayedBroadcastOneFrame()
    {
        yield return null;

        SettingsPanelManager.EnsureInstanceExists();

        var mainMenu = FindObjectOfType<MainMenu>();
        if (mainMenu != null)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        PauseStateChanged?.Invoke(IsGamePaused);
        ChoosingUpgradeChanged?.Invoke(IsChoosingUpgrade);
    }



    public void SetGameState(bool paused, bool choosingUpgrade)
    {
        IsGamePaused = paused;
        IsChoosingUpgrade = choosingUpgrade;
        ApplyState();
        PauseStateChanged?.Invoke(IsGamePaused);
        ChoosingUpgradeChanged?.Invoke(IsChoosingUpgrade);
    }

    void ApplyState()
    {
        Time.timeScale = IsGamePaused ? 0f : 1f;
        Cursor.visible = IsGamePaused || IsChoosingUpgrade;
        Cursor.lockState = Cursor.visible ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

    public void EnterChoosingUpgrade() => SetGameState(true, true);
    public void ExitChoosingUpgrade() => SetGameState(false, false);
    public void PauseGame() => SetGameState(true, false);
    public void ResumeGame() => SetGameState(false, false);
}
