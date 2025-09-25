using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Nervous Toxin")]
public class NervousToxinEvolutionData : WeaponBase
{
    public float pulseBaseDamage = 6f;
    public float pulseRadius = 2.5f;
    public float pulseScaleFromPoisonTick = 0.25f;
    [Range(0f,1f)] public float xpAttractChancePerPoisonTick = 0.06f;
    public float xpAttractRadius = 10f;
    public override IWeaponBehavior CreateBehavior(GameObject owner)
    {
        return owner.AddComponent<NervousToxinBehavior>();
    }
}