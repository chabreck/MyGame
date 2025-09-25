using UnityEngine;
using UnityEngine.Localization;

public abstract class UpgradeBase : ScriptableObject
{
    [Header("General Info")]
    public LocalizedString upgradeNameLocalized;
    public string upgradeName;
    public Sprite upgradeIcon;
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

    public abstract IUpgradeBehavior CreateBehavior(GameObject owner);
    
    // --- Getters ---
    public LocalizedString GetNameLocalized() => upgradeNameLocalized;
    public string GetNameFallback() => string.IsNullOrEmpty(upgradeName) ? name : upgradeName;

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