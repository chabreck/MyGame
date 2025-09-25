using UnityEngine;

public class LivingCarapace : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags = { "defense", "organic" };
    private const int MAX_LEVEL = 5;
    
    [Header("Level Effects")]
    [SerializeField] private float level1DamageReduction = 0.1f;
    [SerializeField] private float level2DodgeChance = 0.05f;
    [SerializeField] private float level4DamageReduction = 0.2f;
    [SerializeField] private float spikeDamage = 20f;
    [SerializeField] private float spikeRadius = 3f;
    [SerializeField] private float slowDuration = 1f;
    [SerializeField] private float slowFactor = 0.5f;

    public string GetUpgradeID() => "LIVING_CARAPACE";
    public string GetTitle(int level) => $"Живой Панцирь {(level > 1 ? "I".PadRight(level-1, 'I') : "")}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int level)
    {
        return level switch
        {
            1 => "-10% получаемого урона",
            2 => "+5% шанс игнорировать урон полностью",
            3 => "После получения урона враги рядом замедляются на 1 сек",
            4 => "-20% получаемого урона",
            5 => "При получении урона выпускается шип (AOE урон)",
            _ => "Живой Панцирь"
        };
    }

    public void Apply(int level)
    {
        var health = GetComponent<HeroHealth>();
        if (!health) return;

        switch (level)
        {
            case 1:
                health.damageReduction += level1DamageReduction;
                break;
            case 2:
                health.dodgeChance += level2DodgeChance;
                break;
            case 3:
                health.OnDamageTaken += SlowEnemiesOnHit;
                break;
            case 4:
                health.damageReduction = level4DamageReduction;
                break;
            case 5:
                health.OnDamageTaken += _ => SpikeOnHit();
                break;
        }
    }

    private void SlowEnemiesOnHit(float damageTaken)
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, spikeRadius, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
            c.GetComponent<EnemyStatus>()?.ApplySlow(slowFactor, slowDuration);
    }


    private void SpikeOnHit()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, spikeRadius, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
            c.GetComponent<IDamageable>()?.TakeDamage(spikeDamage);
    }

}