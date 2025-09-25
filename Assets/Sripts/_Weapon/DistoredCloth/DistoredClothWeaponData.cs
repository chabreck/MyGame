using UnityEngine;

/// 1 уровень: 3 шипа каждые 1.5 сек.
/// 2 уровень: +2 шипа и усиленный урон.
/// 3 уровень: Шип проходит 1 врага насквозь (то есть наносит урон и первому врагу и который за ним)
/// 4 уровень: При каждом выпуске есть шанс (20–30%) удвоить количество шипов. 
/// 5 уровень: Все шипы накладывают яд (3 сек).

[CreateAssetMenu(menuName = "Weapons/Distorted Cloth")]
public class DistortedClothWeapon : WeaponBase
{
    public GameObject spikePrefab;
    public float spawnInterval = 1.5f;
    public int baseSpikeCount = 3;
    public float spikeSpeed = 6f;
    public float spikeDamage = 13f;
    public float level2DamageMultiplier = 1.4f;
    public float level4DoubleChance = 0.25f;
    public float spreadAngle = 20f;
    public float spawnOffset = 0.6f;
    public string projectileLayer = "Projectile";
    public float poisonTick = 6f;
    public float poisonInterval = 1f;
    public float poisonDuration = 3f;
    public float spikeLifetime = 4f;

    public override IWeaponBehavior CreateBehavior(GameObject owner)
    {
        return owner.AddComponent<DistortedClothBehavior>();
    }
}