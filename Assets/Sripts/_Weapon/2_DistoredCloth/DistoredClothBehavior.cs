using UnityEngine;

[DisallowMultipleComponent]
public class DistortedClothBehavior : MonoBehaviour, IWeaponBehavior
{
    private DistortedClothWeapon data;
    private int level = 1;
    private float cooldown = 0f;
    private GameObject owner;

    public void Initialize(GameObject owner, WeaponBase wb, HeroModifierSystem mods, HeroCombat combat)
    {
        this.owner = owner;
        data = wb as DistortedClothWeapon;
        if (data == null) { enabled = false; return; }
        level = 1;
        cooldown = 0f;
    }

    public void Activate()
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;
        ShootSpikes();
        cooldown = data.spawnInterval;
    }

    public void OnUpgrade(int lvl) { level = Mathf.Clamp(lvl, 1, data.maxLevel); }

    private void ShootSpikes()
    {
        if (data == null || data.spikePrefab == null || owner == null) return;

        int extraAtLv2 = 2;
        int count = data.baseSpikeCount + (level >= 2 ? extraAtLv2 : 0);

        if (level >= 4)
        {
            float roll = Random.value;
            if (roll <= data.level4DoubleChance) count *= 2;
        }

        float dmg = data.spikeDamage * (level >= 2 ? data.level2DamageMultiplier : 1f);

        Vector2 aimDir = GetDirectionToNearestEnemy();
        if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.up;

        float spread = data.spreadAngle;
        float baseAngle = -((count - 1) * spread) / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + i * spread;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * aimDir.normalized;
            Vector3 spawnPos = owner.transform.position + (Vector3)(dir * data.spawnOffset);
            GameObject go = Instantiate(data.spikePrefab, spawnPos, Quaternion.identity);
            try { go.layer = LayerMask.NameToLayer(data.projectileLayer); } catch { }

            DistoredClothBullet spike = go.GetComponent<DistoredClothBullet>();
            if (spike == null)
            {
                spike = go.AddComponent<DistoredClothBullet>();
            }

            int penetration = level >= 3 ? 2 : 1;
            bool poison = level >= 5;

            spike.Initialize(owner, dir, data.spikeSpeed, dmg, data.spikeLifetime, penetration, poison, data.poisonTick, data.poisonInterval, data.poisonDuration, data.projectileLayer);
        }
    }

    private Vector2 GetDirectionToNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0) return Vector2.up;
        GameObject nearest = null;
        float best = float.MaxValue;
        foreach (var e in enemies)
        {
            if (e == null) continue;
            float d = Vector2.SqrMagnitude(e.transform.position - transform.position);
            if (d < best) { best = d; nearest = e; }
        }
        return (nearest != null) ? (nearest.transform.position - transform.position).normalized : Vector2.up;
    }
}
