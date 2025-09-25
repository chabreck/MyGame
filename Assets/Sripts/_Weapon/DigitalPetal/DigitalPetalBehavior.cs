using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DigitalPetalBehavior : MonoBehaviour, IWeaponBehavior
{
    private DigitalPetalWeapon data;
    private GameObject owner;
    private int level = 1;

    private List<GameObject> petals = new List<GameObject>();
    private List<float> petalAngles = new List<float>();

    private int PetalCount => data.basePetals + (level >= 2 ? data.addAtLevel2 : 0) + (level >= 4 ? data.addAtLevel4 : 0);
    private float RotationSpeed => data.baseRotationSpeed * (level >= 3 ? data.level3SpeedMultiplier : 1f);
    private float CurrentDamage => data.baseDamage * (level >= 2 ? data.level2DamageMultiplier : 1f);
    private float CurrentRadius => data.radius * (level >= 4 ? data.level4RadiusMultiplier : 1f);

    public void Initialize(GameObject ownerGO, WeaponBase wb, HeroModifierSystem mods, HeroCombat combat)
    {
        owner = ownerGO;
        data = wb as DigitalPetalWeapon;
        if (data == null) { Debug.LogError("DigitalPetalBehavior: invalid WeaponData"); enabled = false; return; }
        level = 1;
        RebuildPetals();
    }

    public void Activate()
    {
        if (owner == null) return;
        if (petals.Count == 0) return;

        float delta = RotationSpeed * Time.deltaTime;
        for (int i = 0; i < petalAngles.Count; i++) petalAngles[i] += delta;

        for (int i = 0; i < petals.Count; i++)
        {
            var p = petals[i];
            if (p == null) continue;
            float angleRad = petalAngles[i] * Mathf.Deg2Rad;
            Vector3 pos = owner.transform.position + new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * CurrentRadius;
            p.transform.position = pos;

            Vector2 tangential = new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));
            float ang = Mathf.Atan2(tangential.y, tangential.x) * Mathf.Rad2Deg;
            p.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    public void OnUpgrade(int lvl)
    {
        level = Mathf.Clamp(lvl, 1, data.maxLevel);
        RebuildPetals();
    }

    private void RebuildPetals()
    {
        foreach (var p in petals) if (p != null) Destroy(p);
        petals.Clear();
        petalAngles.Clear();

        int count = Mathf.Max(1, PetalCount);

        for (int i = 0; i < count; i++)
        {
            GameObject p;
            if (data.petalPrefab != null)
                p = Instantiate(data.petalPrefab, owner != null ? owner.transform.position : Vector3.zero, Quaternion.identity);
            else
            {
                p = new GameObject("DigitalPetal_Auto");
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 50;
            }

            var petalComp = p.GetComponent<DigitalPetalBullet>();
            if (petalComp == null) petalComp = p.AddComponent<DigitalPetalBullet>();

            bool continuous = level >= 5;
            petalComp.Configure(owner, this, CurrentDamage, data.hitCooldownPerEnemy, continuous, data.petalCooldown);

            if (owner != null) p.transform.SetParent(owner.transform, true);

            petals.Add(p);
            float ang = (360f / count) * i;
            petalAngles.Add(ang);
        }
    }

    public float GetPetalDamage() => CurrentDamage;
}
