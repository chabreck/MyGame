using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BlackDustBullet : MonoBehaviour
{
    private GameObject owner;
    private float lifetime = 3f;
    private float radius = 2f;
    private float dps = 4f;
    private bool slowEnabled = false;
    private float slowFactor = 0.4f;
    private bool notifyController = false;

    private CircleCollider2D trigger;
    private HashSet<EnemyStatus> inside = new HashSet<EnemyStatus>();
    private Coroutine tickRoutine;

    public void Initialize(GameObject owner, float duration, float radius, float dps, bool slowEnabled, float slowFactor, bool notifyController)
    {
        this.owner = owner;
        this.lifetime = Mathf.Max(0.1f, duration);
        this.radius = Mathf.Max(0.05f, radius);
        this.dps = Mathf.Max(0f, dps);
        this.slowEnabled = slowEnabled;
        this.slowFactor = Mathf.Clamp01(slowFactor);
        this.notifyController = notifyController;

        trigger = GetComponent<CircleCollider2D>();
        if (trigger == null) trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = this.radius;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float dia = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (dia > 0.0001f)
            {
                float scale = (this.radius * 2f) / dia;
                transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        tickRoutine = StartCoroutine(Tick());
        Destroy(gameObject, lifetime);
    }

    private IEnumerator Tick()
    {
        var wait = new WaitForSeconds(1f);
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            if (inside.Count > 0 && dps > 0f)
            {
                float per = dps; // per second
                // make a snapshot to avoid collection-modified exceptions
                var snapshot = new EnemyStatus[inside.Count];
                inside.CopyTo(snapshot);
                foreach (var es in snapshot)
                {
                    if (es == null) continue;
                    DamageHelper.ApplyDamage(owner, es, per, raw: true, popupType: DamagePopup.DamageType.Poison);
                }
            }
            elapsed += 1f;
            yield return wait;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        var es = other.GetComponent<EnemyStatus>() ?? other.GetComponentInParent<EnemyStatus>();
        if (es != null)
        {
            inside.Add(es);
            if (slowEnabled) es.ApplySlow(slowFactor, Mathf.Max(0.5f, lifetime));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        var es = other.GetComponent<EnemyStatus>() ?? other.GetComponentInParent<EnemyStatus>();
        if (es != null) inside.Remove(es);
    }

    public void SetPosition(Vector3 pos) => transform.position = pos;
    public void ForceDestroy() => Destroy(gameObject);
    private void OnDestroy() { if (tickRoutine != null) StopCoroutine(tickRoutine); inside.Clear(); }
}
