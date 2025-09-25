using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IDamageable), typeof(EnemyStats))]
public class EnemyStatus : MonoBehaviour
{
    private IDamageable dmgable;
    private EnemyStats stats;
    private Dictionary<EffectType, Coroutine> activeEffects = new Dictionary<EffectType, Coroutine>();

    public enum EffectType { Slow, Poison, Burn, Freeze, Stun }

    private void Awake()
    {
        dmgable = GetComponent<IDamageable>();
        stats = GetComponent<EnemyStats>();
        if (dmgable == null) Debug.LogError($"{name}: IDamageable missing!");
        if (stats == null) Debug.LogError($"{name}: EnemyStats missing!");
    }

    /// <summary> Проверяет, мёртв ли враг. </summary>
    public bool IsDead => dmgable.IsDead;

    /// <summary> Наносит урон через статус (стандартный, допускает крит).</summary>
    public void TakeDamage(float amount) => dmgable.TakeDamage(amount);

    /// <summary> Наносит урон с указанным типом для правильного отображения popup'а (допускает крит).</summary>
    public void TakeDamage(float amount, DamagePopup.DamageType damageType)
    {
        if (stats != null)
            stats.TakeDamage(amount, damageType);
        else
            dmgable.TakeDamage(amount);
    }

    /// <summary> Наносит урон без критов — для DoT/статусов/полей/пульсов.</summary>
    public void TakeRawDamage(float amount)
    {
        if (stats != null)
            stats.TakeRawDamage(amount);
        else
            dmgable.TakeDamage(amount); // fallback: неидеально, но стараемся применить урон
    }

    /// <summary> Замедляет врага на factor (0-1) на duration секунд. </summary>
    public void ApplySlow(float factor, float duration = 1f)
    {
        StartOrRestart(EffectType.Slow, SlowRoutine(factor, duration));
    }

    /// <summary> Яд: урон tickDamage каждые interval на duration секунд. </summary>
    public void ApplyPoison(float tickDamage, float interval, float duration)
    {
        StartOrRestart(EffectType.Poison, DamageOverTimeRoutine(tickDamage, interval, duration, DamagePopup.DamageType.Poison));
    }

    /// <summary> Горение: урон tickDamage каждые interval на duration секунд. </summary>
    public void ApplyBurn(float tickDamage, float interval, float duration)
    {
        StartOrRestart(EffectType.Burn, DamageOverTimeRoutine(tickDamage, interval, duration, DamagePopup.DamageType.Burn));
    }

    /// <summary> Заморозка: полная остановка на duration секунд. </summary>
    public void ApplyFreeze(float duration)
    {
        StartOrRestart(EffectType.Freeze, FreezeRoutine(duration));
    }

    /// <summary> Оглушение: остановка и увеличение входящего урона на duration секунд. </summary>
    public void ApplyStun(float duration, float incomingMultiplier = 2f)
    {
        StartOrRestart(EffectType.Stun, StunRoutine(duration, incomingMultiplier));
    }

    /// <summary> Отбрасывает врага в указанном направлении. </summary>
    public void ApplyRetreat(Vector2 direction, float force = 5f)
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        }
    }

    /// <summary> Проверяет, активен ли указанный эффект. </summary>
    public bool HasEffect(EffectType effectType) => activeEffects.ContainsKey(effectType);

    /// <summary> Снимает указанный эффект. </summary>
    public void RemoveEffect(EffectType effectType)
    {
        if (activeEffects.ContainsKey(effectType))
        {
            StopCoroutine(activeEffects[effectType]);
            activeEffects.Remove(effectType);

            if (effectType == EffectType.Slow || effectType == EffectType.Freeze || effectType == EffectType.Stun)
                stats.speedModifier = 1f;
        }
    }

    /// <summary> Снимает все активные эффекты. </summary>
    public void ClearAllEffects()
    {
        foreach (var effect in activeEffects.Values)
            StopCoroutine(effect);
        activeEffects.Clear();
        stats.speedModifier = 1f;
    }

    private void StartOrRestart(EffectType type, IEnumerator routine)
    {
        if (activeEffects.ContainsKey(type))
        {
            StopCoroutine(activeEffects[type]);
            activeEffects.Remove(type);
        }
        var cr = StartCoroutine(routine);
        activeEffects[type] = cr;
    }

    private IEnumerator SlowRoutine(float factor, float duration)
    {
        stats.speedModifier = Mathf.Clamp01(1f - factor);
        yield return new WaitForSeconds(duration);
        stats.speedModifier = 1f;
        activeEffects.Remove(EffectType.Slow);
    }

    private IEnumerator DamageOverTimeRoutine(float damage, float interval, float duration, DamagePopup.DamageType damageType)
    {
        float elapsed = 0f;
        while (elapsed < duration && !IsDead)
        {
            // Для DoT/Poison/Burn используем TakeRawDamage чтобы исключить шанс крита
            TakeRawDamage(damage);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        if (damageType == DamagePopup.DamageType.Poison)
            activeEffects.Remove(EffectType.Poison);
        else if (damageType == DamagePopup.DamageType.Burn)
            activeEffects.Remove(EffectType.Burn);
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        stats.speedModifier = 0f;
        yield return new WaitForSeconds(duration);
        stats.speedModifier = 1f;
        activeEffects.Remove(EffectType.Freeze);
    }

    private IEnumerator StunRoutine(float duration, float incomingMultiplier)
    {
        stats.speedModifier = 0f;
        if (dmgable is HeroHealth hh)
        {
            hh.incomingDamageMultiplier *= incomingMultiplier;
        }
        yield return new WaitForSeconds(duration);
        stats.speedModifier = 1f;
        if (dmgable is HeroHealth hh2)
        {
            hh2.incomingDamageMultiplier /= incomingMultiplier;
        }
        activeEffects.Remove(EffectType.Stun);
    }
}
