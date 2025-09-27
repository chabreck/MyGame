using UnityEngine;

/// 1 уровень - Один лепесток вращается вокруг игрока.
/// 2 уровень - +1 лепесток. Урон увеличен.
/// 3 уровень - Скорость вращения увеличена.
/// 4 уровень - +2 лепестка. Увеличена дальность вращения (лепестки дальше от игрока).
/// 5 уровень - Лепестки пробивают врагов (не останавливаются на первом контакте).

[CreateAssetMenu(menuName = "Weapons/Digital Petal")]
public class DigitalPetalWeapon : WeaponBase
{
    [Header("Visual / Prefab")]
    [Tooltip("Prefab лепестка. Должен содержать Collider2D (isTrigger = true) и опционально SpriteRenderer.")]
    public GameObject petalPrefab;

    [Header("Geometry")]
    [Tooltip("Расстояние лепестка от центра игрока (Unity units)")]
    public float radius = 1.5f;

    [Header("Base stats")]
    [Tooltip("Базовое число лепестков (ур.1)")]
    public int basePetals = 1;
    [Tooltip("Базовая скорость вращения (градусы в секунду) — 720deg = 2 оборота/сек")]
    public float baseRotationSpeed = 720f;
    [Tooltip("Урон за попадание (ур.1)")]
    public float baseDamage = 12f;
    [Tooltip("Минимальное время между попаданиями от одного лепестка в один враг (сек)")]
    public float hitCooldownPerEnemy = 0.45f;

    [Header("Level scaling")]
    [Tooltip("Доп. лепестки на уровне 2")]
    public int addAtLevel2 = 1; // +1 petal
    [Tooltip("Множитель урона начиная с уровня 2")]
    public float level2DamageMultiplier = 1.25f;
    [Tooltip("Множитель скорости на уровне 3")]
    public float level3SpeedMultiplier = 1.5f;
    [Tooltip("Доп. лепестки на уровне 4")]
    public int addAtLevel4 = 2; // +2 petals on lvl4
    [Tooltip("Множитель радиуса на уровне 4 (лепестки дальше от игрока)")]
    public float level4RadiusMultiplier = 1.5f;

    [Header("Petal cooldown (when petal is consumed on hit)")]
    [Tooltip("Сколько секунд лепесток остаётся 'исчезшим' после попадания (если не piercing).")]
    public float petalCooldown = 0.6f;

    public override IWeaponBehavior CreateBehavior(GameObject owner)
    {
        return owner.AddComponent<OrbitalPetalBehavior>();
    }
}