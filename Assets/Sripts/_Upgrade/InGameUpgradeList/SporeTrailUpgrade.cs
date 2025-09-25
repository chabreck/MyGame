using UnityEngine;
using System.Collections.Generic;

public class SporeTrail : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags = { "debuff", "area" };
    private const int MAX_LEVEL = 5;
    
    [Header("Level Effects")]
    [SerializeField] private float damagePerTick = 2f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float slowFactor = 0.4f;
    [SerializeField] private float explosionDamage = 25f;
    [SerializeField] private float explosionRadius = 3f;

    private SporeTrailEffect activeTrail;

    public string GetUpgradeID() => "SPORE_TRAIL";
    public string GetTitle(int level) => $"Споровый Шлейф {(level > 1 ? "I".PadRight(level-1, 'I') : "")}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int level) => level switch
    {
        1 => "Шлейф замедляет врагов",
        2 => "Враги получают урон каждые 0.5 сек",
        3 => "+50% к длине шлейфа",
        4 => "Враги теряют на 10% больше здоровья при отравлении",
        5 => "Враги, умершие в шлейфе, взрываются",
        _ => "Споровый Шлейф"
    };

    public void Apply(int level)
    {
        if (activeTrail == null)
            activeTrail = gameObject.AddComponent<SporeTrailEffect>();
        switch (level)
        {
            case 1:
                activeTrail.slowFactor = slowFactor;
                break;
            case 2:
                activeTrail.damagePerTick = damagePerTick;
                activeTrail.tickInterval = tickInterval;
                break;
            case 3:
                activeTrail.trailLength *= 1.5f;
                break;
            case 4:
                activeTrail.damageMultiplier = 1.1f;
                break;
            case 5:
                activeTrail.enableDeathExplosion = true;
                activeTrail.explosionDamage = explosionDamage;
                activeTrail.explosionRadius = explosionRadius;
                break;
        }
    }
}

public class SporeTrailEffect : MonoBehaviour
{
    public float trailLength = 3f;
    public float slowFactor = 0f;
    public float damagePerTick = 0f;
    public float tickInterval = 1f;
    public float damageMultiplier = 1f;
    public bool enableDeathExplosion = false;
    public float explosionDamage = 0f;
    public float explosionRadius = 0f;

    private float lastTickTime;
    private List<EnemyStatus> affectedEnemies = new List<EnemyStatus>();

    private void Update()
    {
        UpdateTrailEffect();
        ProcessDamageTicks();
    }

    private void UpdateTrailEffect()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, trailLength, LayerMask.GetMask("Enemy"));
        foreach (var enemy in enemies)
        {
            var status = enemy.GetComponent<EnemyStatus>();
            if (status != null && !affectedEnemies.Contains(status))
            {
                status.ApplySlow(slowFactor, Mathf.Infinity);
                affectedEnemies.Add(status);
            }
        }
    }

    private void ProcessDamageTicks()
    {
        if (Time.time - lastTickTime < tickInterval) return;
        lastTickTime = Time.time;

        for (int i = affectedEnemies.Count - 1; i >= 0; i--)
        {
            var status = affectedEnemies[i];
            if (status == null)
            {
                affectedEnemies.RemoveAt(i);
                continue;
            }

            status.TakeDamage(damagePerTick * damageMultiplier);

            if (enableDeathExplosion && status.IsDead)
            {
                ExplodeOnDeath(status.transform.position);
                affectedEnemies.RemoveAt(i);
            }
        }
    }

    private void ExplodeOnDeath(Vector3 position)
    {
        Collider[] enemies = Physics.OverlapSphere(position, explosionRadius, LayerMask.GetMask("Enemy"));
        foreach (var enemy in enemies)
        {
            var status = enemy.GetComponent<EnemyStatus>();
            status?.TakeDamage(explosionDamage);
        }
    }
}
