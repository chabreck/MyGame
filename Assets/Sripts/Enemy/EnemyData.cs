using UnityEngine;

public enum EnemyAttackType { None, ThrowPoison /* и т.д. */ }

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string id;

    [Header("Stats")]
    public int health;
    public int damage;
    public float speed;
    public float damageCooldown;
    public float spawnTime;

    [Header("Reward")]
    [Tooltip("Опыт, выдаваемый при гибели")]
    public int expReward;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Unique Attack")]
    public EnemyAttackType attackType;
    public float attackRange;
    public float attackRate;
    public int attackDamage;
}