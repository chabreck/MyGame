using UnityEngine;

[DisallowMultipleComponent]
public class NervousToxinBehavior : MonoBehaviour, IWeaponBehavior
{
    public static NervousToxinBehavior Instance { get; private set; }
    private NervousToxinEvolutionData data;
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
        Debug.Log("NervousToxinBehavior: Instance created");
    }

    public void Initialize(GameObject ownerGO, WeaponBase wb, HeroModifierSystem mods, HeroCombat combat)
    {
        owner = ownerGO;
        data = wb as NervousToxinEvolutionData;
        level = 1;
        Debug.Log($"NervousToxinBehavior: Initialized with owner {ownerGO.name}, data: {(data != null ? data.name : "null")}");
    }

    public void Activate() { }

    public void OnUpgrade(int lvl)
    {
        level = Mathf.Clamp(lvl, 1, data != null ? data.maxLevel : lvl);
        Debug.Log($"NervousToxinBehavior: Upgraded to level {level}");
    }

    public void OnEnemyPoisonTick(GameObject enemy, float tickDamage)
    {
        if (enemy == null) 
        {
            Debug.LogWarning("NervousToxinBehavior: enemy is null");
            return;
        }
        
        if (owner == null)
        {
            Debug.LogWarning("NervousToxinBehavior: owner is null");
            return;
        }
        
        var ppt = enemy.GetComponent<PoisonPulseTracker>();
        if (ppt == null) 
        {
            ppt = enemy.AddComponent<PoisonPulseTracker>();
            Debug.Log($"NervousToxinBehavior: Added PoisonPulseTracker to {enemy.name}");
        }
        
        float basePulse = data != null ? data.pulseBaseDamage : 6f;
        float radius = data != null ? data.pulseRadius : 2.5f;
        float scale = data != null ? data.pulseScaleFromPoisonTick : 0.25f;
        float xpChance = data != null ? data.xpAttractChancePerPoisonTick : 0.06f;
        float xpRadius = data != null ? data.xpAttractRadius : 10f;
        
        ppt.Configure(owner, enemy, basePulse, radius, scale, xpChance, xpRadius);
        ppt.OnPoisonTick(tickDamage);
        
        Debug.Log($"NervousToxinBehavior: Processed poison tick on {enemy.name}, damage: {tickDamage}");
    }

    private void OnDestroy()
    {
        if (Instance == this) 
        {
            Instance = null;
            Debug.Log("NervousToxinBehavior: Instance destroyed");
        }
    }
}