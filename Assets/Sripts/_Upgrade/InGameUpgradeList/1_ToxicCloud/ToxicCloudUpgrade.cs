using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ToxicCloudUpgrade : MonoBehaviour, IUpgradeBehavior
{
    private ToxicCloudData data;
    private HeroCombat combat;
    private int currentLevel = 0;
    private readonly List<ToxicCloudBullet> activeClouds = new List<ToxicCloudBullet>();
    private GameObject owner;

    private float lastCloudCreationTime = 0f;
    private const float MIN_CLOUD_COOLDOWN = 0.3f;

    public void Configure(ToxicCloudData d) 
    { 
        data = d; 
    }

    public void Initialize(GameObject ownerGO, UpgradeBase d)
    {
        owner = ownerGO;
        if (d is ToxicCloudData bd) Configure(bd);
        combat = owner?.GetComponent<HeroCombat>() ?? FindObjectOfType<HeroCombat>();
        
        DamageHelper.OnDamageApplied -= OnDamageApplied;
        DamageHelper.OnDamageApplied += OnDamageApplied;
    }

    public void OnUpgrade(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, data != null ? data.maxLevel : level);
        
        if (currentLevel >= 5)
        {
            MakeCloudsPermanent();
        }
    }

    public void Activate() { }

    private void OnDestroy()
    {
        DamageHelper.OnDamageApplied -= OnDamageApplied;
        ClearAllClouds();
    }

    private void Update()
    {
        CleanCloudList();
        
        if (currentLevel < 5)
        {
            RemoveExpiredClouds();
        }
    }

    private void OnDamageApplied(GameObject src, Vector3 hitPos, DamageHelper.DamageSourceType sourceType)
    {
        if (data == null) return;
        if (src == null) return;
        
        if (owner != null && src != owner) return;
        
        if (sourceType == DamageHelper.DamageSourceType.Poison || 
            sourceType == DamageHelper.DamageSourceType.AreaEffect ||
            sourceType == DamageHelper.DamageSourceType.Pulse)
        {
            return;
        }
        
        if (Time.time - lastCloudCreationTime < MIN_CLOUD_COOLDOWN) return;
        
        float roll = Random.value;
        if (roll > data.cloudChance) return;
        
        SpawnCloud(hitPos);
        lastCloudCreationTime = Time.time;
    }

    private void SpawnCloud(Vector3 pos)
    {
        if (currentLevel >= 5 && activeClouds.Count >= data.level5_maxClouds)
        {
            RemoveOldestCloud();
        }
        
        GameObject go = null;
        if (data.cloudPrefab != null)
        {
            go = Instantiate(data.cloudPrefab, pos, Quaternion.identity);
        }
        else
        {
            go = CreateSimpleCloudVisual(pos);
        }

        var cloud = go.GetComponent<ToxicCloudBullet>();
        if (cloud == null) cloud = go.AddComponent<ToxicCloudBullet>();

        float dur = data.cloudDuration + (currentLevel >= 2 ? data.level2_extraDuration : 0f);
        float dps = data.cloudDPS * (currentLevel >= 4 ? data.level4_dpsMultiplier : 1f);
        bool slow = currentLevel >= 3;
        bool isPermanent = currentLevel >= 5;

        cloud.Initialize(owner, dur, data.cloudRadius, dps, slow, data.slowFactor, isPermanent);

        RegisterCloud(cloud);
    }

    private GameObject CreateSimpleCloudVisual(Vector3 pos)
    {
        GameObject go = new GameObject("ToxicCloud");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.2f, 0.1f, 0.3f, 0.7f);
        sr.sortingOrder = 10;
        
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
        
        return go;
    }

    private void RegisterCloud(ToxicCloudBullet cloud)
    {
        if (cloud == null) return;
        activeClouds.Add(cloud);
    }

    private void CleanCloudList()
    {
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            if (activeClouds[i] == null)
            {
                activeClouds.RemoveAt(i);
            }
        }
    }

    private void RemoveExpiredClouds()
    {
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            var cloud = activeClouds[i];
            if (cloud != null && cloud.Age > cloud.lifetime)
            {
                activeClouds.RemoveAt(i);
                cloud.ForceDestroy();
            }
        }
    }

    private void MakeCloudsPermanent()
    {
        foreach (var cloud in activeClouds)
        {
            if (cloud != null)
            {
                cloud.isPermanent = true;
            }
        }
    }

    private void RemoveOldestCloud()
    {
        if (activeClouds.Count == 0) return;
        
        ToxicCloudBullet oldestCloud = null;
        float oldestAge = -1f;
        
        foreach (var cloud in activeClouds)
        {
            if (cloud != null && cloud.Age > oldestAge)
            {
                oldestAge = cloud.Age;
                oldestCloud = cloud;
            }
        }
        
        if (oldestCloud != null)
        {
            activeClouds.Remove(oldestCloud);
            oldestCloud.ForceDestroy();
        }
    }

    public void ClearAllClouds()
    {
        foreach (var cloud in activeClouds)
        {
            if (cloud != null)
            {
                cloud.ForceDestroy();
            }
        }
        activeClouds.Clear();
    }
}