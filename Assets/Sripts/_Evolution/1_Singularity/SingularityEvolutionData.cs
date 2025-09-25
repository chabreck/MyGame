using UnityEngine;

[CreateAssetMenu(menuName = "Evolution/Singularity")]
public class SingularityEvolutionData : WeaponBase
{
    public GameObject areaPrefab;
    
    public float fieldBaseRadius = 2f;
    public float fieldBaseDamage = 10f;
    public float firstContactBonusDamage = 25f;
    public float slowFactor = 0.5f;
    
    public float absorbRadiusPerCloud = 0.01f;
    public float absorbDamagePerCloud = 1.5f;
    public float absorbCheckInterval = 0.5f;
    
    public float tickInterval = 1f;
    
    public override IWeaponBehavior CreateBehavior(GameObject owner)
    {
        return owner.AddComponent<SingularityBehavior>();
    }
}