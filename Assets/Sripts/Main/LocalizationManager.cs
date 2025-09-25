using UnityEngine;
using System.Linq;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private const string SELECTED_LANGUAGE_KEY = "SelectedLanguage";

    private List<LanguageData> supportedLanguages;
    public System.Action OnLanguageChanged;

    [System.Serializable]
    public class LanguageData
    {
        public string languageName;
        public string languageCode;
        public string localeName;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLanguages();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(LoadSavedLanguage());
    }

    private void InitializeLanguages()
    {
        supportedLanguages = new List<LanguageData>
        {
            new LanguageData { languageName = "English", languageCode = "en", localeName = "en" },
            new LanguageData { languageName = "Russian", languageCode = "ru", localeName = "ru" },
            new LanguageData { languageName = "Chinese", languageCode = "zh-Hans", localeName = "zh-Hans" },
            new LanguageData { languageName = "Spanish", languageCode = "es", localeName = "es" },
            new LanguageData { languageName = "Portuguese", languageCode = "pt-BR", localeName = "pt-BR" },
            new LanguageData { languageName = "German", languageCode = "de", localeName = "de" },
            new LanguageData { languageName = "Japanese", languageCode = "ja", localeName = "ja" },
            new LanguageData { languageName = "French", languageCode = "fr", localeName = "fr" },
            new LanguageData { languageName = "Polish", languageCode = "pl", localeName = "pl" },
            new LanguageData { languageName = "Korean", languageCode = "ko", localeName = "ko" },
            new LanguageData { languageName = "Turkish", languageCode = "tr", localeName = "tr" },
            new LanguageData { languageName = "Italian", languageCode = "it", localeName = "it" },
            new LanguageData { languageName = "Ukrainian", languageCode = "uk", localeName = "uk" }
        };
    }

    public void SetLanguageByIndex(int index)
    {
        StartCoroutine(SetLanguage(supportedLanguages[index].languageCode));
    }

    private IEnumerator LoadSavedLanguage()
    {
        yield return LocalizationSettings.InitializationOperation;

        string saved = PlayerPrefs.GetString(SELECTED_LANGUAGE_KEY, "en");
        yield return SetLanguage(saved);
    }

    public IEnumerator SetLanguage(string languageCode)
    {
        yield return LocalizationSettings.InitializationOperation;

        var available = LocalizationSettings.AvailableLocales.Locales;

        foreach (var locale in available)
        {
            if (locale.Identifier.Code == languageCode)
            {
                LocalizationSettings.SelectedLocale = locale;

                PlayerPrefs.SetString(SELECTED_LANGUAGE_KEY, languageCode);
                PlayerPrefs.Save();

                RefreshAllLocalizeStringEvents();

                OnLanguageChanged?.Invoke();
                yield break;
            }
        }

        // fallback en
        var en = available.FirstOrDefault(l => l.Identifier.Code == "en");
        if (en != null)
        {
            LocalizationSettings.SelectedLocale = en;
            RefreshAllLocalizeStringEvents();
        }
    }

    public List<LanguageData> GetSupportedLanguages() => supportedLanguages;

    public int GetCurrentLanguageIndex()
    {
        string curr = PlayerPrefs.GetString(SELECTED_LANGUAGE_KEY, "en");
        return supportedLanguages.FindIndex(d => d.languageCode == curr);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RefreshAfterSceneLoad());
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        yield return null;

        // сразу перебиндим всё после загрузки сцены
        var binder = FindObjectOfType<LocalizeAutoBinder>();
        if (binder != null)
            binder.AutoBindAll();

        RefreshAllLocalizeStringEvents();
    }

    private void RefreshAllLocalizeStringEvents()
    {
        var localizers = FindObjectsOfType<UnityEngine.Localization.Components.LocalizeStringEvent>(true);
        foreach (var l in localizers)
        {
            l.RefreshString();
        }
    }
}
