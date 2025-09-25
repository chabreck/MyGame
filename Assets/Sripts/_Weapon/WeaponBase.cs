using UnityEngine;
using UnityEngine.Localization;

public abstract class WeaponBase : ScriptableObject
{
    [Header("General Info")]
    public LocalizedString weaponNameLocalized;
    public string weaponName;
    public Sprite weaponIcon;
    public string[] tags;

    [Header("Level Effects")]
    public LevelEffect[] levelEffects;
    public int maxLevel => levelEffects != null ? levelEffects.Length : 0;

    [System.Serializable]
    public class LevelEffect
    {
        public LocalizedString descriptionLocalized;
        public string description;
    }

    public abstract IWeaponBehavior CreateBehavior(GameObject owner);

    // === Getters ===
    public LocalizedString GetNameLocalized() => weaponNameLocalized;
    public string GetNameFallback() => string.IsNullOrEmpty(weaponName) ? name : weaponName;

    public LocalizedString GetLevelEffectLocalized(int level)
    {
        if (levelEffects != null && level > 0 && level <= levelEffects.Length)
            return levelEffects[level - 1].descriptionLocalized;
        return null;
    }

    public string GetLevelEffectFallback(int level)
    {
        if (levelEffects != null && level > 0 && level <= levelEffects.Length)
            return levelEffects[level - 1].description;
        return null;
    }
}