using UnityEngine;
using UnityEngine.Audio;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections;
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    public static event Action OnSettingsChanged;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    private AudioSource sfxSource; 

    private const float MIN_DB = -80f;
    public const string PREF_MUSIC = "MusicVolume";
    public const string PREF_SFX = "SFXVolume";
    public const string PREF_FULLSCREEN = "FullscreenMode";
    public const string PREF_RES_WIDTH = "ResolutionWidth";
    public const string PREF_RES_HEIGHT = "ResolutionHeight";
    public const string PREF_RES_REFRESH = "ResolutionRefresh";

    private Resolution[] availableResolutions;
    public Resolution[] AvailableResolutions => availableResolutions;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private bool wasFullscreen;
    private bool ignoreNextFullscreenChange = false;

    private static readonly Vector2Int[] ALLOWED_RESOLUTIONS =
    {
        new Vector2Int(800, 1280),
        new Vector2Int(1280, 720),
        new Vector2Int(1280, 1024),
        new Vector2Int(1280, 800),
        new Vector2Int(1360, 768),
        new Vector2Int(1366, 768),
        new Vector2Int(1440, 900),
        new Vector2Int(1470, 956),
        new Vector2Int(1512, 982),
        new Vector2Int(1600, 900),
        new Vector2Int(1680, 1050),
        new Vector2Int(1920, 1080),
        new Vector2Int(1920, 1200),
        new Vector2Int(2560, 1440),
        new Vector2Int(2560, 1600),
        new Vector2Int(2560, 1080),
        new Vector2Int(2880, 1800),
        new Vector2Int(3440, 1440),
        new Vector2Int(3840, 2160),
        new Vector2Int(5120, 1440)
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSettings();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        // Обработка F11 вручную, если системная не работает
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ToggleFullscreen();
        }

        // Отслеживаем изменения размера окна
        if (lastScreenWidth != Screen.width || 
            lastScreenHeight != Screen.height || 
            wasFullscreen != Screen.fullScreen)
        {
            // Если изменение было вызвано F11, обновляем настройки
            if (wasFullscreen != Screen.fullScreen && !ignoreNextFullscreenChange)
            {
                Debug.Log($"[SettingsManager] Fullscreen mode changed via F11 or system. New value: {Screen.fullScreen}");
                PlayerPrefs.SetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
            
            OnWindowSizeChanged();
        }
        
        // Сбрасываем флаг игнорирования после одного кадра
        if (ignoreNextFullscreenChange)
        {
            ignoreNextFullscreenChange = false;
        }
    }

    private void ToggleFullscreen()
    {
        Debug.Log("[SettingsManager] F11 pressed, toggling fullscreen");
        bool newFullscreenState = !Screen.fullScreen;
        SetFullscreen(newFullscreenState);
    }

    private void OnWindowSizeChanged()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        wasFullscreen = Screen.fullScreen;

        // Обновляем сохраненные настройки если окно было изменено пользователем
        if (!wasFullscreen)
        {
            PlayerPrefs.SetInt(PREF_RES_WIDTH, Screen.width);
            PlayerPrefs.SetInt(PREF_RES_HEIGHT, Screen.height);
            PlayerPrefs.Save();
        }

        PlayerPrefs.SetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();

        UpdateCameraAspect();
        OnSettingsChanged?.Invoke();
        
        // Убедимся, что курсор правильно работает в оконном режиме
        EnsureCursorState();
    }
    
    private void EnsureCursorState()
    {
        // В оконном режиме убедимся, что курсор не заблокирован
        if (!Screen.fullScreen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        // В полноэкранном режиме можно заблокировать курсор, если нужно
        else
        {
            // Здесь можно добавить логику для блокировки курсора в полноэкранном режиме
            // если это необходимо для вашей игры
        }
    }

    private void InitializeSettings()
    {
        // создаём источник SFX если его нет
        if (sfxSource == null)
        {
            GameObject go = new GameObject("UI_SFX_Source");
            go.transform.SetParent(transform);
            sfxSource = go.AddComponent<AudioSource>();

            if (audioMixer != null)
            {
                var sfxGroup = GetSFXGroup();
                if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
            }
        }

        float musicVol = PlayerPrefs.GetFloat(PREF_MUSIC, 0.75f);
        float sfxVol = PlayerPrefs.GetFloat(PREF_SFX, 0.75f);
        SetMusicVolume(musicVol, false);
        SetSFXVolume(sfxVol, false);

        availableResolutions = GetFilteredAllowedResolutions();

        int width = PlayerPrefs.GetInt(PREF_RES_WIDTH, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(PREF_RES_HEIGHT, Screen.currentResolution.height);
        int refresh = PlayerPrefs.GetInt(PREF_RES_REFRESH, Screen.currentResolution.refreshRate);
        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;

        // Инициализация переменных для отслеживания изменений
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        wasFullscreen = Screen.fullScreen;

        ApplyResolution(width, height, refresh, isFullscreen);
        
        // Убедимся, что курсор правильно работает
        EnsureCursorState();
    }

    private Resolution[] GetFilteredAllowedResolutions()
    {
        List<Resolution> result = new List<Resolution>();
        Resolution[] systemResolutions = Screen.resolutions;

        foreach (var allowedRes in ALLOWED_RESOLUTIONS)
        {
            var matches = systemResolutions
                .Where(r => r.width == allowedRes.x && r.height == allowedRes.y)
                .ToArray();

            if (matches.Length > 0)
            {
                // Берем максимальную частоту обновления
                Resolution best = matches
                    .OrderByDescending(r => r.refreshRate)
                    .First();

                result.Add(best);
            }
            else
            {
                // Создаем искусственное разрешение с текущей частотой обновления
                result.Add(new Resolution {
                    width = allowedRes.x,
                    height = allowedRes.y,
                    refreshRate = Screen.currentResolution.refreshRate
                });
            }
        }

        // Сортируем по ширине, затем по высоте
        return result
            .OrderByDescending(r => r.width)
            .ThenByDescending(r => r.height)
            .ToArray();
    }

    public Resolution[] GetFilteredResolutions()
    {
        return availableResolutions;
    }

    private void ApplyResolution(int width, int height, int refreshRate, bool fullscreen = false)
    {
        // Ищем точное совпадение в доступных разрешениях
        Resolution targetRes = availableResolutions.FirstOrDefault(r =>
            r.width == width &&
            r.height == height);

        if (targetRes.width == 0)
        {
            // Если не нашли - используем первое разрешение из списка
            targetRes = availableResolutions[0];
        }
        else
        {
            // Используем оригинальную частоту обновления
            targetRes.refreshRate = refreshRate;
        }

        // Устанавливаем флаг игнорирования, чтобы не создавать циклическое обновление
        ignoreNextFullscreenChange = true;

        // Устанавливаем разрешение
        Screen.SetResolution(
            targetRes.width,
            targetRes.height,
            fullscreen,
            targetRes.refreshRate
        );

        // Сохраняем в PlayerPrefs
        PlayerPrefs.SetInt(PREF_RES_WIDTH, targetRes.width);
        PlayerPrefs.SetInt(PREF_RES_HEIGHT, targetRes.height);
        PlayerPrefs.SetInt(PREF_RES_REFRESH, targetRes.refreshRate);
        PlayerPrefs.SetInt(PREF_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        // Обновляем переменные отслеживания
        lastScreenWidth = targetRes.width;
        lastScreenHeight = targetRes.height;
        wasFullscreen = fullscreen;

        // Обновляем камеру
        UpdateCameraAspect();
        
        // Убедимся, что курсор правильно работает
        EnsureCursorState();
    }

    private void UpdateCameraAspect()
    {
        CameraAspectAdjuster adjuster = FindObjectOfType<CameraAspectAdjuster>();
        if (adjuster != null) adjuster.AdjustCamera();
    }

    #region Audio Methods
    public void SetMusicVolume(float volume, bool notify = true)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_MUSIC, volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float dB = (volume > 0.01f) ? 20f * Mathf.Log10(volume) : MIN_DB;
            audioMixer.SetFloat(musicVolumeParam, dB);
        }

        if (notify) OnSettingsChanged?.Invoke();
    }

    public void SetSFXVolume(float volume, bool notify = true)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_SFX, volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float dB = (volume > 0.01f) ? 20f * Mathf.Log10(volume) : MIN_DB;
            audioMixer.SetFloat(sfxVolumeParam, dB);
        }

        if (notify) OnSettingsChanged?.Invoke();
    }
    #endregion

    #region Display Settings
    public void SetFullscreen(bool isFullscreen)
    {
        Debug.Log($"[SettingsManager] SetFullscreen called with: {isFullscreen}");
    
        // Добавим небольшую задержку, чтобы убедиться, что Toggle успел обновиться
        StartCoroutine(SetFullscreenDelayed(isFullscreen));
    }

    private IEnumerator SetFullscreenDelayed(bool isFullscreen)
    {
        yield return null; // Ждем один кадр
    
        // ИСПРАВЛЕНО: получаем текущее разрешение и применяем с новым флагом полноэкранности
        Resolution current = GetCurrentResolution();
        Debug.Log($"[SettingsManager] Applying resolution: {current.width}x{current.height}@{current.refreshRate}Hz, fullscreen: {isFullscreen}");
        ApplyResolution(current.width, current.height, current.refreshRate, isFullscreen);
        OnSettingsChanged?.Invoke();
    }

    public void SetResolution(int width, int height, int refreshRate)
    {
        bool currentFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        ApplyResolution(width, height, refreshRate, currentFullscreen);
        OnSettingsChanged?.Invoke();
    }
    #endregion

    #region Helper Methods
    public AudioMixerGroup GetSFXGroup()
    {
        if (audioMixer == null) return null;
        AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("SFX");
        return groups.Length > 0 ? groups[0] : null;
    }

    public Resolution GetCurrentResolution()
    {
        return new Resolution
        {
            width = PlayerPrefs.GetInt(PREF_RES_WIDTH, Screen.currentResolution.width),
            height = PlayerPrefs.GetInt(PREF_RES_HEIGHT, Screen.currentResolution.height),
            refreshRate = PlayerPrefs.GetInt(PREF_RES_REFRESH, Screen.currentResolution.refreshRate)
        };
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        float currentVolume = PlayerPrefs.GetFloat(PREF_SFX, 0.75f);
        sfxSource.PlayOneShot(clip, currentVolume * Mathf.Clamp01(volumeScale));
    }
    #endregion
}