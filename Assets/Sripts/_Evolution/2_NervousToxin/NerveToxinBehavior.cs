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
    }

    public void Initialize(GameObject ownerGO, WeaponBase wb, HeroModifierSystem mods, HeroCombat combat)
    {
        owner = ownerGO;
        data = wb as NerveToxinEvolutionData;
        level = 1;
    }

    public void Activate() { }

    public void OnUpgrade(int lvl)
    {
        level = Mathf.Clamp(lvl, 1, data != null ? data.maxLevel : lvl);
    }

    public void OnEnemyPoisonTick(GameObject enemy, float tickDamage)
    {
        if (enemy == null || owner == null) return;
        
        var enemyStatus = enemy.GetComponent<EnemyStatus>();
        if (enemyStatus == null) return;
        
        var ppt = enemy.GetComponent<PoisonPulseTracker>();
        if (ppt == null) 
        {
            ppt = enemy.AddComponent<PoisonPulseTracker>();
        }
        
        float basePulse = data != null ? data.pulseBaseDamage : 6f;
        float radius = data != null ? data.pulseRadius : 2.5f;
        float scale = data != null ? data.pulseScaleFromPoisonTick : 0.25f;
        float xpChance = data != null ? data.xpAttractChancePerPoisonTick : 0.06f;
        GameObject pulsePrefab = data != null ? data.pulseVisualPrefab : null;
        
        ppt.Configure(owner, enemy, basePulse, radius, scale, xpChance, pulsePrefab);
        ppt.OnPoisonTick(tickDamage);
    }

    private void OnDestroy()
    {
        if (Instance == this) 
        {
            Instance = null;
        }
    }
}