using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class DistoredClothBullet : MonoBehaviour
{
    private GameObject owner;
    private Vector2 dir;
    private float speed;
    private float damage;
    private float lifeTime;

    private int penetrationCount = 1;
    private bool poisonEnabled = false;
    private float poisonTick = 0f;
    private float poisonInterval = 1f;
    private float poisonDuration = 3f;

    private Rigidbody2D rb;
    private HashSet<EnemyStatus> hitEnemies = new HashSet<EnemyStatus>();

    public void Initialize(GameObject owner, Vector2 direction, float speed, float damage, float lifeTime, int penetrationCount, bool poisonEnabled, float poisonTick, float poisonInterval, float poisonDuration, string projectileLayerName)
    {
        this.owner = owner;
        this.dir = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifeTime = lifeTime;
        this.penetrationCount = Mathf.Max(1, penetrationCount);
        this.poisonEnabled = poisonEnabled;
        this.poisonTick = poisonTick;
        this.poisonInterval = poisonInterval;
        this.poisonDuration = poisonDuration;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.velocity = this.dir * this.speed;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (!string.IsNullOrEmpty(projectileLayerName))
        {
            try { gameObject.layer = LayerMask.NameToLayer(projectileLayerName); } catch { }
        }

        float angle = Mathf.Atan2(this.dir.y, this.dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (owner != null && other.gameObject == owner) return;

        var es = other.GetComponent<EnemyStatus>() ?? other.GetComponentInParent<EnemyStatus>();
        var stats = other.GetComponent<EnemyStats>() ?? other.GetComponentInParent<EnemyStats>();

        if (es == null && stats == null) return;

        if (es != null)
        {
            if (hitEnemies.Contains(es)) return;
            hitEnemies.Add(es);

            DamageHelper.ApplyDamage(owner, es, damage, raw: false, popupType: DamagePopup.DamageType.Normal);

            if (poisonEnabled) es.ApplyPoison(poisonTick, poisonInterval, poisonDuration);

            penetrationCount--;
            if (penetrationCount <= 0) Destroy(gameObject);
        }
        else if (stats != null)
        {
            var maybeEs = stats.GetComponent<EnemyStatus>();
            if (maybeEs != null)
            {
                if (hitEnemies.Contains(maybeEs)) return;
                hitEnemies.Add(maybeEs);
                DamageHelper.ApplyDamage(owner, maybeEs, damage, raw: false, popupType: DamagePopup.DamageType.Normal);
                if (poisonEnabled) maybeEs.ApplyPoison(poisonTick, poisonInterval, poisonDuration);
            }
            else
            {
                DamageHelper.ApplyDamage(owner, stats, damage, raw: false, popupType: DamagePopup.DamageType.Normal);
                if (poisonEnabled)
                {
                    var ps = stats.GetComponent<EnemyStatus>();
                    ps?.ApplyPoison(poisonTick, poisonInterval, poisonDuration);
                }
            }

            penetrationCount--;
            if (penetrationCount <= 0) Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        hitEnemies.Clear();
    }
}
