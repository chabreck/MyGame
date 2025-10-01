using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    public EnemyData data;
    [SerializeField] private GameObject expPickupPrefab;
    [SerializeField] private bool showDamagePopups = true;
    [SerializeField] private float criticalChance = 0.10f;
    [SerializeField] private float criticalMultiplier = 2f;
    
    private float currentHealth;
    private float lastHitTime;
    private float lastAttackTime;
    private Rigidbody2D rb;
    private Transform player;
    private HeroHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private float initialLocalScaleX = 1f;
    [HideInInspector] public float speedModifier = 1f;
    
    public bool IsDead => currentHealth <= 0f;
    
    public void Init(EnemyData d)
    {
        data = d;
        currentHealth = data.health;
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        initialLocalScaleX = transform.localScale.x;
        if (GetComponent<EnemyStatus>() == null)
            gameObject.AddComponent<EnemyStatus>();
    }
    
    private void Start()
    {
        if (currentHealth <= 0f) currentHealth = Mathf.Max(1f, data != null ? data.health : 100f);
        
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<HeroHealth>();
        }
    }
    
    private void FixedUpdate()
    {
        if (player == null || playerHealth == null || IsDead) return;
        
        Vector2 dir = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + dir * (data != null ? data.speed : 1f) * speedModifier * Time.fixedDeltaTime);
        UpdateFacing();
    }
    
    private void UpdateFacing()
    {
        if (player == null) return;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = dx < 0f;
        }
        else
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(initialLocalScaleX) * (dx < 0f ? -1f : 1f);
            transform.localScale = s;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerHurtbox"))
        {
            var health = other.GetComponentInParent<HeroHealth>();
            if (health != null && Time.time - lastAttackTime >= data.damageCooldown)
            {
                health.TakeDamage(data.damage);
                lastAttackTime = Time.time;
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("PlayerHurtbox"))
        {
            if (Time.time - lastAttackTime >= data.damageCooldown)
            {
                var health = other.GetComponentInParent<HeroHealth>();
                if (health != null)
                {
                    health.TakeDamage(data.damage);
                    lastAttackTime = Time.time;
                }
            }
        }
    }
    
    public void TakeDamage(float amount) => InternalTakeDamage(amount, DamagePopup.DamageType.Normal, true);
    public void TakeDamage(float amount, DamagePopup.DamageType damageType) => InternalTakeDamage(amount, damageType, true);
    public void TakeRawDamage(float amount) => InternalTakeDamage(amount, DamagePopup.DamageType.Normal, false);
    public void TakeRawDamage(float amount, DamagePopup.DamageType damageType) => InternalTakeDamage(amount, damageType, false);
    
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
        
        var flash = GetComponent<DamageFlash>();
        if (flash != null)
            flash.TriggerFlash();
        
        if (showDamagePopups && DamagePopupManager.Instance != null)
        {
            Vector3 popupPosition = transform.position + Vector3.up * 0.6f;
            switch (displayType)
            {
                case DamagePopup.DamageType.Critical: DamagePopupManager.Instance.ShowCriticalDamage(finalDamage, popupPosition); break;
                case DamagePopup.DamageType.Poison: DamagePopupManager.Instance.ShowPoisonDamage(finalDamage, popupPosition); break;
                case DamagePopup.DamageType.Burn: DamagePopupManager.Instance.ShowBurnDamage(finalDamage, popupPosition); break;
                case DamagePopup.DamageType.Heal: DamagePopupManager.Instance.ShowHeal(finalDamage, popupPosition); break;
                default: DamagePopupManager.Instance.ShowNormalDamage(finalDamage, popupPosition); break;
            }
        }
        
        if (IsDead) Die();
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
}