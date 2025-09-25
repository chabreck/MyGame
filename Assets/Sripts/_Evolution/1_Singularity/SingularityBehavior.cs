using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SingularityBehavior : MonoBehaviour, IWeaponBehavior
{
    private SingularityEvolutionData data;
    private GameObject owner;
    private HeroModifierSystem mods;
    private HeroCombat combat;
    private SingularityBullet fieldInstance;
    private int level = 1;
    private Coroutine absorbCoroutine;

    public void Initialize(GameObject owner, WeaponBase wb, HeroModifierSystem mods, HeroCombat combat)
    {
        this.owner = owner;
        this.mods = mods;
        this.combat = combat;
        data = wb as SingularityEvolutionData;
        if (data == null) { enabled = false; return; }
        SpawnField();
        if (absorbCoroutine != null) StopCoroutine(absorbCoroutine);
        absorbCoroutine = StartCoroutine(AbsorbLoop());
    }

    public void Activate() { }

    public void OnUpgrade(int lvl)
    {
        level = Mathf.Clamp(lvl, 1, data != null ? data.maxLevel : lvl);
        if (fieldInstance != null)
        {
            fieldInstance.IncreaseBaseDamage(0f);
        }
    }

    private void SpawnField()
    {
        GameObject go;
        if (data.areaPrefab != null)
        {
            go = Instantiate(data.areaPrefab);
            go.transform.SetParent(owner.transform);
            go.transform.localPosition = Vector3.zero;
        }
        else
        {
            go = new GameObject("SingularityField");
            go.transform.SetParent(owner.transform);
            go.transform.localPosition = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.15f, 0.05f, 0.3f, 0.7f);
        }

        fieldInstance = go.GetComponent<SingularityBullet>();
        if (fieldInstance == null) fieldInstance = go.AddComponent<SingularityBullet>();

        float radius = data.fieldBaseRadius * (level >= 3 ? 1.5f : 1f);
        float baseDmg = data.fieldBaseDamage * (level >= 4 ? 1.5f : 1f);
        float firstDmg = data.firstContactBonusDamage;
        float slow = data.slowFactor;
        float tickInt = data.tickInterval;

        fieldInstance.Configure(owner, radius, baseDmg, firstDmg, slow, tickInt);
    }

    private IEnumerator AbsorbLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.05f, data.absorbCheckInterval));
        while (true)
        {
            if (fieldInstance != null)
            {
                var clouds = Object.FindObjectsOfType<BlackDustBullet>();
                if (clouds != null && clouds.Length > 0)
                {
                    foreach (var c in clouds)
                    {
                        if (c == null) continue;
                        float dist = Vector3.Distance(c.transform.position, fieldInstance.transform.position);
                        if (dist <= fieldInstance.Radius)
                        {
                            fieldInstance.IncreaseRadius(data.absorbRadiusPerCloud);
                            fieldInstance.IncreaseBaseDamage(data.absorbDamagePerCloud);
                            try { c.ForceDestroy(); } catch { Object.Destroy(c.gameObject); }
                        }
                    }
                }
            }
            yield return wait;
        }
    }

    private void OnDestroy()
    {
        if (absorbCoroutine != null) StopCoroutine(absorbCoroutine);
        if (fieldInstance != null) Destroy(fieldInstance.gameObject);
    }
}
