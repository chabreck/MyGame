using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MagneticMutation : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags;
    private const int MAX_LEVEL = 5;

    [Header("Level Effects")]
    [SerializeField] private float radius = 10f;
    [SerializeField] private float pullForce = 2f;
    [SerializeField] private float attractionDamage = 5f;
    [SerializeField] private float explDamage = 20f;
    [SerializeField] private float explRadius = 3f;

    public string GetUpgradeID() => "MAGNETIC_MUTATION";
    public string GetTitle(int lvl) => $"Магнетическая Мутация {new string('I', lvl)}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;
    public string GetDescription(int lvl)
    {
        return lvl switch
        {
            1 => "Увеличен радиус сбора опыта",
            2 => "Сила притяжения периодически притягивает врагов",
            3 => "Сбор опыта наносит урон",
            4 => "Радиус притяжения ×1.5",
            5 => "С установкой взрывов при сборе опыта",
            _ => "Магнитная Мутация"
        };
    }
    
    public void Apply(int lvl)
    {
        var exp = FindObjectOfType<HeroExperience>();
        if (exp == null) return;
        switch (lvl)
        {
            case 1: exp.GetComponent<HeroModifierSystem>()?.AddModifier(StatType.ExperienceRadius, 0.2f); break;
            case 2: StartCoroutine(PullRoutine()); break;
            case 3: exp.OnExperienceCollected += _ => DamageAttract(); break;
            case 4:
                radius *= 1.5f;
                break;
            case 5: exp.OnExperienceCollected += _ => SetupExplosions(); break;
        }
    }

    private IEnumerator PullRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            var cols = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));
            foreach (var c in cols)
            {
                var rb = c.GetComponent<Rigidbody2D>();
                if (rb) rb.AddForce((transform.position - c.transform.position).normalized * pullForce);
            }
        }
    }

    private void DamageAttract()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
            c.GetComponent<IDamageable>()?.TakeDamage(attractionDamage);
    }

    private void SetupExplosions()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
        {
            var health = c.GetComponent<HeroHealth>();
            if (health != null)
            {
                health.OnDeath += () =>
                {
                    var cols2 = Physics2D.OverlapCircleAll(c.transform.position, explRadius, LayerMask.GetMask("Enemy"));
                    foreach (var e in cols2)
                        e.GetComponent<IDamageable>()?.TakeDamage(explDamage);
                };
            }
        }
    }
}