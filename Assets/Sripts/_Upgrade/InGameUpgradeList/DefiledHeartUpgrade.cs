using UnityEngine;

public class DefiledHeart : MonoBehaviour, IUpgrade
{
    [Header("Settings")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string[] tags;
    private const int MAX_LEVEL = 5;

    [Header("Level Effects")]
    [SerializeField] private int killsPerHeal = 10;
    [SerializeField] private float healPerKill = 0.1f;
    [SerializeField] private float lifeShardChance = 0.3f;
    [SerializeField] private int shieldThreshold = 5;
    [SerializeField] private float shieldDuration = 5f;

    private int killCount;
    private int shardCount;

    public string GetUpgradeID() => "DEFILED_HEART";
    public string GetTitle(int lvl) => $"Осквернённое Сердце {new string('I', lvl)}";
    public Sprite Icon => icon;
    public string[] Tags => tags;
    public int MaxLevel => MAX_LEVEL;

    public string GetDescription(int lvl) => lvl switch
    {
        1 => "+1 HP за 10 убийств",
        2 => "Эффект усиливается рядом с врагами",
        3 => "Враги оставляют осколки жизни",
        4 => "+2 HP за 10 убийств",
        5 => "Каждый 5-й осколок даёт щит",
        _ => "Осквернённое Сердце"
    };

    public void Apply(int lvl)
    {
        var combat = GetComponent<HeroCombat>();
        if (combat == null) return;
        switch (lvl)
        {
            case 1: combat.OnAttack += CountKill; break;
            case 2: healPerKill = 0.2f; break;
            case 3: combat.OnAttack += SpawnShard; break;
            case 4: killsPerHeal = 5; break;
            case 5: LifeShard.onCollected += CountShard; break;
        }
    }

    private void CountKill(Vector3 pos)
    {
        killCount++;
        if (killCount >= killsPerHeal)
        {
            killCount = 0;
            GetComponent<HeroHealth>()?.Heal(1);
        }
    }

    private void SpawnShard(Vector3 pos)
    {
        if (Random.value < lifeShardChance)
            LifeShard.Spawn(pos);
    }

    private void CountShard()
    {
        shardCount++;
        if (shardCount >= shieldThreshold)
        {
            shardCount = 0;
            GetComponent<HeroHealth>()?.AddShield(shieldDuration);
        }
    }
}

public class LifeShard : MonoBehaviour
{
    public static System.Action onCollected;

    public static void Spawn(Vector3 pos)
    {
        var shard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shard.transform.position = pos;
        shard.AddComponent<LifeShard>();
        shard.GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<HeroHealth>()?.Heal(1);
        onCollected?.Invoke();
        Destroy(gameObject);
    }
}