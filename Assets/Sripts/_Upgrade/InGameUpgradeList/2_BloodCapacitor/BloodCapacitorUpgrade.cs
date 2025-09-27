using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BloodCapacitorUpgrade : MonoBehaviour, IUpgradeBehavior
{
    private BloodCapacitorData data;
    private GameObject owner;
    private HeroMovement movement;
    private HeroModifierSystem mods;
    private HeroExperience heroExp;
    private HeroCombat combat;

    private int currentLevel = 0;
    private Coroutine autoAttractCoroutine;
    private readonly List<Coroutine> activeDamageRemovals = new List<Coroutine>();

    public void Configure(BloodCapacitorData d)
    {
        data = d;
    }

    public void Initialize(GameObject ownerGO, UpgradeBase d)
    {
        owner = ownerGO;
        if (d is BloodCapacitorData ds)
            Configure(ds);
        else
            Debug.LogWarning($"BloodCapacitorUpgrade.Initialize: expected BloodCapacitorData, got {d?.GetType().Name}");

        movement = owner?.GetComponent<HeroMovement>() ?? GetComponent<HeroMovement>() ?? FindObjectOfType<HeroMovement>();
        mods = owner?.GetComponent<HeroModifierSystem>() ?? GetComponent<HeroModifierSystem>() ?? FindObjectOfType<HeroModifierSystem>();
        heroExp = owner?.GetComponent<HeroExperience>() ?? GetComponent<HeroExperience>() ?? FindObjectOfType<HeroExperience>();
        combat = owner?.GetComponent<HeroCombat>() ?? GetComponent<HeroCombat>() ?? FindObjectOfType<HeroCombat>();

        if (heroExp != null)
        {
            heroExp.OnExperienceCollected -= OnExperienceCollected;
            heroExp.OnExperienceCollected += OnExperienceCollected;
        }
        else
        {
            Debug.LogWarning("BloodCapacitorUpgrade: HeroExperience not found in scene.");
        }
    }

    public void OnUpgrade(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, data != null ? data.maxLevel : level);

        if (data != null && currentLevel >= 4 && mods != null)
        {
            mods.AddModifier(StatType.ExperienceRadius, data.experienceRadiusMultiplier);
            Debug.Log($"BloodCapacitorUpgrade: Applied ExperienceRadius multiplier x{data.experienceRadiusMultiplier}");
        }

        if (data != null && currentLevel >= 5)
        {
            if (autoAttractCoroutine == null)
                autoAttractCoroutine = StartCoroutine(AutoAttractLoop());
        }
        else
        {
            if (autoAttractCoroutine != null)
            {
                StopCoroutine(autoAttractCoroutine);
                autoAttractCoroutine = null;
            }
        }

        Debug.Log($"BloodCapacitorUpgrade: Upgraded to level {currentLevel}");
    }

    public void Activate() { }

    private void OnDestroy()
    {
        if (heroExp != null) heroExp.OnExperienceCollected -= OnExperienceCollected;
    }

    private void OnExperienceCollected(int amount)
    {
        if (data == null) return;

        float duration = data.baseDuration + (currentLevel >= 2 ? data.level2_extraDuration : 0f);
        
        float moveBonus = data.moveSpeedBonus;
        if (moveBonus != 0f && mods != null)
        {
            mods.AddModifier(StatType.MoveSpeed, moveBonus, duration);
            Debug.Log($"BloodCapacitorUpgrade: Applied move boost {moveBonus * 100f}% for {duration}s");
        }
        else if (movement != null)
        {
            movement.AddSpeedBoost(moveBonus, duration);
            Debug.Log($"BloodCapacitorUpgrade: Applied move boost {moveBonus * 100f}% for {duration}s via HeroMovement");
        }

        if (currentLevel >= 3)
        {
            float dmg = data.damageBonus;
            if (mods != null)
            {
                mods.AddModifier(StatType.Damage, dmg, duration);
                Debug.Log($"BloodCapacitorUpgrade: Applied damage boost {dmg * 100f}% for {duration}s");
            }
            else if (combat != null)
            {
                combat.AddDamageBoost(dmg, duration);
                Debug.Log($"BloodCapacitorUpgrade: Applied damage boost {dmg * 100f}% for {duration}s via HeroCombat");
            }
            else
            {
                Debug.LogWarning("BloodCapacitorUpgrade: cannot apply damage boost (no HeroModifierSystem or HeroCombat found).");
            }
        }
    }


    private IEnumerator RemoveDamageAfterDelay(float amount, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (mods != null)
        {
            mods.AddModifier(StatType.Damage, -amount);
            Debug.Log($"BloodCapacitorUpgrade: Removed damage boost {amount*100f}% after {delay}s");
        }
    }

    private IEnumerator AutoAttractLoop()
    {
        if (data == null) yield break;

        while (true)
        {
            yield return new WaitForSeconds(data.autoAttractInterval);

            var playerT = owner != null ? owner.transform : (FindObjectOfType<HeroExperience>()?.transform ?? null);
            if (playerT == null) continue;

            var pickups = FindObjectsOfType<ExperienceCollector>();
            if (pickups == null || pickups.Length == 0) continue;

            var list = new List<ExperienceCollector>();
            foreach (var p in pickups)
            {
                if (p == null) continue;
                float dist = Vector2.Distance(p.transform.position, playerT.position);
                if (dist <= data.autoAttractRange) list.Add(p);
            }

            if (list.Count == 0) continue;

            var pullCoroutines = new List<Coroutine>();
            foreach (var p in list)
            {
                pullCoroutines.Add(StartCoroutine(PullPickupToPlayer(p, playerT, data.autoAttractPullDuration)));
            }

            foreach (var c in pullCoroutines) if (c != null) yield return c;
        }
    }

    private IEnumerator PullPickupToPlayer(ExperienceCollector pickup, Transform playerT, float duration)
    {
        if (pickup == null || playerT == null) yield break;

        Transform t = pickup.transform;
        Vector3 start = t.position;
        float elapsed = 0f;

        Rigidbody2D rb = pickup.GetComponent<Rigidbody2D>();
        Collider2D col = pickup.GetComponent<Collider2D>();
        if (rb != null) { rb.isKinematic = true; rb.velocity = Vector2.zero; }
        if (col != null) col.enabled = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (t != null)
                t.position = Vector3.Lerp(start, playerT.position, p);
            yield return null;
        }

        if (pickup == null) yield break;

        var heroExp = owner != null ? owner.GetComponent<HeroExperience>() : FindObjectOfType<HeroExperience>();
        if (heroExp != null)
        {
            int amt = 0;
            try
            {
                amt = pickup.amount;
            }
            catch
            {
                var f = pickup.GetType().GetField("amount");
                if (f != null) amt = (int)f.GetValue(pickup);
                else
                {
                    var p = pickup.GetType().GetProperty("amount");
                    if (p != null) amt = (int)p.GetValue(pickup);
                }
            }

            if (amt > 0)
            {
                heroExp.AddExp(amt);
            }
            else
            {
                var collectMethod = pickup.GetType().GetMethod("Collect");
                if (collectMethod != null)
                {
                    try { collectMethod.Invoke(pickup, new object[] { owner }); }
                    catch { }
                }
            }
        }
        else
        {
            var collectMethod = pickup.GetType().GetMethod("Collect");
            if (collectMethod != null)
            {
                try { collectMethod.Invoke(pickup, new object[] { owner }); }
                catch { }
            }
        }

        if (pickup != null && pickup.gameObject != null)
            Destroy(pickup.gameObject);
    }
}
