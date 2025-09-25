using System.Collections;
using UnityEngine;

public class ThirdEye : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags;
    private const int MAX_LEVEL = 5;

    [Header("Level Effects")]
    [SerializeField] private float expRadius = 0.2f;
    [SerializeField] private float rareChance = 0.1f;
    [SerializeField] private float expBoost = 0.15f;
    [SerializeField] private float gazeDamage = 10f;
    [SerializeField] private float gazeRadius = 8f;
    [SerializeField] private float gazeInterval = 1f;

    public string GetUpgradeID() => "THIRD_EYE";
    public string GetTitle(int lvl) => $"Третье Сетчатое Око {new string('I', lvl)}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int lvl) => lvl switch
    {
        1 => "Увеличен радиус подбора опыта",
        2 => "Появляются скрытые ресурсы",
        3 => "+10% шанс найти редкий ресурс",
        4 => "Эффекты опыта усиливаются",
        5 => "Враги в зоне взгляда получают урон",
        _ => "Третье Сетчатое Око"
    };

    public void Apply(int lvl)
    {
        var exp = FindObjectOfType<HeroExperience>();
        if (exp == null) return;
        switch (lvl)
        {
            case 1: exp.GetComponent<HeroModifierSystem>()?.AddModifier(StatType.ExperienceRadius, expRadius); break;
            case 2: exp.OnExperienceCollected += _ => CollectHidden(); break;
            case 3: exp.GetComponent<HeroExperience>().OnExperienceCollected += amt => TryRare(amt); break;
            case 4: exp.GetComponent<HeroModifierSystem>()?.AddModifier(StatType.CollectionSpeed, expBoost); break;
            case 5: StartCoroutine(GazeRoutine()); break;
        }
    }

    private IEnumerator GazeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(gazeInterval);
            var cols = Physics2D.OverlapCircleAll(transform.position, gazeRadius, LayerMask.GetMask("Enemy"));
            foreach (var c in cols)
                c.GetComponent<IDamageable>()?.TakeDamage(gazeDamage);
        }
    }

    private void CollectHidden() { /* implementation dependent */ }
    private void TryRare(int amt) { /* random resource spawn */ }
}