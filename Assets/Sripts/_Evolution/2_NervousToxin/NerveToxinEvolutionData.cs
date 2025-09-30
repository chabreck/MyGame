using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Nerve Toxin")]
public class NerveToxinEvolutionData : WeaponBase
{
    [Header("Pulse Settings")]
    public float pulseBaseDamage = 6f;
    public float pulseRadius = 2.5f;
    public float pulseScaleFromPoisonTick = 0.25f;
    [Range(0f,1f)] public float xpAttractChancePerPoisonTick = 0.06f;

    [Header("Visual")]
    public GameObject pulseVisualPrefab;

    public override IWeaponBehavior CreateBehavior(GameObject owner)
    {
        return owner.AddComponent<NerveToxinBehavior>();
    }
}