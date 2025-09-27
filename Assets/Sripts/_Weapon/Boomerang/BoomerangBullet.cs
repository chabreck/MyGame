using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BoomerangBullet : MonoBehaviour
{
    private GameObject owner;
    private Vector2 forwardDir;
    private float speed = 10f;
    private float maxDistance = 4f;
    private float damage = 10f;
    private bool stunOnReturn = false;
    private float stunDuration = 1f;

    private Rigidbody2D rb;
    private Vector3 startPos;
    private bool isReturning = false;
    private HashSet<EnemyStatus> hitThisPass = new HashSet<EnemyStatus>();
    private float destroyTimeout = 10f;
    private float createdAt;

    public void Initialize(GameObject owner, Vector2 direction, float speed, float maxDistance, float damage, bool stunOnReturn, float stunDuration)
    {
        this.owner = owner;
        this.forwardDir = direction.normalized;
        this.speed = speed;
        this.maxDistance = Mathf.Max(0.1f, maxDistance);
        this.damage = damage;
        this.stunOnReturn = stunOnReturn;
        this.stunDuration = stunDuration;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.velocity = this.forwardDir * this.speed;

        startPos = transform.position;
        createdAt = Time.time;

        float angle = Mathf.Atan2(forwardDir.y, forwardDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Destroy(gameObject, destroyTimeout);
    }

    private void Update()
    {
        if (owner == null)
        {
            if (!isReturning) StartReturn();
        }

        if (!isReturning)
        {
            float dist = Vector3.Distance(startPos, transform.position);
            if (dist >= maxDistance) StartReturn();
        }
        else
        {
            Vector3 target = owner != null ? owner.transform.position : startPos;
            Vector2 dir = (target - transform.position);
            if (dir.sqrMagnitude < 0.04f) { Destroy(gameObject); return; }
            rb.velocity = dir.normalized * speed;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void StartReturn()
    {
        isReturning = true;
        hitThisPass.Clear();
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
            if (!hitThisPass.Contains(es))
            {
                hitThisPass.Add(es);
                DamageHelper.ApplyDamage(owner, es, damage, raw: false, popupType: DamagePopup.DamageType.Normal, DamageHelper.DamageSourceType.Weapon);
                if (isReturning && stunOnReturn) es.ApplyStun(stunDuration);
            }
        }
        else if (stats != null)
        {
            var maybeEs = stats.GetComponent<EnemyStatus>();
            if (maybeEs != null)
            {
                if (!hitThisPass.Contains(maybeEs))
                {
                    hitThisPass.Add(maybeEs);
                    DamageHelper.ApplyDamage(owner, maybeEs, damage, raw: false, popupType: DamagePopup.DamageType.Normal, DamageHelper.DamageSourceType.Weapon);
                    if (isReturning && stunOnReturn) maybeEs.ApplyStun(stunDuration);
                }
            }
            else
            {
                DamageHelper.ApplyDamage(owner, stats, damage, raw: false, popupType: DamagePopup.DamageType.Normal, DamageHelper.DamageSourceType.Weapon);
                if (isReturning && stunOnReturn)
                {
                    var ps = stats.GetComponent<EnemyStatus>();
                    ps?.ApplyStun(stunDuration);
                }
            }
        }
    }
}
