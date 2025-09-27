using UnityEngine;
using System.Collections;

public class PoisonPulseVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    public float animationDuration = 0.5f;
    public Color pulseColor = new Color(0.2f, 0.8f, 0.1f, 0.7f);
    
    private SpriteRenderer spriteRenderer;
    private float damage;
    private float radius;
    private GameObject owner;
    private GameObject sourceEnemy;
    private bool damageApplied = false;

    public void Initialize(GameObject owner, GameObject sourceEnemy, float damage, float radius)
    {
        this.owner = owner;
        this.sourceEnemy = sourceEnemy;
        this.damage = damage;
        this.radius = radius;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CreateCircleSprite();
        }
        
        spriteRenderer.color = pulseColor;
        spriteRenderer.sortingOrder = 100;
        
        transform.localScale = Vector3.one * radius * 2f;
        
        StartCoroutine(PulseAnimation());
    }

    private void CreateCircleSprite()
    {
        int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        
        Vector2 center = new Vector2(textureSize / 2, textureSize / 2);
        float maxDistance = textureSize / 2;
        
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - (distance / maxDistance);
                
                if (alpha > 0)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), 
                                     new Vector2(0.5f, 0.5f), 100f);
        spriteRenderer.sprite = sprite;
    }

    private IEnumerator PulseAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.1f;
        Vector3 endScale = Vector3.one * radius * 2f;
        
        transform.localScale = startScale;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(0.8f, 0f, progress);
            spriteRenderer.color = color;
            
            if (!damageApplied && progress >= 0.5f)
            {
                ApplyPulseDamage();
                damageApplied = true;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }

    private void ApplyPulseDamage()
    {
        Vector2 center = transform.position;
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, radius);
        
        if (cols != null && cols.Length > 0)
        {
            foreach (var c in cols)
            {
                if (c == null) continue;
                var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
                if (es == null) continue;
                if (es.gameObject == sourceEnemy) continue;
                
                DamageHelper.ApplyDamage(owner, es, damage, raw: false, popupType: DamagePopup.DamageType.Poison);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}