using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UpgradeButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button pickButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI pickButtonText;

    [Header("Localization")]
    [SerializeField] private LocalizedString levelPrefixLocalized;
    [SerializeField] private LocalizedString evolutionTextLocalized;
    [SerializeField] private LocalizedString pickLocalized;

    private UpgradeSystem upgradeSystem;
    private UpgradeSystem.UpgradeOption currentOption;

    private string cachedLevelPrefix = "Level";
    private string cachedEvolutionText = "Evolution";
    private string cachedPickText = "Pick";

    private LocalizedString.ChangeHandler levelPrefixHandler;
    private LocalizedString.ChangeHandler evolutionHandler;
    private LocalizedString.ChangeHandler pickHandler;
    private LocalizedString.ChangeHandler titleOptionHandler;
    private LocalizedString.ChangeHandler descriptionOptionHandler;

    private void Awake()
    {
        levelPrefixHandler = (s) => { cachedLevelPrefix = s; ApplyLevelText(); };
        evolutionHandler = (s) => { cachedEvolutionText = s; ApplyLevelText(); };
        pickHandler = (s) => { cachedPickText = s; ApplyPickText(); };

        titleOptionHandler = (s) => { if (titleText != null) titleText.text = s ?? ""; };
        descriptionOptionHandler = (s) => { if (descriptionText != null) descriptionText.text = s ?? ""; };

        TrySubscribeLabel(levelPrefixLocalized, levelPrefixHandler);
        TrySubscribeLabel(evolutionTextLocalized, evolutionHandler);
        TrySubscribeLabel(pickLocalized, pickHandler);

        TryRefreshLabel(levelPrefixLocalized, levelPrefixHandler);
        TryRefreshLabel(evolutionTextLocalized, evolutionHandler);
        TryRefreshLabel(pickLocalized, pickHandler);
    }

    private void OnDestroy()
    {
        TryUnsubscribeLabel(levelPrefixLocalized, levelPrefixHandler);
        TryUnsubscribeLabel(evolutionTextLocalized, evolutionHandler);
        TryUnsubscribeLabel(pickLocalized, pickHandler);

        UnsubscribeOptionLocalized();
    }

    public void Initialize(UpgradeSystem system)
    {
        upgradeSystem = system;

        if (pickButton != null)
        {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(OnButtonClick);
        }
    }

    public void Setup(UpgradeSystem.UpgradeOption option)
    {
        UnsubscribeOptionLocalized();

        currentOption = option;
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        titleText.text = option.Title ?? "";
        descriptionText.text = option.Description ?? "";

        iconImage.sprite = option.Icon;
        iconImage.gameObject.SetActive(option.Icon != null);

        pickButton.interactable = true;
        pickButton.navigation = new Navigation() { mode = Navigation.Mode.None };

        SubscribeOptionLocalized(option);

        ApplyLevelText();
        ApplyPickText();
    }

    private void ApplyLevelText()
    {
        if (levelText == null) return;
        if (currentOption == null)
        {
            levelText.gameObject.SetActive(false);
            return;
        }

        if (currentOption.Type == UpgradeSystem.UpgradeType.Evolution)
        {
            levelText.text = cachedEvolutionText;
            levelText.gameObject.SetActive(true);
        }
        else if (currentOption.currentLevel > 0)
        {
            levelText.text = $"{cachedLevelPrefix} {currentOption.currentLevel}";
            levelText.gameObject.SetActive(true);
        }
        else
        {
            levelText.gameObject.SetActive(false);
        }
    }

    private void ApplyPickText()
    {
        if (pickButtonText != null)
            pickButtonText.text = cachedPickText;
    }

    private void OnButtonClick()
    {
        if (currentOption == null || upgradeSystem == null) return;
        if (!pickButton.interactable) return;
        upgradeSystem.SelectUpgrade(currentOption);
    }

    public void ResetButton()
    {
        if (pickButton != null) pickButton.interactable = false;
        gameObject.SetActive(false);

        UnsubscribeOptionLocalized();
        currentOption = null;
    }

    private void TrySubscribeLabel(LocalizedString ls, LocalizedString.ChangeHandler handler)
    {
        if (ls != null) ls.StringChanged += handler;
    }

    private void TryUnsubscribeLabel(LocalizedString ls, LocalizedString.ChangeHandler handler)
    {
        if (ls != null) ls.StringChanged -= handler;
    }

    private void TryRefreshLabel(LocalizedString ls, LocalizedString.ChangeHandler handler)
    {
        if (ls == null) return;
        var h = ls.GetLocalizedStringAsync();
        h.Completed += (res) =>
        {
            if (res.Status == AsyncOperationStatus.Succeeded)
                handler?.Invoke(res.Result);
        };
    }

    private void SubscribeOptionLocalized(UpgradeSystem.UpgradeOption opt)
    {
        if (opt == null) return;
        if (opt.titleLocalized != null) opt.titleLocalized.StringChanged += titleOptionHandler;
        if (opt.descriptionLocalized != null) opt.descriptionLocalized.StringChanged += descriptionOptionHandler;

        if (opt.titleLocalized != null) TryRefreshLabel(opt.titleLocalized, titleOptionHandler);
        if (opt.descriptionLocalized != null) TryRefreshLabel(opt.descriptionLocalized, descriptionOptionHandler);
    }

    private void UnsubscribeOptionLocalized()
    {
        if (currentOption == null) return;
        if (currentOption.titleLocalized != null) currentOption.titleLocalized.StringChanged -= titleOptionHandler;
        if (currentOption.descriptionLocalized != null) currentOption.descriptionLocalized.StringChanged -= descriptionOptionHandler;
    }
}
