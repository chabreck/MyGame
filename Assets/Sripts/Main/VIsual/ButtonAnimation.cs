using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class ButtonAnimation : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float animSpeed = 0.2f;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float hoverSoundVolume = 0.5f;
    [SerializeField] private float clickSoundVolume = 1f;

    private Vector3 originalScale;
    private Selectable selectable;
    private bool isToggle; // Добавляем флаг для определения Toggle

    void Start()
    {
        originalScale = transform.localScale;
        selectable = GetComponent<Selectable>();
        
        // Проверяем, является ли этот компонент Toggle
        isToggle = GetComponent<Toggle>() != null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!selectable.interactable || isToggle) return; // Не применяем анимацию к Toggle

        LeanTween.scale(gameObject, originalScale * hoverScale, animSpeed)
            .setEase(LeanTweenType.easeOutBack)
            .setIgnoreTimeScale(true);

        if (hoverSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySFX(hoverSound, hoverSoundVolume);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isToggle) return; // Не применяем анимацию к Toggle

        LeanTween.scale(gameObject, originalScale, animSpeed)
            .setEase(LeanTweenType.easeOutQuad)
            .setIgnoreTimeScale(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!selectable.interactable || isToggle) return; // Не применяем анимацию к Toggle

        LeanTween.scale(gameObject, originalScale * clickScale, animSpeed * 0.5f)
            .setEase(LeanTweenType.easeInOutCubic)
            .setIgnoreTimeScale(true);

        if (clickSound != null && SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySFX(clickSound, clickSoundVolume);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isToggle) return; // Не применяем анимацию к Toggle

        LeanTween.scale(gameObject, originalScale, animSpeed * 0.5f)
            .setEase(LeanTweenType.easeOutQuad)
            .setIgnoreTimeScale(true);
    }
}