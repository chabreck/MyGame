using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BloodCapacitorUpgrade : MonoBehaviour, IUpgradeBehavior
{
    private BloodCapacitorData data;
    private GameObject owner;
    private HeroMovement movement;
    private HeroModifierSystem mods;
    private HeroExperience heroExp;
    private HeroCombat combat;

    private int currentLevel = 0;
    private Coroutine autoAttractCoroutine;
    private Coroutine activeBuffCoroutine;
    private bool buffActive = false;
    private float currentMoveBonus = 0f;
    private float currentDamageBonus = 0f;

    public void Configure(BloodCapacitorData d)
    {
        data = d;
    }

    public void Initialize(GameObject ownerGO, UpgradeBase d)
    {
        owner = ownerGO;
        if (d is BloodCapacitorData ds)
            Configure(ds);
        movement = owner?.GetComponent<HeroMovement>() ?? FindObjectOfType<HeroMovement>();
        mods = owner?.GetComponent<HeroModifierSystem>() ?? FindObjectOfType<HeroModifierSystem>();
        heroExp = owner?.GetComponent<HeroExperience>() ?? FindObjectOfType<HeroExperience>();
        combat = owner?.GetComponent<HeroCombat>() ?? FindObjectOfType<HeroCombat>();
        if (heroExp != null)
        {
            heroExp.OnExperienceCollected -= OnExperienceCollected;
            heroExp.OnExperienceCollected += OnExperienceCollected;
        }
    }

    public void OnUpgrade(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, data != null ? data.maxLevel : level);
        if (data != null && currentLevel >= 4 && mods != null)
        {
            mods.AddModifier(StatType.ExperienceRadius, data.experienceRadiusMultiplier);
        }
        if (data != null && currentLevel >= 5)
        {
            if (autoAttractCoroutine == null) autoAttractCoroutine = StartCoroutine(AutoAttractLoop());
        }
        else
        {
            if (autoAttractCoroutine != null) { StopCoroutine(autoAttractCoroutine); autoAttractCoroutine = null; }
        }
    }

    public void Activate() { }

    private void OnDestroy()
    {
        if (heroExp != null) heroExp.OnExperienceCollected -= OnExperienceCollected;
    }

    private void OnExperienceCollected(int amount)
    {
        if (data == null) return;
        float duration = data.baseDuration + (currentLevel >= 2 ? data.level2_extraDuration : 0f);
        float moveBonus = data.moveSpeedBonus;
        float damageBonus = (currentLevel >= 3) ? data.damageBonus : 0f;
        if (!buffActive)
        {
            buffActive = true;
            currentMoveBonus = moveBonus;
            currentDamageBonus = damageBonus;
            if (mods != null)
            {
                if (Mathf.Abs(currentMoveBonus) > 0f) mods.AddModifier(StatType.MoveSpeed, currentMoveBonus, duration);
                if (Mathf.Abs(currentDamageBonus) > 0f) mods.AddModifier(StatType.Damage, currentDamageBonus, duration);
            }
            else
            {
                if (movement != null && Mathf.Abs(currentMoveBonus) > 0f) movement.AddSpeedBoost(currentMoveBonus, duration);
                if (combat != null && Mathf.Abs(currentDamageBonus) > 0f) combat.AddDamageBoost(currentDamageBonus, duration);
            }
            if (activeBuffCoroutine != null) StopCoroutine(activeBuffCoroutine);
            activeBuffCoroutine = StartCoroutine(BuffTimer(duration));
        }
        else
        {
            if (activeBuffCoroutine != null) StopCoroutine(activeBuffCoroutine);
            activeBuffCoroutine = StartCoroutine(BuffTimer(duration));
        }
    }

    private IEnumerator BuffTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (mods != null)
        {
            if (Mathf.Abs(currentMoveBonus) > 0f) mods.AddModifier(StatType.MoveSpeed, -currentMoveBonus);
            if (Mathf.Abs(currentDamageBonus) > 0f) mods.AddModifier(StatType.Damage, -currentDamageBonus);
        }
        else
        {
            if (movement != null && Mathf.Abs(currentMoveBonus) > 0f) movement.AddSpeedBoost(-currentMoveBonus, 0.01f);
            if (combat != null && Mathf.Abs(currentDamageBonus) > 0f) combat.AddDamageBoost(-currentDamageBonus);
        }
        buffActive = false;
        currentMoveBonus = 0f;
        currentDamageBonus = 0f;
        activeBuffCoroutine = null;
    }

    private IEnumerator AutoAttractLoop()
    {
        if (data == null) yield break;
        float pullSpeed = 8f;
        while (true)
        {
            yield return new WaitForSeconds(data.autoAttractInterval);
            var playerT = owner != null ? owner.transform : (FindObjectOfType<HeroExperience>()?.transform ?? null);
            if (playerT == null) continue;
            ExperienceCollector.AttractAllTo(playerT, pullSpeed, data.autoAttractPullDuration);
        }
    }
}
