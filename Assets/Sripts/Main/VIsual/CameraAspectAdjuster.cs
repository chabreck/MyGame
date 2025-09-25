using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectAdjuster : MonoBehaviour
{
    [SerializeField] private float referenceAspect = 16f / 9f; // 640x360 = 16:9
    [SerializeField] private float referenceOrthographicSize = 5.625f; // 360/2/32

    private Camera cam;
    private int lastWidth;
    private int lastHeight;
    private float lastCheckTime;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        AdjustCamera();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastCheckTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (Time.unscaledTime - lastCheckTime > 0.2f)
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                AdjustCamera();
                lastWidth = Screen.width;
                lastHeight = Screen.height;
            }
            lastCheckTime = Time.unscaledTime;
        }
    }

    public void AdjustCamera()
    {
        if (cam == null || !cam.orthographic) return;
        float currentAspect = (float)Screen.width / Screen.height;
        cam.orthographicSize = referenceOrthographicSize * Mathf.Max(1f, referenceAspect / currentAspect);
    }
}