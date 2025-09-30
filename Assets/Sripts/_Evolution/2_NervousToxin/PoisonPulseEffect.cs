using UnityEngine;
using System.Collections;

public class PoisonPulseEffect : MonoBehaviour
{
    private GameObject owner;
    private GameObject sourceEnemy;
    private float damage;
    private float radius;
    private bool damageApplied = false;

    public void Initialize(GameObject owner, GameObject sourceEnemy, float damage, float radius)
    {
        this.owner = owner;
        this.sourceEnemy = sourceEnemy;
        this.damage = damage;
        this.radius = radius;
        
        transform.localScale = Vector2.one * radius * 2f;
        
        StartCoroutine(PulseAnimation());
    }

    private IEnumerator PulseAnimation()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale * 0.1f;
        Vector3 endScale = transform.localScale;
        
        transform.localScale = startScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            if (!damageApplied && progress >= 0.5f)
            {
                ApplyDamage();
                damageApplied = true;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }

    private void ApplyDamage()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var c in cols)
        {
            if (c == null) continue;
            var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
            if (es == null) continue;
            if (es.gameObject == sourceEnemy) continue;
            
            DamageHelper.ApplyDamage(owner, es, damage, raw: false, 
                popupType: DamagePopup.DamageType.Poison,
                sourceType: DamageHelper.DamageSourceType.Pulse);
        }
    }
}