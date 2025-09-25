using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Data")]
    public EnemyData data;

    [Header("Drop")]
    [SerializeField] private GameObject expPickupPrefab;

    [Header("Damage display")]
    [SerializeField] private bool showDamagePopups = true;
    [SerializeField] private float criticalChance = 0.10f;
    [SerializeField] private float criticalMultiplier = 2f;

    private float currentHealth;
    private float lastHitTime;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private Rigidbody2D rb;
    private Collider2D enemyCollider;

    private Transform player;
    private Collider2D playerCollider;
    private HeroHealth playerHealth;

    [HideInInspector] public float speedModifier = 1f;
    private float flipThreshold = 0.1f;

    public bool IsDead => currentHealth <= 0f;

    public void Init(EnemyData d)
    {
        data = d;
        currentHealth = data.health;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (GetComponent<EnemyStatus>() == null)
            gameObject.AddComponent<EnemyStatus>();
    }

    private void Start()
    {
        if (currentHealth <= 0f) currentHealth = Mathf.Max(1f, data != null ? data.health : 100f);

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) { Debug.LogError("Player not found!"); enabled = false; return; }
        player = playerObj.transform;
        playerCollider = playerObj.GetComponent<Collider2D>();
        playerHealth = playerObj.GetComponent<HeroHealth>();
        if (playerCollider == null || playerHealth == null) { Debug.LogError("Player missing components!"); enabled = false; }
    }

    private void FixedUpdate()
    {
        if (player == null || playerHealth == null || IsDead) return;

        Vector2 dir = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + dir * (data != null ? data.speed : 1f) * speedModifier * Time.fixedDeltaTime);

        if (spriteRenderer != null && Mathf.Abs(dir.x) > flipThreshold)
            spriteRenderer.flipX = dir.x < 0f;

        if (Time.time >= lastHitTime + (data != null ? data.damageCooldown : 1f) &&
            enemyCollider != null && playerCollider != null &&
            enemyCollider.bounds.Intersects(playerCollider.bounds))
        {
            playerHealth.TakeDamage(data != null ? data.damage : 1f);
            lastHitTime = Time.time;
        }
    }

    public void TakeDamage(float amount) => InternalTakeDamage(amount, DamagePopup.DamageType.Normal, allowCritical: true);
    public void TakeDamage(float amount, DamagePopup.DamageType damageType) => InternalTakeDamage(amount, damageType, allowCritical: true);
    public void TakeRawDamage(float amount) => InternalTakeDamage(amount, DamagePopup.DamageType.Normal, allowCritical: false);

    private void InternalTakeDamage(float amount, DamagePopup.DamageType damageType, bool allowCritical)
    {
        if (IsDead) return;

        float finalDamage = amount;
        DamagePopup.DamageType displayType = damageType;

        if (allowCritical && damageType == DamagePopup.DamageType.Normal && Random.value < criticalChance)
        {
            finalDamage *= criticalMultiplier;
            displayType = DamagePopup.DamageType.Critical;
        }

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, data != null ? data.health : currentHealth);

        if (showDamagePopups && DamagePopupManager.Instance != null)
        {
            Vector3 popupPosition = transform.position + Vector3.up * 0.6f;
            try
            {
                switch (displayType)
                {
                    case DamagePopup.DamageType.Critical: DamagePopupManager.Instance.ShowCriticalDamage(finalDamage, popupPosition); break;
                    case DamagePopup.DamageType.Poison: DamagePopupManager.Instance.ShowPoisonDamage(finalDamage, popupPosition); break;
                    case DamagePopup.DamageType.Burn: DamagePopupManager.Instance.ShowBurnDamage(finalDamage, popupPosition); break;
                    case DamagePopup.DamageType.Heal: DamagePopupManager.Instance.ShowHeal(finalDamage, popupPosition); break;
                    default: DamagePopupManager.Instance.ShowNormalDamage(finalDamage, popupPosition); break;
                }
            }
            catch { }
        }

        if (spriteRenderer != null)
        {
            if (flashCoroutine != null) { StopCoroutine(flashCoroutine); spriteRenderer.color = originalColor; }
            flashCoroutine = StartCoroutine(FlashDamage());
        }

        if (IsDead) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        float old = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, data != null ? data.health : currentHealth);
        float actualHeal = currentHealth - old;

        if (actualHeal > 0 && showDamagePopups && DamagePopupManager.Instance != null)
        {
            Vector3 popupPosition = transform.position + Vector3.up * 0.6f;
            try { DamagePopupManager.Instance.ShowHeal(actualHeal, popupPosition); }
            catch { }
        }
    }

    private IEnumerator FlashDamage()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    private void Die()
    {
        if (data != null && data.expReward > 0 && expPickupPrefab != null)
        {
            var drop = Instantiate(expPickupPrefab, transform.position, Quaternion.identity);
            var pickup = drop.GetComponent<ExperienceCollector>();
            if (pickup != null) pickup.amount = data.expReward;
        }

        Destroy(gameObject);
    }

    public float GetHealthPercentage() => (data != null && data.health > 0f) ? currentHealth / data.health : 0f;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => data != null ? data.health : 0f;
}
