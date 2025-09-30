using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public class WeatherUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private LocalizedString difficultyFormat;

    private WeatherBase current;
    private int currentDifficulty;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    public void Show(WeatherBase data, int difficulty)
    {
        current = data;
        currentDifficulty = difficulty;
        if (panel != null) panel.SetActive(true);
        UpdateName();
        UpdateDuration();
        UpdateDifficultyText();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void UpdateName()
    {
        if (current == null)
        {
            if (nameText != null) nameText.text = "";
            return;
        }

        if (nameText == null) return;

        if (current.conditionNameLocalized != null)
        {
            var handle = current.conditionNameLocalized.GetLocalizedStringAsync();
            handle.Completed += (AsyncOperationHandle<string> h) =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded) nameText.text = h.Result;
                else nameText.text = current.conditionNameFallback ?? current.name;
            };
        }
        else
        {
            nameText.text = current.conditionNameFallback ?? current.name;
        }
    }

    private void UpdateDuration()
    {
        if (current == null || durationText == null)
        {
            if (durationText != null) durationText.text = "";
            return;
        }

        float dur = 0f;
        if (current.durations != null && current.durations.Length > 0)
        {
            int idx = Mathf.Clamp(currentDifficulty - 1, 0, current.durations.Length - 1);
            dur = current.durations[idx];
        }

        durationText.text = $"Duration: {Mathf.RoundToInt(dur)}s";
    }

    private void UpdateDifficultyText()
    {
        if (difficultyText == null) return;

        if (difficultyFormat != null)
        {
            difficultyFormat.Arguments = new object[] { currentDifficulty };
            var h = difficultyFormat.GetLocalizedStringAsync();
            h.Completed += (AsyncOperationHandle<string> res) =>
            {
                if (res.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrEmpty(res.Result))
                    difficultyText.text = res.Result;
                else
                    difficultyText.text = $"Difficulty: {currentDifficulty}";
            };
        }
        else
        {
            difficultyText.text = $"Difficulty: {currentDifficulty}";
        }
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        UpdateName();
        UpdateDuration();
        UpdateDifficultyText();
    }
}
