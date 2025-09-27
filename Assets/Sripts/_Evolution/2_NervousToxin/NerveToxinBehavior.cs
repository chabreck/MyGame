using UnityEngine;

[DisallowMultipleComponent]
public class NerveToxinBehavior : MonoBehaviour, IWeaponBehavior
{
    public static NerveToxinBehavior Instance { get; private set; }
    private NerveToxinEvolutionData data;
    private GameObject owner;
    private int level = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Debug.Log("NerveToxinBehavior: Instance created");
    }

    public void Initialize(GameObject ownerGO, WeaponBase wb, HeroModifierSystem mods, HeroCombat combat)
    {
        owner = ownerGO;
        data = wb as NerveToxinEvolutionData;
        level = 1;
        Debug.Log($"NerveToxinBehavior: Initialized with owner {ownerGO.name}");
    }

    public void Activate() { }

    public void OnUpgrade(int lvl)
    {
        level = Mathf.Clamp(lvl, 1, data != null ? data.maxLevel : lvl);
        Debug.Log($"NerveToxinBehavior: Upgraded to level {level}");
    }

    public void OnEnemyPoisonTick(GameObject enemy, float tickDamage)
    {
        if (enemy == null) 
        {
            Debug.LogWarning("NerveToxinBehavior: enemy is null");
            return;
        }
        
        if (owner == null)
        {
            Debug.LogWarning("NerveToxinBehavior: owner is null");
            return;
        }
        
        var enemyStatus = enemy.GetComponent<EnemyStatus>();
        if (enemyStatus == null)
        {
            Debug.LogWarning($"NerveToxinBehavior: enemy {enemy.name} has no EnemyStatus");
            return;
        }
        
        var ppt = enemy.GetComponent<PoisonPulseTracker>();
        if (ppt == null) 
        {
            ppt = enemy.AddComponent<PoisonPulseTracker>();
            Debug.Log($"NerveToxinBehavior: Added PoisonPulseTracker to {enemy.name}");
        }
        
        float basePulse = data != null ? data.pulseBaseDamage : 6f;
        float radius = data != null ? data.pulseRadius : 2.5f;
        float scale = data != null ? data.pulseScaleFromPoisonTick : 0.25f;
        float xpChance = data != null ? data.xpAttractChancePerPoisonTick : 0.06f;
        float xpRadius = 1000f;
        
        ppt.Configure(owner, enemy, basePulse, radius, scale, xpChance, xpRadius);
        ppt.OnPoisonTick(tickDamage);
        
        Debug.Log($"NerveToxinBehavior: Processed poison tick on {enemy.name}, damage: {tickDamage}");
    }

    private void OnDestroy()
    {
        if (Instance == this) 
        {
            Instance = null;
            Debug.Log("NerveToxinBehavior: Instance destroyed");
        }
    }
}