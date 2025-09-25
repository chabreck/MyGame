using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class LocalizeAutoBinder : MonoBehaviour
{
    public bool autoBindOnStart = true;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (autoBindOnStart)
            AutoBindAll();
    }

    void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += AutoBindAll;
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= AutoBindAll;
    }

    public void AutoBindAll()
    {
        var all = FindObjectsOfType<LocalizeStringEvent>(true);
        int bound = 0;

        foreach (var lse in all)
        {
            if (lse.OnUpdateString.GetPersistentEventCount() > 0)
            {
                lse.RefreshString();
                continue;
            }

            var tmp = lse.GetComponent<TMP_Text>() ?? lse.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                UnityAction<string> act = s => tmp.SetText(s);
                lse.OnUpdateString.AddListener(act);
                bound++;
                lse.RefreshString();
                continue;
            }

            var uiText = lse.GetComponent<UnityEngine.UI.Text>() ?? lse.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null)
            {
                UnityAction<string> act2 = s => uiText.text = s;
                lse.OnUpdateString.AddListener(act2);
                bound++;
                lse.RefreshString();
            }
        }
    }
}