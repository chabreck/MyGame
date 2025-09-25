using System.Collections;
using UnityEngine;

public class RealityEcho : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags;
    private const int MAX_LEVEL = 5;

    [Header("Level Effects")]
    [SerializeField] private float cloneDuration = 2f;
    [SerializeField] private float cloneDamage = 0.5f;
    [SerializeField] private float knockback = 5f;
    [SerializeField] private float explosionDamage = 30f;

    private int count;

    public string GetUpgradeID() => "REALITY_ECHO";
    public string GetTitle(int lvl) => $"Эхо Реальности {new string('I', lvl)}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int lvl) => lvl switch
    {
        1 => "Копия действует 2 сек",
        2 => "Урон копии 50%",
        3 => "Копия отбрасывает врагов",
        4 => "Время жизни +2 сек",
        5 => "Копия взрывается при исчезновении",
        _ => "Эхо Реальности"
    };

    public void Apply(int lvl)
    {
        count++;
        if (count % 3 == 0)
            SpawnClone();
    }

    private void SpawnClone()
    {
        var clone = Instantiate(gameObject, transform.position, transform.rotation);
        Destroy(clone.GetComponent<RealityEcho>());
        var comp = clone.AddComponent<RealityEchoClone>();
        comp.Setup(cloneDuration, cloneDamage, knockback, explosionDamage);
    }
}

public class RealityEchoClone : MonoBehaviour
{
    private float duration;
    private float damageMult;
    private float kb;
    private float expl;

    public void Setup(float dur, float dmg, float k, float e)
    {
        duration = dur;
        damageMult = dmg;
        kb = k;
        expl = e;
        Destroy(gameObject, dur);
    }

    private void Update()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, 10f, LayerMask.GetMask("Enemy"));
        if (cols.Length > 0)
        {
            var target = cols[0].transform;
            transform.position = Vector3.MoveTowards(transform.position, target.position, 3f * Time.deltaTime);
        }
    }

    private void OnTriggerStay2D(Collider2D c)
    {
        if (c.CompareTag("Enemy"))
        {
            c.GetComponent<IDamageable>()?.TakeDamage(10 * damageMult * Time.deltaTime);
            c.GetComponent<Rigidbody2D>()?.AddForce((c.transform.position - transform.position).normalized * kb);
        }
    }

    private void OnDestroy()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, 4f, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
            c.GetComponent<IDamageable>()?.TakeDamage(expl);
    }
}