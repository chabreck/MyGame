using UnityEngine;

[DisallowMultipleComponent]
public class BoomerangBehavior : MonoBehaviour, IWeaponBehavior
{
    private BoomerangWeaponData d;
    private GameObject owner;
    private HeroModifierSystem mods;
    private HeroCombat combat;

    private int level = 1;
    private float cooldown = 0f;

    public void Initialize(GameObject owner, WeaponBase data, HeroModifierSystem mods, HeroCombat combat)
    {
        this.owner = owner;
        this.mods = mods;
        this.combat = combat;
        d = data as BoomerangWeaponData;
        if (d == null) { Debug.LogError("BoomerangBehavior: invalid data assigned"); enabled = false; return; }
        level = 1;
        cooldown = 0f;
    }

    public void Activate()
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;
        LaunchBoomerangRoutine();
        cooldown = d.spawnInterval;
    }

    public void OnUpgrade(int lvl) { level = Mathf.Clamp(lvl, 1, d.maxLevel); }

    private void LaunchBoomerangRoutine()
    {
        Vector2 dir = GetDirectionToNearestEnemy();
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;

        float dmg = d.baseDamage * (level >= 2 ? d.level2DamageMultiplier : 1f);
        float maxDist = d.baseMaxDistance * (level >= 3 ? d.level3DistanceMultiplier : 1f);
        float speed = d.projectileSpeed;
        bool stunOnReturn = level >= 4;
        float stunDuration = d.stunDuration;

        SpawnBoomerang(dir, speed, maxDist, dmg, stunOnReturn, stunDuration);

        if (level >= 5 && d.level5SpawnOpposite) SpawnBoomerang(-dir, speed, maxDist, dmg, stunOnReturn, stunDuration);
    }

    private void SpawnBoomerang(Vector2 dir, float speed, float maxDist, float damage, bool stunOnReturn, float stunDuration)
    {
        if (d.boomerangPrefab == null || owner == null) return;

        Vector3 spawnPos = owner.transform.position + (Vector3)(dir.normalized * 0.4f);
        GameObject go = Instantiate(d.boomerangPrefab, spawnPos, Quaternion.identity);
        try { go.layer = LayerMask.NameToLayer("Projectile"); } catch { }

        var proj = go.GetComponent<BoomerangBullet>();
        if (proj == null) proj = go.AddComponent<BoomerangBullet>();

        proj.Initialize(owner, dir.normalized, speed, maxDist, damage, stunOnReturn, stunDuration);
    }

    private Vector2 GetDirectionToNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0) return Vector2.up;
        GameObject nearest = null; float best = float.MaxValue;
        foreach (var e in enemies)
        {
            if (e == null) continue;
            float dist = (e.transform.position - transform.position).sqrMagnitude;
            if (dist < best) { best = dist; nearest = e; }
        }
        return (nearest != null) ? (nearest.transform.position - transform.position).normalized : Vector2.up;
    }
}
