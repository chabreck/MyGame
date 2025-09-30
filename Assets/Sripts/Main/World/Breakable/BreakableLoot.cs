using UnityEngine;
using System.Reflection;

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

        if (GetComponent<EnemyStatus>() == null)
            gameObject.AddComponent<EnemyStatus>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        float dmg = TryGetDamageFromCollider(collision.collider);
        if (dmg > 0f) ApplyHit(dmg);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        float dmg = TryGetDamageFromCollider(other);
        if (dmg > 0f) ApplyHit(dmg);
    }

    private float TryGetDamageFromCollider(Collider2D col)
    {
        if (col == null) return 0f;
        GameObject go = col.gameObject;

        float dmg = TryInspectObjectForDamage(go);
        if (dmg > 0f) return dmg;

        Transform p = go.transform.parent;
        while (p != null)
        {
            dmg = TryInspectObjectForDamage(p.gameObject);
            if (dmg > 0f) return dmg;
            p = p.parent;
        }

        if (go.CompareTag("Projectile")) return baseHitDamage;
        return 0f;
    }

    private float TryInspectObjectForDamage(GameObject go)
    {
        if (go == null) return 0f;

        var monoComps = go.GetComponents<MonoBehaviour>();
        foreach (var c in monoComps)
        {
            if (c == null) continue;
            var t = c.GetType();

            var f = t.GetField("damage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                var v = f.GetValue(c);
                if (v is float vf) return vf;
                if (v is int vi) return vi;
                if (v is double vd) return (float)vd;
            }

            var f2 = t.GetField("hitDamage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f2 != null)
            {
                var v2 = f2.GetValue(c);
                if (v2 is float vf2) return vf2;
                if (v2 is int vi2) return vi2;
            }

            var p = t.GetProperty("damage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null)
            {
                var v = p.GetValue(c);
                if (v is float vp) return vp;
                if (v is int vpi) return vpi;
            }

            var p2 = t.GetProperty("Damage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p2 != null)
            {
                var v = p2.GetValue(c);
                if (v is float vp2) return vp2;
                if (v is int vpi2) return vpi2;
            }

            var m = t.GetMethod("GetDamage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null && m.ReturnType == typeof(float))
            {
                try
                {
                    var r = m.Invoke(c, null);
                    if (r is float mr) return mr;
                }
                catch { }
            }
        }

        if (go.CompareTag("Projectile")) return baseHitDamage;
        return 0f;
    }

    private void ApplyHit(float amount)
    {
        if (amount <= 0f) amount = baseHitDamage;
        TakeDamage(amount);
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
        TrySpawnPickup(healthPickupPrefab, healthDropChance);
        TrySpawnPickup(magnetPickupPrefab, magnetDropChance);
        Destroy(gameObject);
    }

    private void TrySpawnPickup(GameObject prefab, float chance)
    {
        if (prefab == null) return;
        if (Random.value <= chance) Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
