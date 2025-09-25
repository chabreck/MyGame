using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DigitalPetalBullet : MonoBehaviour
{
    private GameObject owner;
    private DigitalPetalBehavior controller;
    private float damagePerHit = 10f;
    private float perEnemyCooldown = 0.45f;
    private bool continuous = false;
    private float selfCooldown = 1f;

    private Dictionary<EnemyStatus, float> lastHitTime = new Dictionary<EnemyStatus, float>();
    private SpriteRenderer sr;
    private Collider2D col;
    private bool isOnCooldown = false;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Configure(GameObject owner, DigitalPetalBehavior controller, float damage, float hitCooldownPerEnemy, bool continuous, float cooldown)
    {
        this.owner = owner;
        this.controller = controller;
        this.damagePerHit = damage;
        this.perEnemyCooldown = Mathf.Max(0.02f, hitCooldownPerEnemy);
        this.continuous = continuous;
        this.selfCooldown = Mathf.Max(0.05f, cooldown);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (other == null) return;
        if (owner != null && other.gameObject == owner) return;
        if (isOnCooldown && !continuous) return;

        var es = other.GetComponent<EnemyStatus>() ?? other.GetComponentInParent<EnemyStatus>();
        var stats = other.GetComponent<EnemyStats>() ?? other.GetComponentInParent<EnemyStats>();

        if (es == null && stats == null) return;

        float now = Time.time;
        EnemyStatus key = es;
        if (key == null && stats != null)
        {
            key = stats.GetComponent<EnemyStatus>();
        }

        if (key != null)
        {
            if (lastHitTime.TryGetValue(key, out var t) && now - t < perEnemyCooldown) return;
            lastHitTime[key] = now;
            DamageHelper.ApplyDamage(owner, key, damagePerHit, raw: false, popupType: DamagePopup.DamageType.Normal);
        }
        else
        {
            DamageHelper.ApplyDamage(owner, stats, damagePerHit, raw: false, popupType: DamagePopup.DamageType.Normal);
        }

        if (!continuous)
        {
            StartCoroutine(DisableAndCooldown());
        }
    }

    private IEnumerator DisableAndCooldown()
    {
        isOnCooldown = true;
        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;
        yield return new WaitForSeconds(selfCooldown);
        if (col != null) col.enabled = true;
        if (sr != null) sr.enabled = true;
        isOnCooldown = false;
    }

    private void OnDestroy()
    {
        lastHitTime.Clear();
    }
}
