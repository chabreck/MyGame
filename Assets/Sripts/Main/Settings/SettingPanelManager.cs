using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using TMPro;

[DisallowMultipleComponent]
public class SettingsPanelManager : MonoBehaviour
{
    public static SettingsPanelManager Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    public event Action OnHidden;
    private const string RESOURCES_PATH = "SettingCanvas";
    private static bool _isCreating = false;
    private bool _isVisible = false;
    
    private void Awake()
    {
        Debug.Log("[SettingsPanelManager] Awake called");
        
        if (Instance != null && Instance != this)
        {
            Debug.Log("[SettingsPanelManager] Destroying duplicate instance");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (panelRoot == null)
        {
            Transform t = transform.Find("Setting Panel");
            if (t != null) panelRoot = t.gameObject;
            else if (transform.childCount > 0) panelRoot = transform.GetChild(0).gameObject;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void EnsureInstanceExists()
    {
        Debug.Log("[SettingsPanelManager] EnsureInstanceExists called");
        
        if (Instance != null)
        {
            Debug.Log("[SettingsPanelManager] Instance already exists");
            return;
        }

        // Проверяем, есть ли уже объект на сцене
        var existingObj = GameObject.Find(RESOURCES_PATH);
        if (existingObj != null)
        {
            Debug.Log("[SettingsPanelManager] Found existing object: " + existingObj.name);
            var mgr = existingObj.GetComponentInChildren<SettingsPanelManager>(true) ?? existingObj.GetComponent<SettingsPanelManager>();
            if (mgr != null)
            {
                Instance = mgr;
                Debug.Log("[SettingsPanelManager] Bound to existing instance");
                return;
            }
        }

        if (_isCreating)
        {
            Debug.Log("[SettingsPanelManager] Already creating instance, skipping");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(RESOURCES_PATH);
        if (prefab == null)
        {
            Debug.LogWarning("[SettingsPanelManager] Prefab not found in Resources: " + RESOURCES_PATH);
            return;
        }

        try
        {
            _isCreating = true;
            Debug.Log("[SettingsPanelManager] Creating new instance from prefab");
            
            // Проверяем, нет ли уже объекта с таким именем
            existingObj = GameObject.Find(prefab.name);
            if (existingObj != null)
            {
                Debug.Log("[SettingsPanelManager] Object with name already exists: " + prefab.name);
                var mgrAgain = existingObj.GetComponentInChildren<SettingsPanelManager>(true) ?? existingObj.GetComponent<SettingsPanelManager>();
                if (mgrAgain != null)
                {
                    Instance = mgrAgain;
                    return;
                }
            }

            var inst = Instantiate(prefab);
            inst.name = prefab.name;
            DontDestroyOnLoad(inst);

            var mgrComp = inst.GetComponentInChildren<SettingsPanelManager>(true) ?? inst.GetComponent<SettingsPanelManager>();
            if (mgrComp != null)
            {
                Instance = mgrComp;
                Debug.Log("[SettingsPanelManager] New instance created and assigned");
            }
        }
        finally
        {
            _isCreating = false;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        Debug.Log("[SettingsPanelManager] Scene loaded: " + s.name);
        ForceCloseAllDropdowns(true);
    }

    private void ForceCloseAllDropdowns(bool includeTemplates = false)
    {
        var tmpDropdowns = FindObjectsOfType<TMP_Dropdown>(true);
        foreach (var dd in tmpDropdowns)
        {
            try
            {
                dd.Hide();
                
                if (includeTemplates)
                {
                    Transform template = dd.transform.Find("Template");
                    if (template != null && template.gameObject.activeSelf)
                    {
                        template.gameObject.SetActive(false);
                    }
                }
                
                dd.interactable = false;
                dd.interactable = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsPanelManager] Error resetting TMP_Dropdown {dd.name}: {e.Message}");
            }
        }

        var uiDropdowns = FindObjectsOfType<UnityEngine.UI.Dropdown>(true);
        foreach (var dd in uiDropdowns)
        {
            try
            {
                dd.Hide();
                
                if (includeTemplates)
                {
                    Transform template = dd.transform.Find("Template");
                    if (template != null && template.gameObject.activeSelf)
                    {
                        template.gameObject.SetActive(false);
                    }
                }
                
                dd.interactable = false;
                dd.interactable = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsPanelManager] Error resetting UI.Dropdown {dd.name}: {e.Message}");
            }
        }
    }

    public void Show()
    {
        Debug.Log("[SettingsPanelManager] Show called");
        
        if (Instance != this)
        {
            Debug.LogWarning("[SettingsPanelManager] Show called on non-instance, redirecting");
            if (Instance != null) Instance.Show();
            return;
        }

        GameObject prefabRoot = transform.root != null ? transform.root.gameObject : gameObject;
        if (!prefabRoot.activeSelf) prefabRoot.SetActive(true);

        if (panelRoot == null)
        {
            Transform t = transform.Find("Setting Panel");
            if (t != null) panelRoot = t.gameObject;
            else if (transform.childCount > 0) panelRoot = transform.GetChild(0).gameObject;
        }

        GameObject uiRoot = panelRoot != null ? panelRoot : prefabRoot;
        if (!uiRoot.activeSelf) uiRoot.SetActive(true);

        var canvases = prefabRoot.GetComponentsInChildren<Canvas>(true);
        if (canvases == null || canvases.Length == 0)
        {
            var newCanvas = uiRoot.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 1000;
            uiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        else
        {
            foreach (var c in canvases)
            {
                if (!c.gameObject.activeSelf) c.gameObject.SetActive(true);
                try { c.sortingOrder = Mathf.Max(c.sortingOrder, 1000); } catch { }
                if (c.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    c.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        var initializers = uiRoot.GetComponentsInChildren<ISettingsUIInitializer>(true);
        foreach (var init in initializers)
        {
            try { init.InitializeUI(); } catch { }
        }
        
        ForceCloseAllDropdowns(true);

        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        EnsureEventSystemExists();

        _isVisible = true;
        Debug.Log("[SettingsPanelManager] Show completed");
    }

    private void EnsureEventSystemExists()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var existing = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (existing != null) return;

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        DontDestroyOnLoad(esGO);
        Debug.Log("[SettingsPanelManager] EventSystem created");
    }

    public void ShowForPauseMenu()
    {
        Show();
    }

    public void ShowForMainMenu()
    {
        Show();
    }

    public void Hide()
    {
        Debug.Log("[SettingsPanelManager] Hide called");
        
        if (!_isVisible)
        {
            Debug.Log("[SettingsPanelManager] Already hidden, ignoring");
            return;
        }

        ForceCloseAllDropdowns(true);

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
        {
            GameObject prefabRoot = transform.root != null ? transform.root.gameObject : gameObject;
            if (prefabRoot != null) prefabRoot.SetActive(false);
        }

        PlayerPrefs.Save();
        _isVisible = false;

        try
        {
            OnHidden?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        
        Debug.Log("[SettingsPanelManager] Hide completed");
    }

    public bool IsVisible()
    {
        return _isVisible;
    }
    
    public SettingsController GetController()
    {
        if (panelRoot == null) return GetComponentInChildren<SettingsController>(true);
        return panelRoot.GetComponentInChildren<SettingsController>(true);
    }
}