using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableLoot : MonoBehaviour
{
    public float maxHp = 50f;
    public float baseHitDamage = 25f;
    public GameObject healthPickupPrefab;
    public GameObject magnetPickupPrefab;
    [Range(0f,1f)] public float healthDropChance = 0.3f;
    [Range(0f,1f)] public float magnetDropChance = 0.15f;
    
    private float hp;
    private BoxCollider2D box;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        hp = maxHp;
        box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = false;
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        if (GetComponent<DamageFlash>() == null)
            gameObject.AddComponent<DamageFlash>();
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        ProcessCollision(collision.collider);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        ProcessCollision(other);
    }
    
    private void ProcessCollision(Collider2D collider)
    {
        if (collider.CompareTag("Projectile") || 
            collider.gameObject.layer == LayerMask.NameToLayer("Projectile"))
        {
            DamageHelper.ApplyDamage(null, this, baseHitDamage, false, 
                DamagePopup.DamageType.Normal, DamageHelper.DamageSourceType.Weapon);
        }
    }
    
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        hp -= amount;
        var flash = GetComponent<DamageFlash>();
        if (flash != null) flash.TriggerFlash();
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.ShowNormalDamage(amount, transform.position + Vector3.up * 0.5f);
        if (hp <= 0f) Die();
    }
    
    private void Die()
    {
        float roll = Random.value;
        if (roll <= healthDropChance && healthPickupPrefab != null)
        {
            Instantiate(healthPickupPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            if (Random.value <= magnetDropChance && magnetPickupPrefab != null)
                Instantiate(magnetPickupPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}