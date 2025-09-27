using UnityEngine;

/// 1 уровень - Летит вперёд и возвращается
/// 2 уровень - Урон увеличен.
/// 3 уровень - Дальность увеличена.
/// 4 уровень - Возвращаясь, бумеранг оглушает врагов по пути.
/// 5 уровень - +1 Бумеранг (летит в противоположную сторону).

[CreateAssetMenu(menuName = "Weapons/Returning Boomerang")]
public class BoomerangWeaponData : WeaponBase
{
    [Header("Prefab")]
    public GameObject boomerangPrefab;

    [Header("Spawn")]
    [Tooltip("Время между бросками")]
    public float spawnInterval = 1.2f;

    [Header("Damage / Range / Speed")]
    [Tooltip("Урон за одно попадание (ур.1)")]
    public float baseDamage = 15f;
    [Tooltip("Множитель урона на ур.2")]
    public float level2DamageMultiplier = 1.5f;
    [Tooltip("Базовая дистанция полета туда (Unity units)")]
    public float baseMaxDistance = 4f;
    [Tooltip("Множитель дистанции на ур.3")]
    public float level3DistanceMultiplier = 1.5f;
    [Tooltip("Скорость полёта")]
    public float projectileSpeed = 12f;

    [Header("Stun (level 4)")]
    [Tooltip("Длительность оглушения (сек)")]
    public float stunDuration = 1.0f;

    [Header("Level 5 extra boomerang")]
    [Tooltip("Если true — при ур.5 появляется второй бумеранг в противоположную сторону")]
    public bool level5SpawnOpposite = true;

    public override IWeaponBehavior CreateBehavior(GameObject owner)
    {
        return owner.AddComponent<BoomerangBehavior>();
    }
}