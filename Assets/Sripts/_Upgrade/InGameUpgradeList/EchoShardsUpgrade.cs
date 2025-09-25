using UnityEngine;

public class EchoShards : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags;
    private const int MAX_LEVEL = 5;

    [Header("Level Effects")]
    [SerializeField] private float baseReflect = 0.05f;
    [SerializeField] private float aoeRadius = 3f;
    [SerializeField] private float explosionChance = 0.2f;
    [SerializeField] private float maxReflect = 0.15f;
    [SerializeField] private float healOnReflect = 1f;

    public string GetUpgradeID() => "ECHO_SHARDS";
    public string GetTitle(int lvl) => $"Осколки Эха {new string('I', lvl)}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int lvl) => lvl switch
    {
        1 => "5% отражение урона",
        2 => "Отражение наносит AOE урон",
        3 => "Шанс отражать взрывной урон",
        4 => "15% отражение урона",
        5 => "Восстановление HP при отражении",
        _ => "Осколки Эха"
    };

    public void Apply(int lvl)
    {
        var health = GetComponent<HeroHealth>();
        if (health == null) return;
        switch (lvl)
        {
            case 1: health.damageReflection = baseReflect; break;
            case 2: health.OnDamageReflected += ApplyAOE; break;
            case 3: health.explosiveReflectionChance = explosionChance; break;
            case 4: health.damageReflection = maxReflect; break;
            case 5: health.OnDamageReflected += HealOnReflect; break;
        }
    }

    private void ApplyAOE(float dmg, Vector3 pos)
    {
        var cols = Physics2D.OverlapCircleAll(pos, aoeRadius, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
            c.GetComponent<IDamageable>()?.TakeDamage(dmg * 0.5f);
    }

    private void HealOnReflect(float dmg, Vector3 pos)
    {
        GetComponent<HeroHealth>()?.Heal(healOnReflect);
    }
}