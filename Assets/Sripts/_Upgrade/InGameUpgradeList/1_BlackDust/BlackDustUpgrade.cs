using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BlackDustUpgrade : MonoBehaviour, IUpgradeBehavior
{
    private BlackDustData data;
    private HeroCombat combat;
    private int currentLevel = 0;
    private readonly List<BlackDustBullet> registeredClouds = new List<BlackDustBullet>();
    private GameObject owner;

    public void Configure(BlackDustData d) { data = d; Debug.Log($"BlackDustUpgrade: Configured with data {(d != null ? d.name : "null")}, cloudChance: {(d != null ? d.cloudChance.ToString() : "null")}"); }

    public void Initialize(GameObject ownerGO, UpgradeBase d)
    {
        owner = ownerGO;
        if (d is BlackDustData bd) Configure(bd);
        combat = owner?.GetComponent<HeroCombat>() ?? FindObjectOfType<HeroCombat>();
        DamageHelper.OnDamageApplied -= OnDamageApplied;
        DamageHelper.OnDamageApplied += OnDamageApplied;
    }

    public void OnUpgrade(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, data != null ? data.maxLevel : level);
        if (data != null)
        {
            Debug.Log($"BlackDustUpgrade Level {currentLevel}: chance={data.cloudChance}, duration={data.cloudDuration + (currentLevel >= 2 ? data.level2_extraDuration : 0f)}, dps={data.cloudDPS * (currentLevel >= 4 ? data.level4_dpsMultiplier : 1f)}, slow={currentLevel >= 3}");
        }
    }

    public void Activate() { }

    private void OnDestroy()
    {
        DamageHelper.OnDamageApplied -= OnDamageApplied;
    }

    private void OnDamageApplied(GameObject src, Vector3 hitPos)
    {
        if (data == null) return;
        if (src == null) return;
        if (owner != null && src != owner) return;
        float roll = Random.value;
        if (roll > data.cloudChance) return;
        SpawnCloud(hitPos);
    }

    private void SpawnCloud(Vector3 pos)
    {
        GameObject go = null;
        if (data.cloudPrefab != null)
        {
            go = Instantiate(data.cloudPrefab, pos, Quaternion.identity);
        }
        else
        {
            go = new GameObject("BlackDustCloud");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.2f, 0.1f, 0.3f, 0.7f);
            var texture = new Texture2D(64, 64);
            var center = new Vector2(32, 32);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= 30)
                    {
                        float alpha = 1f - (dist / 30f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else texture.SetPixel(x, y, Color.clear);
                }
            }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 100f);
            sr.sprite = sprite;
        }

        var cloud = go.GetComponent<BlackDustBullet>();
        if (cloud == null) cloud = go.AddComponent<BlackDustBullet>();

        float dur = data.cloudDuration + (currentLevel >= 2 ? data.level2_extraDuration : 0f);
        float dps = data.cloudDPS * (currentLevel >= 4 ? data.level4_dpsMultiplier : 1f);
        bool slow = currentLevel >= 3;
        float slowFactor = data.slowFactor;
        bool notifyController = currentLevel >= 5;

        cloud.Initialize(gameObject, dur, data.cloudRadius, dps, slow, slowFactor, notifyController);

        if (notifyController) RegisterCloud(cloud);
    }

    private void RegisterCloud(BlackDustBullet c)
    {
        registeredClouds.Add(c);
        registeredClouds.RemoveAll(x => x == null);
        if (registeredClouds.Count >= data.level5_convergeCount) StartCoroutine(ConvergeAndExplode());
    }

    private IEnumerator ConvergeAndExplode()
    {
        if (registeredClouds.Count == 0) yield break;
        Vector3 center = Vector3.zero; int cnt = 0;
        foreach (var c in registeredClouds) if (c != null) { center += c.transform.position; cnt++; }
        if (cnt == 0) { registeredClouds.Clear(); yield break; }
        center /= cnt;

        float t = 0f, dur = 0.6f;
        var starts = new List<Vector3>();
        foreach (var c in registeredClouds) starts.Add(c != null ? c.transform.position : center);

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            for (int i = 0; i < registeredClouds.Count; i++)
            {
                if (registeredClouds[i] != null) registeredClouds[i].SetPosition(Vector3.Lerp(starts[i], center, p));
            }
            yield return null;
        }

        float damage = data.level5_convergeDamage;
        float aoe = 5f;
        DamageHelper.ApplyAoe(gameObject, center, aoe, damage, raw: false, popupType: DamagePopup.DamageType.Normal);

        foreach (var c in registeredClouds) if (c != null) c.ForceDestroy();
        registeredClouds.Clear();
    }
}
