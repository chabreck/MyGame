using System;
using UnityEngine;

public class HeroHealth : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Level scaling")]
    [Tooltip("Additive HP increase per level (e.g. 0.10 = +10% per level)")]
    [SerializeField] private float hpIncreasePerLevel = 0.10f;

    [Tooltip("Heal fraction of max HP when leveling up (e.g. 0.05 = +5% of new max)")]
    [SerializeField] private float healPercentOnLevel = 0.10f;

    private float currentHealth;
    private float lastDamageTime;
    private float originalMax;

    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    [HideInInspector] public float incomingDamageMultiplier = 1f;

    public float damageReduction;
    public float dodgeChance;
    public float damageReflection;
    public float explosiveReflectionChance;

    public event Action<float> OnDamageTaken;
    public event Action<float, Vector3> OnDamageReflected;
    public event Action OnDeath;
    public event Action OnLowHealth;
    public event Action OnCriticalHealth;

    private HeroExperience heroExp;

    private void Awake()
    {
        originalMax = maxHealth;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Find HeroExperience to subscribe to level-up events and to initialize HP scaling to current level
        heroExp = FindObjectOfType<HeroExperience>();
        int lvl = heroExp != null ? heroExp.CurrentLevel : 1;

        ApplyLevelScaling(lvl, doHeal: false);

        currentHealth = Mathf.Min(currentHealth, maxHealth); // clamp if needed
        GameStatsUIController.Instance?.UpdateHealthUI((int)currentHealth, (int)maxHealth);

        if (heroExp != null)
            heroExp.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (heroExp != null)
            heroExp.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        // Increase max HP according to new level and heal on levelup
        ApplyLevelScaling(newLevel, doHeal: true);
    }

    /// <summary>
    /// Recalculates maxHealth based on base (originalMax) and level.
    /// If doHeal == true, heals by fraction healPercentOnLevel of the NEW maxHealth.
    /// </summary>
    public void ApplyLevelScaling(int level, bool doHeal)
    {
        level = Mathf.Max(1, level);
        float newMax = originalMax * (1f + hpIncreasePerLevel * (level - 1));
        maxHealth = newMax;

        if (doHeal)
        {
            float healAmount = newMax * Mathf.Clamp01(healPercentOnLevel);
            Heal(healAmount);
        }
        else
        {
            // Ensure current health is not above new max
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            GameStatsUIController.Instance?.UpdateHealthUI((int)currentHealth, (int)maxHealth);
        }
    }

    public void TakeDamage(float amount)
    {
        if (Time.time - lastDamageTime < invincibilityDuration || IsDead) return;
        if (UnityEngine.Random.value < dodgeChance) return;

        amount *= incomingDamageMultiplier;

        if (damageReflection > 0 && UnityEngine.Random.value < damageReflection)
        {
            float reflected = amount * damageReflection;
            OnDamageReflected?.Invoke(reflected, transform.position);

            if (explosiveReflectionChance > 0 && UnityEngine.Random.value < explosiveReflectionChance)
            {
                var cols = Physics2D.OverlapCircleAll(transform.position, 5f, LayerMask.GetMask("Enemy"));
                foreach (var c in cols)
                    c.GetComponent<IDamageable>()?.TakeDamage(reflected);
            }
        }

        float final = amount * (1f - damageReduction);
        currentHealth -= final;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        lastDamageTime = Time.time;

        OnDamageTaken?.Invoke(final);
        GameStatsUIController.Instance?.UpdateHealthUI((int)currentHealth, (int)maxHealth);

        if (currentHealth <= maxHealth * 0.3f) OnCriticalHealth?.Invoke();
        else if (currentHealth <= maxHealth * 0.5f) OnLowHealth?.Invoke();

        if (currentHealth <= 0f)
        {
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        GameStatsUIController.Instance?.UpdateHealthUI((int)currentHealth, (int)maxHealth);
    }

    public void AddShield(float duration)
    {
        StartCoroutine(ShieldRoutine(duration));
    }

    private System.Collections.IEnumerator ShieldRoutine(float duration)
    {
        float saved = damageReduction;
        damageReduction += 0.5f;
        yield return new WaitForSeconds(duration);
        damageReduction = saved;
    }
}
