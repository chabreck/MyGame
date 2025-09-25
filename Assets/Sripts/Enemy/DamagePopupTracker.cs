using UnityEngine;

public class DamagePopupWorldTracker : MonoBehaviour
{
    private Vector3 worldPosition;
    private Vector3 startWorldPosition;
    private Camera trackingCamera;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    public void Initialize(Vector3 worldPos, Camera camera, RectTransform rect)
    {
        worldPosition = worldPos;
        startWorldPosition = worldPos;
        trackingCamera = camera;
        rectTransform = rect;
        parentCanvas = GetComponentInParent<Canvas>();

        UpdateScreenPosition();
    }

    private void Update()
    {
        if (trackingCamera == null || rectTransform == null || parentCanvas == null) return;
        UpdateScreenPosition();
    }

    private void UpdateScreenPosition()
    {
        Vector3 screenPos = trackingCamera.WorldToScreenPoint(worldPosition);

        if (screenPos.z <= 0)
        {
            if (rectTransform.gameObject.activeSelf) rectTransform.gameObject.SetActive(false);
            return;
        }

        if (!rectTransform.gameObject.activeSelf) rectTransform.gameObject.SetActive(true);

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, trackingCamera, out localPoint);
        if (success) rectTransform.anchoredPosition = localPoint;
    }

    public void UpdateWorldPosition(Vector3 offset)
    {
        worldPosition = startWorldPosition + offset;
    }

    public Vector3 GetWorldPosition() => worldPosition;
    public Vector3 GetStartWorldPosition() => startWorldPosition;
}