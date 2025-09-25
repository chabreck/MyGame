using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 1f;
    public float fadeSpeed = 0.4f;
    public float lifetime = 0.8f;
    public Vector2 randomOffsetWorld = new Vector2(0.5f, 0.5f);

    public TMP_FontAsset overrideFont;

    private TextMeshProUGUI damageText;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private DamagePopupWorldTracker worldTracker;

    public enum DamageType { Normal, Critical, Poison, Burn, Heal }

    private void Awake()
    {
        damageText = GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        worldTracker = GetComponent<DamagePopupWorldTracker>();

        if (damageText == null) Debug.LogError("DamagePopup: TextMeshProUGUI not found!");

        if (overrideFont != null && damageText != null) damageText.font = overrideFont;

        if (damageText != null)
        {
            damageText.enableAutoSizing = false;
            damageText.enableWordWrapping = false;
            damageText.raycastTarget = false;
        }

        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (rectTransform == null) rectTransform = gameObject.AddComponent<RectTransform>();
    }

    public void Initialize(float damage, DamageType type = DamageType.Normal)
    {
        SetupDamageDisplay(damage, type);

        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        if (worldTracker != null)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomOffsetWorld.x, randomOffsetWorld.x),
                Random.Range(0f, randomOffsetWorld.y),
                0f
            );
            worldTracker.UpdateWorldPosition(randomOffset);
        }

        StartCoroutine(AnimatePopup());
    }

    private void SetupDamageDisplay(float damage, DamageType type)
    {
        string txt = Mathf.RoundToInt(damage).ToString();
        Color textColor = Color.white;
        float fontSize = 33f;

        switch (type)
        {
            case DamageType.Normal: textColor = Color.white; fontSize = 33f; break;
            case DamageType.Critical: textColor = Color.red; fontSize = 33f; break;
            case DamageType.Poison: textColor = Color.green; fontSize = 33f; break;
            case DamageType.Burn: textColor = new Color(1f, 0.5f, 0f); fontSize = 33f; break;
            case DamageType.Heal: textColor = Color.cyan; txt = "+" + txt; fontSize = 33f; break;
        }

        if (damageText != null)
        {
            damageText.text = txt;
            damageText.color = textColor;
            damageText.fontSize = fontSize;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private IEnumerator AnimatePopup()
    {
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        Vector3 startWorldOffset = Vector3.zero;
        Vector3 targetWorldOffset = Vector3.up * moveSpeed;

        float fadeStartTime = Mathf.Max(0f, lifetime - fadeSpeed);

        while (elapsedTime < lifetime)
        {
            float dt = Time.deltaTime;
            elapsedTime += dt;
            float progress = Mathf.Clamp01(elapsedTime / lifetime);

            if (progress < 0.3f)
            {
                float scaleProgress = progress / 0.3f;
                rectTransform.localScale = Vector3.Lerp(startScale, targetScale,
                    Mathf.Sin(scaleProgress * Mathf.PI * 0.5f));
            }

            if (worldTracker != null)
            {
                float lerpT = Mathf.SmoothStep(0f, 1f, progress);
                Vector3 currentWorldOffset = Vector3.Lerp(startWorldOffset, targetWorldOffset, lerpT);
                worldTracker.UpdateWorldPosition(currentWorldOffset);
            }

            if (elapsedTime >= fadeStartTime)
            {
                float t = (elapsedTime - fadeStartTime) / Mathf.Max(0.0001f, fadeSpeed);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
