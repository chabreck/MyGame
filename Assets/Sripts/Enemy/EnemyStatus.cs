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

    public bool IsDead => dmgable.IsDead;

    public void TakeDamage(float amount) => dmgable.TakeDamage(amount);

    public void TakeDamage(float amount, DamagePopup.DamageType damageType)
    {
        if (stats != null)
            stats.TakeDamage(amount, damageType);
        else
            dmgable.TakeDamage(amount);
    }

    public void TakeRawDamage(float amount)
    {
        if (stats != null)
            stats.TakeRawDamage(amount);
        else
            dmgable.TakeDamage(amount);
    }

    public void ApplySlow(float factor, float duration = 1f)
    {
        StartOrRestart(EffectType.Slow, SlowRoutine(factor, duration));
    }

    public void ApplyPoison(float tickDamage, float interval, float duration)
    {
        StartOrRestart(EffectType.Poison, PoisonRoutine(tickDamage, interval, duration));
    }

    public void ApplyBurn(float tickDamage, float interval, float duration)
    {
        StartOrRestart(EffectType.Burn, DamageOverTimeRoutine(tickDamage, interval, duration, DamagePopup.DamageType.Burn));
    }

    public void ApplyFreeze(float duration)
    {
        StartOrRestart(EffectType.Freeze, FreezeRoutine(duration));
    }

    public void ApplyStun(float duration, float incomingMultiplier = 2f)
    {
        StartOrRestart(EffectType.Stun, StunRoutine(duration, incomingMultiplier));
    }

    public void ApplyRetreat(Vector2 direction, float force = 5f)
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        }
    }

    public bool HasEffect(EffectType effectType) => activeEffects.ContainsKey(effectType);

    public void RemoveEffect(EffectType effectType)
    {
        if (activeEffects.TryGetValue(effectType, out Coroutine routine))
        {
            if (routine != null)
                StopCoroutine(routine);
            activeEffects.Remove(effectType);

            if (effectType == EffectType.Slow || effectType == EffectType.Freeze || effectType == EffectType.Stun)
                stats.speedModifier = 1f;
        }
    }

    public void ClearAllEffects()
    {
        foreach (var kvp in activeEffects)
        {
            if (kvp.Value != null)
                StopCoroutine(kvp.Value);
        }
        activeEffects.Clear();
        stats.speedModifier = 1f;
    }

    private void StartOrRestart(EffectType type, IEnumerator routine)
    {
        if (activeEffects.TryGetValue(type, out Coroutine currentRoutine))
        {
            if (currentRoutine != null)
                StopCoroutine(currentRoutine);
            activeEffects.Remove(type);
        }
        
        Coroutine newRoutine = StartCoroutine(routine);
        activeEffects[type] = newRoutine;
    }

    private IEnumerator SlowRoutine(float factor, float duration)
    {
        stats.speedModifier = Mathf.Clamp01(1f - factor);
        yield return new WaitForSeconds(duration);
        stats.speedModifier = 1f;
        
        if (activeEffects.TryGetValue(EffectType.Slow, out Coroutine routine) && routine != null)
        {
            activeEffects.Remove(EffectType.Slow);
        }
    }

    private IEnumerator PoisonRoutine(float damage, float interval, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !IsDead)
        {
            // Используем TakeRawDamage для яда, но с зеленым цветом
            TakeRawDamage(damage);
            
            // Уведомляем NervousToxin о тике яда
            if (NervousToxinBehavior.Instance != null)
            {
                NervousToxinBehavior.Instance.OnEnemyPoisonTick(gameObject, damage);
            }
            
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        
        if (activeEffects.TryGetValue(EffectType.Poison, out Coroutine routine) && routine != null)
        {
            activeEffects.Remove(EffectType.Poison);
        }
    }

    private IEnumerator DamageOverTimeRoutine(float damage, float interval, float duration, DamagePopup.DamageType damageType)
    {
        float elapsed = 0f;
        while (elapsed < duration && !IsDead)
        {
            TakeDamage(damage, damageType);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        if (damageType == DamagePopup.DamageType.Burn && activeEffects.TryGetValue(EffectType.Burn, out Coroutine routine) && routine != null)
        {
            activeEffects.Remove(EffectType.Burn);
        }
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        stats.speedModifier = 0f;
        yield return new WaitForSeconds(duration);
        stats.speedModifier = 1f;
        
        if (activeEffects.TryGetValue(EffectType.Freeze, out Coroutine routine) && routine != null)
        {
            activeEffects.Remove(EffectType.Freeze);
        }
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
        
        if (activeEffects.TryGetValue(EffectType.Stun, out Coroutine routine) && routine != null)
        {
            activeEffects.Remove(EffectType.Stun);
        }
    }
}