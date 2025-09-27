using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Blood Capacitor (Data)")]
public class BloodCapacitorData : UpgradeBase
{
    [Header("Pickup-triggered buffs")]
    [Tooltip("Move speed bonus expressed as fraction: 0.1 = +10%")]
    public float moveSpeedBonus = 0.1f;
    [Tooltip("Base duration (seconds) for level1 effect")]
    public float baseDuration = 2f;
    [Tooltip("Extra duration added at level 2")]
    public float level2_extraDuration = 2f;

    [Header("Damage buff (level 3)")]
    [Tooltip("Damage bonus when level >= 3 (fraction). 0.10 = +10%")]
    public float damageBonus = 0.10f;

    [Header("Experience collection (level 4)")]
    [Tooltip("Multiplier applied to experience collection radius when level >= 4 (e.g. 2 = x2)")]
    public float experienceRadiusMultiplier = 2f;

    [Header("Auto-attract (level 5)")]
    [Tooltip("Interval between automatic attract waves (seconds)")]
    public float autoAttractInterval = 45f;
    [Tooltip("How long a pickup is pulled towards player when auto-attract triggers")]
    public float autoAttractPullDuration = 0.8f;
    [Tooltip("Max search radius for attract (world units). Set large to catch far pickups.")]
    public float autoAttractRange = 20f;

    public override IUpgradeBehavior CreateBehavior(GameObject owner)
    {
        if (owner == null) return null;
        var comp = owner.GetComponent<BloodCapacitorUpgrade>();
        if (comp == null) comp = owner.AddComponent<BloodCapacitorUpgrade>();
        comp.Configure(this);
        return comp;
    }
}