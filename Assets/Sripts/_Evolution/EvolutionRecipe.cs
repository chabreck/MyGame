using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EvolutionRecipe
{
    public List<string> requiredWeaponTags = new List<string>();
    public List<string> requiredUpgradeTags = new List<string>();
    public WeaponBase resultWeapon;
    [Range(0f,1f)] public float offerChance = 0.6f;

    public bool MatchesPair(WeaponBase weapon, UpgradeBase upgrade)
    {
        if (weapon == null || upgrade == null) return false;
        foreach (var t in requiredWeaponTags)
            if (!string.IsNullOrEmpty(t) && (weapon.tags == null || System.Array.IndexOf(weapon.tags, t) < 0))
                return false;
        foreach (var t in requiredUpgradeTags)
            if (!string.IsNullOrEmpty(t) && (upgrade.tags == null || System.Array.IndexOf(upgrade.tags, t) < 0))
                return false;
        return true;
    }
}