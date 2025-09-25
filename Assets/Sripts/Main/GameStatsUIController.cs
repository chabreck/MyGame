using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

public class GameStatsUIController : MonoBehaviour
{
    public static GameStatsUIController Instance;

    [Header("Health UI (Image Fill)")]
    [SerializeField] private Image healthBarImage;

    [Header("XP UI (Image Fill)")]
    [SerializeField] private Image xpBarImage;

    [Header("Level / Timer UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text timerText;

    private float elapsedTime;
    private int cachedCurrentXP = 0;
    private int cachedMaxXP = 1;
    private int cachedLevel = 1;

    public LocalizedString levelKey = new LocalizedString { TableReference = "UI", TableEntryReference = "bottomui_level" };

    private string cachedLevelPrefix = "";

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

        StartCoroutine(RefreshLocalizedPrefixes());
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateTimerTextImmediate();
    }

    private void OnLanguageChanged() => StartCoroutine(RefreshLocalizedPrefixes());

    private IEnumerator RefreshLocalizedPrefixes()
    {
        var levelHandle = levelKey.GetLocalizedStringAsync();
        yield return levelHandle;
        cachedLevelPrefix = levelHandle.Status == AsyncOperationStatus.Succeeded ? levelHandle.Result ?? "" : levelKey.GetLocalizedString();

        UpdateLevelTextImmediate();
    }

    private void UpdateTimerTextImmediate()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        if (timerText != null) timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateLevelTextImmediate()
    {
        string lvlPrefix = string.IsNullOrEmpty(cachedLevelPrefix) ? levelKey.GetLocalizedString() : cachedLevelPrefix;
        if (levelText != null) levelText.text = $"{lvlPrefix}: {cachedLevel}";
    }

    public void UpdateHealthUI(int current, int max)
    {
        if (max <= 0) max = 1;
        float ratio = Mathf.Clamp01((float)current / max);
        if (healthBarImage != null) healthBarImage.fillAmount = ratio;
    }

    public void UpdateXPUI(int currentXP, int maxXP, int level)
    {
        cachedCurrentXP = currentXP;
        cachedMaxXP = maxXP <= 0 ? 1 : maxXP;
        cachedLevel = level;
        if (xpBarImage != null)
            xpBarImage.fillAmount = Mathf.Clamp01((float)currentXP / cachedMaxXP);
        UpdateLevelTextImmediate();
    }
}
