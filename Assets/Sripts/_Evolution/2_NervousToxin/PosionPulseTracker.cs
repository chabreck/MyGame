using UnityEngine;
using System.Collections;

public class PoisonPulseTracker : MonoBehaviour
{
    [Header("Pulse Visual")]
    public GameObject pulsePrefab;
    
    private GameObject ownerPlayer;
    private GameObject sourceEnemy;
    private float pulseBase = 6f;
    private float pulseRadius = 2.5f;
    private float pulseScale = 0.25f;
    private float xpChance = 0.06f;
    private float xpRadius = 10f;
    private float lastTickTime = -999f;

    public void Configure(GameObject owner, GameObject enemy, float baseDamage, float radius, 
                         float scaleFromTick, float xpAttractChance, float xpAttractRadius)
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
        
        Debug.Log($"PoisonPulse: Processing tick with damage {dmg}, XP chance: {xpChance}");
        
        ApplyPulseDamage(dmg);
        
        if (Random.value <= xpChance) 
        {
            Debug.Log("PoisonPulse: Attempting to attract XP orb");
            TryAttractNearestXPOrb(transform.position);
        }
    }

    private void ApplyPulseDamage(float damage)
    {
        Vector2 center = transform.position;
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, pulseRadius);
        
        if (cols != null && cols.Length > 0)
        {
            Debug.Log($"PoisonPulse: Found {cols.Length} colliders in radius");
            
            foreach (var c in cols)
            {
                if (c == null) continue;
                var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
                if (es == null) continue;
                if (es.gameObject == sourceEnemy) continue;
                
                Debug.Log($"PoisonPulse: Damaging enemy {es.gameObject.name}");
                DamageHelper.ApplyDamage(ownerPlayer, es, damage, raw: false, 
                    popupType: DamagePopup.DamageType.Poison,
                    sourceType: DamageHelper.DamageSourceType.Pulse);
            }
        }
        else
        {
            Debug.Log("PoisonPulse: No colliders found in radius");
        }
    }

    private void TryAttractNearestXPOrb(Vector2 fromPos)
    {
        var xpCollectors = FindObjectsOfType<ExperienceCollector>();
        Debug.Log($"PoisonPulse: Found {xpCollectors.Length} XP collectors");
        
        if (xpCollectors == null || xpCollectors.Length == 0) return;

        ExperienceCollector closestCollector = null;
        float closestDistance = float.MaxValue;
        
        foreach (var xp in xpCollectors)
        {
            if (xp == null) continue;
            float distance = Vector3.Distance(xp.transform.position, fromPos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollector = xp;
            }
        }
        
        if (closestCollector != null)
        {
            Debug.Log($"PoisonPulse: Attracting XP orb at distance {closestDistance}");
            AttractXPOrb(closestCollector);
        }
    }

    private void AttractXPOrb(ExperienceCollector xpCollector)
    {
        if (xpCollector == null) return;
        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) 
        {
            Debug.LogWarning("PoisonPulse: Player not found");
            return;
        }

        var attractMethod = xpCollector.GetType().GetMethod("AttractTo");
        if (attractMethod != null)
        {
            try 
            { 
                attractMethod.Invoke(xpCollector, new object[] { player.transform }); 
                Debug.Log("PoisonPulse: Used AttractTo method");
                return; 
            } 
            catch (System.Exception e) 
            {
                Debug.LogWarning($"PoisonPulse: Failed to invoke AttractTo: {e.Message}");
            }
        }

        var rb = xpCollector.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = ((Vector2)player.transform.position - (Vector2)xpCollector.transform.position).normalized;
            rb.velocity = dir * 15f;
            Debug.Log("PoisonPulse: Used Rigidbody2D velocity");
        }
        else
        {
            xpCollector.transform.position = Vector3.MoveTowards(xpCollector.transform.position, player.transform.position, 2f);
            Debug.Log("PoisonPulse: Used direct position movement");
        }
    }

    private void Update()
    {
        if (Time.time - lastTickTime > 30f) 
        {
            Debug.Log("PoisonPulse: Removing tracker due to inactivity");
            Destroy(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pulseRadius);
    }
}