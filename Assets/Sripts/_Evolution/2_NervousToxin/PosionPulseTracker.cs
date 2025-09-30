using UnityEngine;

public class PoisonPulseTracker : MonoBehaviour
{
    [Header("Pulse Settings")]
    public GameObject pulsePrefab;
    
    private GameObject ownerPlayer;
    private GameObject sourceEnemy;
    private float pulseBase = 6f;
    private float pulseRadius = 2.5f;
    private float pulseScale = 0.25f;
    private float xpChance = 0.06f;
    private float lastTickTime = -999f;

    public void Configure(GameObject owner, GameObject enemy, float baseDamage, float radius, 
                         float scaleFromTick, float xpAttractChance, GameObject visualPrefab)
    {
        ownerPlayer = owner;
        sourceEnemy = enemy;
        pulseBase = baseDamage;
        pulseRadius = radius;
        pulseScale = scaleFromTick;
        xpChance = xpAttractChance;
        pulsePrefab = visualPrefab;
    }

    public void OnPoisonTick(float tickDamage)
    {
        lastTickTime = Time.time;
        float dmg = pulseBase + tickDamage * pulseScale;
        
        if (pulsePrefab != null)
        {
            CreatePulseEffect(dmg);
        }
        else
        {
            ApplyPulseDamage(dmg);
        }
        
        if (Random.value <= xpChance) 
        {
            TryAttractXPOrb();
        }
    }

    private void CreatePulseEffect(float damage)
    {
        GameObject pulse = Instantiate(pulsePrefab, transform.position, Quaternion.identity);
        
        var pulseComponent = pulse.GetComponent<PoisonPulseEffect>();
        if (pulseComponent == null)
        {
            pulseComponent = pulse.AddComponent<PoisonPulseEffect>();
        }
        
        pulseComponent.Initialize(ownerPlayer, sourceEnemy, damage, pulseRadius);
    }

    private void ApplyPulseDamage(float damage)
    {
        Vector2 center = transform.position;
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, pulseRadius);
        
        foreach (var c in cols)
        {
            if (c == null) continue;
            var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
            if (es == null) continue;
            if (es.gameObject == sourceEnemy) continue;
            
            DamageHelper.ApplyDamage(ownerPlayer, es, damage, raw: false, 
                popupType: DamagePopup.DamageType.Poison,
                sourceType: DamageHelper.DamageSourceType.Pulse);
        }
    }

    private void TryAttractXPOrb()
    {
        var xpCollectors = FindObjectsOfType<ExperienceCollector>();
        if (xpCollectors == null || xpCollectors.Length == 0) return;

        ExperienceCollector closestCollector = null;
        float closestDistance = float.MaxValue;
        
        foreach (var xp in xpCollectors)
        {
            if (xp == null) continue;
            float distance = Vector3.Distance(xp.transform.position, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollector = xp;
            }
        }
        
        if (closestCollector != null)
        {
            AttractXPOrb(closestCollector);
        }
    }

    private void AttractXPOrb(ExperienceCollector xpCollector)
    {
        if (xpCollector == null) return;
        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var attractMethod = xpCollector.GetType().GetMethod("AttractTo");
        if (attractMethod != null)
        {
            try 
            { 
                attractMethod.Invoke(xpCollector, new object[] { player.transform }); 
                return; 
            } 
            catch { }
        }

        Vector3 direction = (player.transform.position - xpCollector.transform.position).normalized;
        xpCollector.transform.position = player.transform.position - direction * 2f;
    }

    private void Update()
    {
        if (Time.time - lastTickTime > 30f) 
            Destroy(this);
    }
}