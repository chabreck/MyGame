using UnityEngine;

/// 1 уровень— Малое поле радиус, наносит урон врагам внутри области.
/// 2 уровень — Враги внутри замедляются на 50%.
/// 3 уровень — Радиус увеличен в 1.5 раза
/// 4 уровень — Урон внутри поля увеличен в 1.5 раза.
/// 5 уровень — Враги, вошедшие в поле, получают мощный урон при первом контакте. 

[CreateAssetMenu(menuName = "Weapons/Warp Field")]
public class WarpFieldWeaponData : WeaponBase
{
    [Header("Field")]
    public float baseRadius = 1.5f;
    public GameObject areaPrefab;

    [Header("Slow")]
    public float slowFactor = 0.5f;

    [Header("Damage over time")]
    public float damagePerTick = 20f;
    public float tickInterval = 1f;

    [Header("First contact")]
    public float firstContactDamage = 60f;

    public override IWeaponBehavior CreateBehavior(GameObject owner)
    {
        return owner.AddComponent<WarpFieldBehavior>();
    }
}