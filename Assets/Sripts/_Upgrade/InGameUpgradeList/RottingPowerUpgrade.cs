using UnityEngine;

public class RottingPower : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags;
    private const int MAX_LEVEL = 5;

    [Header("Level Effects")]
    [SerializeField] private float lowHpBoost1 = 0.1f;
    [SerializeField] private float lowHpBoost2 = 0.2f;
    [SerializeField] private float shieldDur = 2f;
    [SerializeField] private float rotDmg = 5f;
    [SerializeField] private float rotDur = 3f;
    [SerializeField] private float critBoost = 1f;

    public string GetUpgradeID() => "ROTTING_POWER";
    public string GetTitle(int lvl) => $"Гниющая Сила {new string('I', lvl)}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int lvl) => lvl switch
    {
        1 => "+10% урона при HP <50%",
        2 => "+20% урона при HP <30%",
        3 => "При низком HP даёт щит",
        4 => "Атаки накладывают гниение",
        5 => "×2 урон при HP <25%",
        _ => "Гниющая Сила"
    };

    public void Apply(int lvl)
    {
        var health = GetComponent<HeroHealth>();
        var combat = GetComponent<HeroCombat>();
        if (health == null || combat == null) return;
        switch (lvl)
        {
            case 1: health.OnLowHealth += () => combat.AddDamageBoost(lowHpBoost1, float.MaxValue); break;
            case 2: health.OnCriticalHealth += () => combat.AddDamageBoost(lowHpBoost2, float.MaxValue); break;
            case 3: health.OnLowHealth += () => health.AddShield(shieldDur); break;
            case 4: combat.OnAttack += ApplyRot; break;
            case 5: health.OnCriticalHealth += () => combat.AddDamageBoost(critBoost, float.MaxValue); break;
        }
    }

    private void ApplyRot(Vector3 pos)
    {
        var cols = Physics2D.OverlapCircleAll(pos, 3f, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
            c.GetComponent<RotEffect>()?.ApplyRot(rotDmg, rotDur);
    }
}

public class RotEffect : MonoBehaviour
{
    private float end;
    private float dps;

    public void ApplyRot(float dmg, float dur)
    {
        dps = dmg;
        end = Time.time + dur;
    }

    private void Update()
    {
        if (Time.time < end)
            GetComponent<IDamageable>()?.TakeDamage(dps * Time.deltaTime);
        else
            Destroy(this);
    }
}