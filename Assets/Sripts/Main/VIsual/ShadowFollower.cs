using UnityEngine;

[DisallowMultipleComponent]
public class ShadowFollower : MonoBehaviour
{
    [Header("Shadow Appearance")]
    [Tooltip("Цвет тени (темный с прозрачностью для top-down игры)")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.6f);
    
    [Tooltip("Базовый масштаб тени")]
    public float baseScale = 0.7f;
    
    [Tooltip("Минимальный масштаб тени при прыжке")]
    public float minScale = 0.55f;

    [Header("Placement")]
    [Tooltip("Смещение тени (для top-down обычно немного вниз)")]
    public Vector3 localOffset = new Vector3(0f, -0.55f, 0f);
    
    [Tooltip("Слой сортировки для теней")]
    public string sortingLayerName = "Shadows";
    
    [Tooltip("Order in layer")]
    public int orderInLayer = 0;

    [Header("Smooth Following")]
    [Tooltip("Плавность следования")]
    [Range(0f, 1f)]
    public float followLerp = 0.15f;

    private GameObject shadowGO;
    private SpriteRenderer sr;
    private Sprite ovalSprite;
    private Transform cachedTransform;

    public float RuntimeHeight { get; set; } = 0f;

    void Awake()
    {
        cachedTransform = transform;
        CreateShadow();
    }

    void OnValidate()
    {
        if (sr != null)
        {
            sr.color = shadowColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = orderInLayer;
        }
    }

    private void CreateShadow()
    {
        if (shadowGO != null) return;

        shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(transform, false);
        shadowGO.transform.localPosition = localOffset;
        shadowGO.transform.localRotation = Quaternion.identity;
        
        sr = shadowGO.AddComponent<SpriteRenderer>();

        if (ovalSprite == null)
            ovalSprite = GenerateOvalSprite(48, 32);
            
        sr.sprite = ovalSprite;
        sr.color = shadowColor;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = orderInLayer;
        
        shadowGO.transform.localEulerAngles = Vector3.zero;
    }

    void LateUpdate()
    {
        if (shadowGO == null) CreateShadow();

        Vector3 targetPos = cachedTransform.position + localOffset;
        shadowGO.transform.position = Vector3.Lerp(shadowGO.transform.position, targetPos, 
            1f - Mathf.Pow(1f - followLerp, Time.deltaTime * 60f));

        float h = Mathf.Max(0f, RuntimeHeight);
        float scale = baseScale * (1f - h * 0.3f);
        scale = Mathf.Max(minScale, scale);
        shadowGO.transform.localScale = new Vector3(scale, scale, 1f);

        Color c = shadowColor;
        c.a = shadowColor.a * (1f - h * 0.4f);
        sr.color = c;
    }

    private Sprite GenerateOvalSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float cx = (width - 1) / 2f;
        float cy = (height - 1) / 2f;
        float radiusX = (width - 1) / 2f;
        float radiusY = (height - 1) / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                
                float ellipseValue = (dx * dx) / (radiusX * radiusX) + (dy * dy) / (radiusY * radiusY);
                
                float t = Mathf.Clamp01((1f - ellipseValue) / 0.3f);
                Color col = new Color(1f, 1f, 1f, t);
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();

        Rect rect = new Rect(0, 0, width, height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Sprite s = Sprite.Create(tex, rect, pivot, 32f);
        s.name = "OvalShadowSprite";
        
        return s;
    }
}