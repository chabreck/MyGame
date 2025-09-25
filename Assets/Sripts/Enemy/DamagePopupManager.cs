using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    [Header("Prefab")]
    public GameObject damagePopupPrefab;

    [Header("Canvas Settings")]
    public Canvas worldCanvas;

    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupWorldCanvas();
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
        StartCoroutine(DelayedCanvasSetup());
    }

    private IEnumerator DelayedCanvasSetup()
    {
        yield return new WaitForEndOfFrame();

        mainCamera = Camera.main;

        if (worldCanvas == null || worldCanvas.gameObject == null)
        {
            SetupWorldCanvas();
        }
        else
        {
            RefreshCanvasSettings();
        }
    }

    private void RefreshCanvasSettings()
    {
        if (worldCanvas == null) return;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("DamagePopupManager: Main Camera not found!");
            return;
        }

        worldCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        worldCanvas.worldCamera = mainCamera;
        worldCanvas.planeDistance = 1f;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 32767;

        var scaler = worldCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = worldCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var canvasRect = worldCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.localScale = Vector3.one;
        }

        if (worldCanvas.GetComponent<GraphicRaycaster>() == null)
            worldCanvas.gameObject.AddComponent<GraphicRaycaster>();

        Debug.Log($"Canvas configured: {worldCanvas.renderMode}, camera: {worldCanvas.worldCamera?.name}");
    }

    private void SetupWorldCanvas()
    {
        mainCamera = Camera.main;

        if (worldCanvas == null)
        {
            GameObject found = null;
            try { found = GameObject.FindWithTag("WorldCanvas"); } catch { }
            if (found == null) found = GameObject.Find("WorldCanvas");
            if (found == null) found = GameObject.Find("DamagePopup Canvas");

            if (found != null)
            {
                var c = found.GetComponent<Canvas>();
                if (c != null)
                {
                    worldCanvas = c;
                    Debug.Log("Found existing canvas: " + found.name);
                }
            }
        }

        if (worldCanvas == null)
        {
            GameObject canvasObj = new GameObject("DamagePopup Canvas");
            worldCanvas = canvasObj.AddComponent<Canvas>();
            Debug.Log("Created new canvas: " + canvasObj.name);
        }

        RefreshCanvasSettings();
    }

    public void ShowDamagePopup(float damage, Vector3 worldPosition, DamagePopup.DamageType type = DamagePopup.DamageType.Normal)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogError("DamagePopupManager: damagePopupPrefab is null!");
            return;
        }

        if (worldCanvas == null || worldCanvas.gameObject == null)
        {
            SetupWorldCanvas();
            if (worldCanvas == null) return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("DamagePopupManager: Camera not found!");
                return;
            }
        }

        if (worldCanvas.renderMode != RenderMode.ScreenSpaceCamera || worldCanvas.worldCamera != mainCamera)
        {
            RefreshCanvasSettings();
        }

        RectTransform canvasRect = worldCanvas.transform as RectTransform;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        if (screenPos.z <= 0)
        {
            return;
        }

        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, mainCamera, out localPoint);
        if (!success)
        {
            Debug.LogWarning("DamagePopupManager: ScreenPointToLocalPointInRectangle failed.");
        }

        GameObject popupObj = Instantiate(damagePopupPrefab, worldCanvas.transform, false);

        RectTransform popupRect = popupObj.GetComponent<RectTransform>();
        if (popupRect == null) popupRect = popupObj.AddComponent<RectTransform>();

        popupRect.localScale = Vector3.one;
        popupRect.localRotation = Quaternion.identity;
        popupRect.anchoredPosition = localPoint;
        popupObj.transform.SetAsLastSibling();

        var popup = popupObj.GetComponent<DamagePopup>();
        var tracker = popupObj.GetComponent<DamagePopupWorldTracker>();
        if (tracker == null)
            tracker = popupObj.AddComponent<DamagePopupWorldTracker>();

        tracker.Initialize(worldPosition, mainCamera, popupRect);

        if (popup != null)
        {
            popup.Initialize(damage, type);
        }
        else
        {
            Debug.LogError("DamagePopup component not found on prefab!");
        }
    }

    public void ShowNormalDamage(float damage, Vector3 worldPosition) => ShowDamagePopup(damage, worldPosition, DamagePopup.DamageType.Normal);
    public void ShowCriticalDamage(float damage, Vector3 worldPosition) => ShowDamagePopup(damage, worldPosition, DamagePopup.DamageType.Critical);
    public void ShowPoisonDamage(float damage, Vector3 worldPosition) => ShowDamagePopup(damage, worldPosition, DamagePopup.DamageType.Poison);
    public void ShowBurnDamage(float damage, Vector3 worldPosition) => ShowDamagePopup(damage, worldPosition, DamagePopup.DamageType.Burn);
    public void ShowHeal(float amount, Vector3 worldPosition) => ShowDamagePopup(amount, worldPosition, DamagePopup.DamageType.Heal);

    public void ForceRefreshCanvas()
    {
        Debug.Log("Force refresh canvas...");
        if (worldCanvas == null || worldCanvas.gameObject == null) SetupWorldCanvas(); else RefreshCanvasSettings();
    }
}
