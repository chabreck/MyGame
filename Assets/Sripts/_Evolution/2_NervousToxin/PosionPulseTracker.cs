using UnityEngine;

public class PoisonPulseTracker : MonoBehaviour
{
    private GameObject ownerPlayer;
    private GameObject sourceEnemy;
    private float pulseBase = 6f;
    private float pulseRadius = 2.5f;
    private float pulseScale = 0.25f;
    private float xpChance = 0.06f;
    private float xpRadius = 10f;
    private float lastTickTime = -999f;

    public void Configure(GameObject owner, GameObject enemy, float baseDamage, float radius, float scaleFromTick, float xpAttractChance, float xpAttractRadius)
    {
        ownerPlayer = owner;
        sourceEnemy = enemy;
        pulseBase = baseDamage;
        pulseRadius = radius;
        pulseScale = scaleFromTick;
        xpChance = xpAttractChance;
        xpRadius = xpAttractRadius;
    }

    public void OnPoisonTick(float tickDamage)
    {
        lastTickTime = Time.time;
        float dmg = pulseBase + tickDamage * pulseScale;
        Vector2 center = transform.position;
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, pulseRadius);
        
        if (cols != null && cols.Length > 0)
        {
            foreach (var c in cols)
            {
                if (c == null) continue;
                var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
                if (es == null) continue;
                if (es.gameObject == sourceEnemy) continue;
                
                // Наносим урон пульсации с зеленым цветом
                DamageHelper.ApplyDamage(ownerPlayer, es, dmg, raw: false, popupType: DamagePopup.DamageType.Poison);
            }
        }
        
        if (Random.value <= xpChance) 
            TryAttractNearestXPOrb(center, xpRadius);
    }

    private void TryAttractNearestXPOrb(Vector2 fromPos, float searchRadius)
    {
        // Ищем все объекты с тегом XPOrb или содержащие "xp"/"orb" в имени
        var allObjects = FindObjectsOfType<GameObject>();
        GameObject bestOrb = null;
        float bestDist = float.MaxValue;
        
        foreach (var obj in allObjects)
        {
            if (obj == null) continue;
            
            bool isXPOrb = obj.CompareTag("XPOrb") || 
                          obj.name.ToLower().Contains("xp") || 
                          obj.name.ToLower().Contains("orb") ||
                          obj.GetComponent<ExperienceCollector>() != null;
            
            if (isXPOrb)
            {
                float dist = Vector3.Distance(obj.transform.position, fromPos);
                if (dist <= searchRadius && dist < bestDist)
                {
                    bestOrb = obj;
                    bestDist = dist;
                }
            }
        }
        
        if (bestOrb == null) return;

        // Пытаемся использовать метод AttractTo если он есть
        var expCollector = bestOrb.GetComponent<ExperienceCollector>();
        var player = GameObject.FindGameObjectWithTag("Player");
        
        if (expCollector != null && player != null)
        {
            var attractMethod = expCollector.GetType().GetMethod("AttractTo");
            if (attractMethod != null)
            {
                try 
                { 
                    attractMethod.Invoke(expCollector, new object[] { player.transform }); 
                    return; 
                } 
                catch (System.Exception e) 
                {
                    Debug.LogWarning($"Failed to invoke AttractTo: {e.Message}");
                }
            }
            
            // Fallback: прямое притягивание
            var rb = bestOrb.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = ((Vector2)player.transform.position - (Vector2)bestOrb.transform.position).normalized;
                rb.velocity = dir * 10f;
            }
            else
            {
                bestOrb.transform.position = Vector3.MoveTowards(bestOrb.transform.position, player.transform.position, 0.5f);
            }
        }
    }

    private void Update()
    {
        // Удаляем компонент если не было тиков более 12 секунд
        if (Time.time - lastTickTime > 12f) 
            Destroy(this);
    }
}