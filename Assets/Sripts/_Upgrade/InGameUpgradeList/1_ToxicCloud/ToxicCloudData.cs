using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Upgrades/Black Dust (Data)")]
public class ToxicCloudData : UpgradeBase
{
    [Header("Cloud params")]
    [Range(0f,1f)] public float cloudChance = 0.30f;
    public float cloudDuration = 3f;
    public float cloudRadius = 2f;
    public float cloudDPS = 4f;
    public float slowFactor = 0.4f;

    [Header("Scaling / level tweaks")]
    public float level2_extraDuration = 2f;
    public float level4_dpsMultiplier = 1.2f;
    
    [Header("Level 5 - Permanent Clouds")]
    public int level5_maxClouds = 4;

    [Header("Visual prefab (optional)")]
    public GameObject cloudPrefab;

    public override IUpgradeBehavior CreateBehavior(GameObject owner)
    {
        if (owner == null) return null;
        var comp = owner.GetComponent<ToxicCloudUpgrade>();
        if (comp == null) comp = owner.AddComponent<ToxicCloudUpgrade>();
        comp.Configure(this);
        return comp;
    }
}