using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class SingularityBullet : MonoBehaviour
{
    private GameObject owner;
    private CircleCollider2D trigger;
    private HashSet<EnemyStatus> inside = new HashSet<EnemyStatus>();
    private float baseDamage;
    private float firstContactDamage;
    private float slowFactor;
    private float radius;
    private float tickInterval = 1f;
    private float damagePerTick = 0f;
    private Coroutine tickCoroutine;

    public float Radius => radius;

    public void Configure(GameObject owner, float radius, float baseDamage, float firstContactDamage, float slowFactor, float tickInterval = 1f)
    {
        this.owner = owner;
        this.radius = Mathf.Max(0.05f, radius);
        this.baseDamage = Mathf.Max(0f, baseDamage);
        this.firstContactDamage = Mathf.Max(0f, firstContactDamage);
        this.slowFactor = Mathf.Clamp01(slowFactor);
        this.tickInterval = Mathf.Max(0.05f, tickInterval);
        this.damagePerTick = this.baseDamage;

        trigger = GetComponent<CircleCollider2D>();
        if (trigger == null) trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float spriteDiameter = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (spriteDiameter > 0.0001f)
            {
                float desiredDiameter = this.radius * 2f;
                float scaleFactor = desiredDiameter / spriteDiameter;
                transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
                trigger.radius = spriteDiameter * 0.5f;
            }
            else
            {
                trigger.radius = this.radius;
            }
        }
        else
        {
            trigger.radius = this.radius;
        }

        transform.position = owner != null ? owner.transform.position : transform.position;

        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        tickCoroutine = StartCoroutine(DamageTickRoutine());
    }

    private void Update()
    {
        if (owner != null) transform.position = owner.transform.position;
    }

    private System.Collections.IEnumerator DamageTickRoutine()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            yield return wait;
            var snapshot = new EnemyStatus[inside.Count];
            inside.CopyTo(snapshot);
            foreach (var es in snapshot)
            {
                if (es == null) continue;
                DamageHelper.ApplyDamage(owner, es, damagePerTick, raw: true, popupType: DamagePopup.DamageType.Normal);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        var es = other.GetComponent<EnemyStatus>() ?? other.GetComponentInParent<EnemyStatus>();
        if (es == null) return;
        if (!inside.Contains(es))
        {
            inside.Add(es);
            es.ApplySlow(slowFactor, Mathf.Max(0.5f, 2f));
            DamageHelper.ApplyDamage(owner, es, firstContactDamage, raw: false, popupType: DamagePopup.DamageType.Normal);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        var es = other.GetComponent<EnemyStatus>() ?? other.GetComponentInParent<EnemyStatus>();
        if (es != null) inside.Remove(es);
    }

    public void IncreaseRadius(float amount)
    {
        radius += amount;
        if (trigger == null) trigger = GetComponent<CircleCollider2D>();
        if (trigger != null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteDiameter = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                if (spriteDiameter > 0.0001f)
                {
                    float desiredDiameter = radius * 2f;
                    float scaleFactor = desiredDiameter / spriteDiameter;
                    transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
                    trigger.radius = spriteDiameter * 0.5f;
                }
                else
                {
                    trigger.radius = radius;
                }
            }
            else
            {
                trigger.radius = radius;
            }
        }
    }

    public void IncreaseBaseDamage(float amount)
    {
        baseDamage += amount;
        damagePerTick = baseDamage;
    }

    public void SetPosition(Vector3 pos) => transform.position = pos;
    public void ForceDestroy() => Destroy(gameObject);

    private void OnDestroy()
    {
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        inside.Clear();
    }
}
