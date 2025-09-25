using System;
using UnityEngine;

public static class DamageHelper
{
    public static event Action<GameObject, Vector3> OnDamageApplied;

    private static float GetDamageMultiplier(GameObject source)
    {
        if (source != null)
        {
            var mods = source.GetComponent<HeroModifierSystem>();
            if (mods != null) return mods.GetModifier(StatType.Damage);
        }

        var globalMods = UnityEngine.Object.FindObjectOfType<HeroModifierSystem>();
        if (globalMods != null) return globalMods.GetModifier(StatType.Damage);

        return 1f;
    }

    public static void ApplyDamage(GameObject source, EnemyStatus es, float baseDamage, bool raw = false, DamagePopup.DamageType popupType = DamagePopup.DamageType.Normal)
    {
        if (es == null) return;
        if (es.IsDead) return;

        float mult = GetDamageMultiplier(source);
        float final = baseDamage * mult;

        if (raw)
            es.TakeRawDamage(final);
        else
            es.TakeDamage(final, popupType);

        OnDamageApplied?.Invoke(source, es.transform != null ? es.transform.position : Vector3.zero);
    }

    public static void ApplyDamage(GameObject source, EnemyStats stats, float baseDamage, bool raw = false, DamagePopup.DamageType popupType = DamagePopup.DamageType.Normal)
    {
        if (stats == null) return;
        if (stats.GetCurrentHealth() <= 0f) return;

        float mult = GetDamageMultiplier(source);
        float final = baseDamage * mult;

        if (raw)
            stats.TakeRawDamage(final);
        else
            stats.TakeDamage(final, popupType);

        OnDamageApplied?.Invoke(source, stats.transform != null ? stats.transform.position : Vector3.zero);
    }

    public static void ApplyAoe(GameObject source, Vector3 center, float radius, float baseDamage, bool raw = false, DamagePopup.DamageType popupType = DamagePopup.DamageType.Normal)
    {
        var cols = Physics2D.OverlapCircleAll(center, radius);
        if (cols == null || cols.Length == 0) return;
        foreach (var c in cols)
        {
            if (c == null) continue;
            var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
            if (es != null)
            {
                ApplyDamage(source, es, baseDamage, raw, popupType);
            }
            else
            {
                var stats = c.GetComponent<EnemyStats>() ?? c.GetComponentInParent<EnemyStats>();
                if (stats != null)
                    ApplyDamage(source, stats, baseDamage, raw, popupType);
            }
        }
    }
}
