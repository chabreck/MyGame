using UnityEngine;
using UnityEngine.UI;

public class UpgradePanelBackButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject mainPanel;

    private Button backButton;

    private void Start()
    {
        backButton = GetComponent<Button>();
        
        if (backButton == null)
        {
            Debug.LogError("Button component not found on this object!");
            return;
        }

        if (upgradePanel == null)
        {
            upgradePanel = GameObject.Find("UpgradePanel");
            if (upgradePanel == null)
            {
                upgradePanel = GameObject.FindWithTag("UpgradePanel");
            }
        }

        if (mainPanel == null)
        {
            mainPanel = GameObject.Find("MainPanel");
            if (mainPanel == null)
            {
                mainPanel = GameObject.FindWithTag("MainPanel");
            }
        }

        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackButtonClicked()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
        
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Main panel reference is missing!");
        }
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }
}