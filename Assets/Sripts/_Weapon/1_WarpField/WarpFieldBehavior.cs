using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WarpFieldBehavior : MonoBehaviour, IWeaponBehavior
{
    public string enemyLayerName = "Enemy";

    private WarpFieldWeaponData d;
    private GameObject owner;
    private int level = 1;

    private float currentRadius => (d == null) ? 0f : ((level >= 3) ? d.baseRadius * 1.5f : d.baseRadius);
    private int enemyLayerMask;

    private HashSet<EnemyStatus> insideEnemies = new HashSet<EnemyStatus>();
    private Dictionary<EnemyStatus, Coroutine> slowCoroutines = new Dictionary<EnemyStatus, Coroutine>();

    private Coroutine damageTickCoroutine;
    private GameObject visualInstance;
    private float checkInterval = 0.15f;
    private Coroutine checkCoroutine;

    public void Initialize(GameObject ownerGO, WeaponBase data, HeroModifierSystem mods, HeroCombat combat)
    {
        owner = ownerGO;
        d = data as WarpFieldWeaponData;
        if (d == null) { enabled = false; return; }
        level = 1;
        enemyLayerMask = LayerMask.GetMask(enemyLayerName);

        if (d.areaPrefab != null && owner != null)
        {
            visualInstance = Instantiate(d.areaPrefab, owner.transform.position, Quaternion.identity);
            UpdateVisualScale();
        }

        if (checkCoroutine != null) StopCoroutine(checkCoroutine);
        checkCoroutine = StartCoroutine(PeriodicCheck());

        ApplyLevelState();
    }

    public void Activate() { }

    private void Update()
    {
        if (visualInstance != null && owner != null)
            visualInstance.transform.position = owner.transform.position;
    }

    public void OnUpgrade(int lvl)
    {
        level = Mathf.Clamp(lvl, 1, 5);
        ApplyLevelState();
    }

    private void ApplyLevelState()
    {
        UpdateVisualScale();

        if (damageTickCoroutine != null) { StopCoroutine(damageTickCoroutine); damageTickCoroutine = null; }

        if (level >= 1) damageTickCoroutine = StartCoroutine(DamageTickRoutine());

        if (level < 2)
        {
            foreach (var kv in slowCoroutines)
            {
                if (kv.Value != null) StopCoroutine(kv.Value);
            }
            slowCoroutines.Clear();
        }
    }

    private void UpdateVisualScale()
    {
        if (visualInstance == null || d == null) return;

        var sr = visualInstance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float spriteDiameter = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (spriteDiameter > 0.0001f)
            {
                float desiredDiameter = currentRadius * 2f;
                float scale = desiredDiameter / spriteDiameter;
                visualInstance.transform.localScale = new Vector3(scale, scale, 1f);
                return;
            }
        }

        visualInstance.transform.localScale = Vector3.one * currentRadius;
    }

    private IEnumerator PeriodicCheck()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            RefreshInsideEnemies();
            yield return wait;
        }
    }

    private void RefreshInsideEnemies()
    {
        if (owner == null || d == null) return;

        Vector2 center = owner.transform.position;
        float radius = currentRadius;

        Collider2D[] cols = enemyLayerMask != 0 ? Physics2D.OverlapCircleAll(center, radius, enemyLayerMask) : Physics2D.OverlapCircleAll(center, radius);

        HashSet<EnemyStatus> found = new HashSet<EnemyStatus>();

        if (cols != null && cols.Length > 0)
        {
            foreach (var c in cols)
            {
                if (c == null) continue;
                var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
                if (es != null) { found.Add(es); continue; }
                var stats = c.GetComponent<EnemyStats>() ?? c.GetComponentInParent<EnemyStats>();
                if (stats != null)
                {
                    var parentEs = stats.GetComponent<EnemyStatus>() ?? stats.GetComponentInParent<EnemyStatus>();
                    if (parentEs != null) found.Add(parentEs);
                }
            }
        }

        if (found.Count == 0)
        {
            var cols2 = Physics2D.OverlapCircleAll(center, radius);
            foreach (var c in cols2)
            {
                if (c == null) continue;
                if (c.gameObject.CompareTag("Enemy"))
                {
                    var es = c.GetComponent<EnemyStatus>() ?? c.GetComponentInParent<EnemyStatus>();
                    if (es != null) { found.Add(es); continue; }
                    var stats = c.GetComponent<EnemyStats>() ?? c.GetComponentInParent<EnemyStats>();
                    if (stats != null)
                    {
                        var parentEs = stats.GetComponent<EnemyStatus>() ?? stats.GetComponentInParent<EnemyStatus>();
                        if (parentEs != null) found.Add(parentEs);
                    }
                }
            }
        }

        foreach (var es in found)
        {
            if (es == null) continue;
            if (!insideEnemies.Contains(es))
            {
                insideEnemies.Add(es);
                OnEnemyEnter(es);
            }
        }

        var toRemove = new List<EnemyStatus>();
        foreach (var es in insideEnemies)
        {
            if (es == null || !found.Contains(es)) toRemove.Add(es);
        }

        foreach (var es in toRemove)
        {
            insideEnemies.Remove(es);
            if (es != null) OnEnemyExit(es);
        }
    }

    private void OnEnemyEnter(EnemyStatus es)
    {
        if (level >= 2)
        {
            if (!slowCoroutines.ContainsKey(es)) slowCoroutines[es] = StartCoroutine(SlowRefreshRoutine(es));
        }

        if (level >= 5 && d != null)
        {
            float multiplier = (level >= 4) ? 1.5f : 1f;
            float firstDmg = d.firstContactDamage * multiplier;
            DamageHelper.ApplyDamage(owner, es, firstDmg, raw: false, popupType: DamagePopup.DamageType.Normal, DamageHelper.DamageSourceType.AreaEffect);
        }
    }

    private void OnEnemyExit(EnemyStatus es)
    {
        if (es == null) return;
        if (slowCoroutines.TryGetValue(es, out var cr))
        {
            if (cr != null) StopCoroutine(cr);
            slowCoroutines.Remove(es);
        }
    }

    private IEnumerator SlowRefreshRoutine(EnemyStatus es)
    {
        var wait = new WaitForSeconds(0.45f);
        while (es != null && insideEnemies.Contains(es) && level >= 2)
        {
            try { es.ApplySlow(d.slowFactor, 0.6f); }
            catch
            {
                var stats = es.GetComponent<EnemyStats>();
                if (stats != null) stats.speedModifier = Mathf.Clamp01(1f - d.slowFactor);
            }
            yield return wait;
        }
        if (es != null && slowCoroutines.ContainsKey(es)) slowCoroutines.Remove(es);
    }

    private IEnumerator DamageTickRoutine()
    {
        var wait = new WaitForSeconds(d.tickInterval);
        while (true)
        {
            yield return wait;
            float multiplier = (level >= 4) ? 1.5f : 1f;
            float dmg = d.damagePerTick * multiplier;
            var snapshot = new EnemyStatus[insideEnemies.Count];
            insideEnemies.CopyTo(snapshot);
            foreach (var es in snapshot)
            {
                if (es == null) continue;
                DamageHelper.ApplyDamage(owner, es, dmg, raw: true, popupType: DamagePopup.DamageType.Normal, DamageHelper.DamageSourceType.AreaEffect);
            }
        }
    }

    public void ForceDestroy()
    {
        CleanUpAllCoroutines();
    }

    private void OnDisable() => CleanUpAllCoroutines();
    private void OnDestroy() => CleanUpAllCoroutines();

    private void CleanUpAllCoroutines()
    {
        if (checkCoroutine != null) { StopCoroutine(checkCoroutine); checkCoroutine = null; }
        if (damageTickCoroutine != null) { StopCoroutine(damageTickCoroutine); damageTickCoroutine = null; }
        foreach (var kv in slowCoroutines) if (kv.Value != null) StopCoroutine(kv.Value);
        slowCoroutines.Clear();
        insideEnemies.Clear();
        if (visualInstance != null) { Destroy(visualInstance); visualInstance = null; }
    }
}