using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsController : MonoBehaviour, ISettingsUIInitializer
{
    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Localization")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private Resolution[] resolutions;
    private bool isInitializing = false;

    private void OnEnable()
    {
        SettingsManager.OnSettingsChanged += InitializeUI;
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        SettingsManager.OnSettingsChanged -= InitializeUI;
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    public void InitializeUI()
    {
        if (SettingsManager.Instance == null || isInitializing) return;
        
        isInitializing = true;
        
        try
        {
            resolutions = SettingsManager.Instance.GetFilteredResolutions();
            InitializeAudioSettings();
            InitializeDisplaySettings();
            InitializeLanguageDropdown();
        }
        finally
        {
            isInitializing = false;
        }
    }

    #region Audio
    private void InitializeAudioSettings()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.value = PlayerPrefs.GetFloat(SettingsManager.PREF_MUSIC, 0.75f);
            musicSlider.onValueChanged.AddListener(val => SettingsManager.Instance.SetMusicVolume(val));
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.value = PlayerPrefs.GetFloat(SettingsManager.PREF_SFX, 0.75f);
            sfxSlider.onValueChanged.AddListener(val => SettingsManager.Instance.SetSFXVolume(val));
        }
    }
    #endregion

    #region Display
    private void InitializeDisplaySettings()
    {
        if (fullscreenToggle != null)
        {
            Debug.Log("Initializing fullscreen toggle");
        
            fullscreenToggle.onValueChanged.RemoveAllListeners();
        
            bool isFullscreen = PlayerPrefs.GetInt(SettingsManager.PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            Debug.Log($"Setting initial fullscreen value: {isFullscreen}");
            fullscreenToggle.isOn = isFullscreen;
        
            fullscreenToggle.onValueChanged.AddListener(val => {
                Debug.Log($"Fullscreen toggle value changed to: {val}");
                if (!isInitializing && SettingsManager.Instance != null)
                {
                    Debug.Log("Calling SettingsManager.SetFullscreen");
                    SettingsManager.Instance.SetFullscreen(val);
                }
                else
                {
                    Debug.LogWarning("Cannot call SetFullscreen - isInitializing: " + isInitializing + ", SettingsManager.Instance: " + (SettingsManager.Instance != null));
                }
            });
        }

        if (resolutionDropdown != null)
        {
            InitializeResolutionDropdown();
        }
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null || resolutions == null) return;

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.ClearOptions();

        Resolution current = SettingsManager.Instance.GetCurrentResolution();
        List<string> options = new List<string>();
        int closestIndex = 0;
        int minDifference = int.MaxValue;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution res = resolutions[i];
            string optionText = $"{res.width}x{res.height}";
            options.Add(optionText);

            int diffWidth = Mathf.Abs(res.width - current.width);
            int diffHeight = Mathf.Abs(res.height - current.height);
            int totalDiff = diffWidth + diffHeight;

            if (totalDiff < minDifference)
            {
                minDifference = totalDiff;
                closestIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = Mathf.Clamp(closestIndex, 0, options.Count - 1);
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(index =>
        {
            if (isInitializing) return;
            Resolution selected = resolutions[index];
            SettingsManager.Instance.SetResolution(selected.width, selected.height, selected.refreshRate);
        });
    }
    #endregion

    #region Localization
    private void InitializeLanguageDropdown()
    {
        if (languageDropdown == null || LocalizationManager.Instance == null) return;

        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.ClearOptions();

        var languages = LocalizationManager.Instance.GetSupportedLanguages();
        List<string> options = new List<string>();
        foreach (var lang in languages) options.Add(lang.languageName);

        languageDropdown.AddOptions(options);

        int curr = LocalizationManager.Instance.GetCurrentLanguageIndex();
        if (curr < 0 || curr >= options.Count) curr = 0;
        languageDropdown.value = curr;
        languageDropdown.RefreshShownValue();

        languageDropdown.onValueChanged.AddListener(idx =>
        {
            if (isInitializing) return;
            LocalizationManager.Instance.SetLanguageByIndex(idx);
        });
    }

    private void OnLanguageChanged()
    {
        if (languageDropdown == null || LocalizationManager.Instance == null) return;
        int idx = LocalizationManager.Instance.GetCurrentLanguageIndex();
        if (idx < 0) idx = 0;
        
        isInitializing = true;
        try
        {
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.value = idx;
            languageDropdown.RefreshShownValue();
            languageDropdown.onValueChanged.AddListener(i => LocalizationManager.Instance.SetLanguageByIndex(i));
        }
        finally
        {
            isInitializing = false;
        }
    }
    #endregion
    
    public void ResetDropdowns()
    {
        if (resolutionDropdown != null)
        {
            resolutionDropdown.Hide();
            resolutionDropdown.interactable = false;
            resolutionDropdown.interactable = true;
        }
        
        if (languageDropdown != null)
        {
            languageDropdown.Hide();
            languageDropdown.interactable = false;
            languageDropdown.interactable = true;
        }
    }
}