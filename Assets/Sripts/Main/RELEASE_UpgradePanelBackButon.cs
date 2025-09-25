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
        // Получаем компонент Button с текущего объекта
        backButton = GetComponent<Button>();
        
        if (backButton == null)
        {
            Debug.LogError("Button component not found on this object!");
            return;
        }

        // Если панели не назначены в инспекторе, пытаемся найти их автоматически
        if (upgradePanel == null)
        {
            // Ищем UpgradePanel по имени или тегу
            upgradePanel = GameObject.Find("UpgradePanel");
            if (upgradePanel == null)
            {
                upgradePanel = GameObject.FindWithTag("UpgradePanel");
            }
        }

        if (mainPanel == null)
        {
            // Ищем MainPanel по имени или тегу
            mainPanel = GameObject.Find("MainPanel");
            if (mainPanel == null)
            {
                mainPanel = GameObject.FindWithTag("MainPanel");
            }
        }

        // Добавляем обработчик нажатия
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackButtonClicked()
    {
        // Закрываем панель улучшений и открываем главное меню
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
        // Убираем обработчик при уничтожении объекта
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }
}