using UnityEngine;

public class PlayerHurtbox : MonoBehaviour
{
    [SerializeField] private HeroHealth health;
    
    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<HeroHealth>();
        
        if (health == null)
        {
            Debug.LogError("PlayerHurtbox: HeroHealth component not found!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyStats>();
            if (enemy != null && health != null)
            {
                health.TakeDamage(enemy.data.damage);
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyStats>();
            if (enemy != null && health != null)
            {
                // Повторный урон будет обработан в EnemyStats с кулдауном
            }
        }
    }
}