using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeroCombat : MonoBehaviour
{
    private List<WeaponInstance> equipped = new List<WeaponInstance>();
    private HeroModifierSystem mods;
    public event Action<Vector3> OnAttack;

    private void Awake()
    {
        mods = GetComponent<HeroModifierSystem>();
    }

    private void Update()
    {
        UpdateWeapons();
    }

    public void EquipWeapon(WeaponBase data)
    {
        if (data == null) return;
        if (equipped.Exists(w => w.Data == data)) return;
        var inst = new WeaponInstance(data);
        inst.Initialize(gameObject, mods, this);
        equipped.Add(inst);
    }

    public void UpgradeWeapon(WeaponBase data)
    {
        var inst = equipped.Find(w => w.Data == data);
        if (inst != null) inst.Upgrade();
    }

    public void ApplyEvolution(WeaponBase newWeapon, WeaponBase sourceWeapon)
    {
        if (newWeapon == null) return;

        int replaceIndex = -1;
        if (sourceWeapon != null)
        {
            replaceIndex = equipped.FindIndex(w => w.Data == sourceWeapon);
        }

        if (replaceIndex == -1)
        {
            EquipWeapon(newWeapon);
            return;
        }

        var oldWeapon = equipped[replaceIndex];
        if (oldWeapon != null)
        {
            oldWeapon.Uninitialize();
        }

        var newInst = new WeaponInstance(newWeapon);
        newInst.Initialize(gameObject, mods, this);
        equipped[replaceIndex] = newInst;
    }

    private void UpdateWeapons()
    {
        for (int i = 0; i < equipped.Count; i++)
        {
            try
            {
                equipped[i].Behavior?.Activate();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"HeroCombat.UpdateWeapons: behavior Activate threw: {ex.Message}");
            }
        }
    }

    public void AttackAt(Vector3 pos)
    {
        pos.z = 0f;
        RaiseOnAttack(pos);
    }

    public void RaiseOnAttack(Vector3 pos)
    {
        OnAttack?.Invoke(pos);
    }

    public void AddDamageBoost(float amount, float duration)
    {
        GetComponent<HeroModifierSystem>()?.AddModifier(StatType.Damage, amount);
        StartCoroutine(RemoveDamageBoost(amount, duration));
    }

    private IEnumerator RemoveDamageBoost(float amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        GetComponent<HeroModifierSystem>()?.AddModifier(StatType.Damage, -amount);
    }

    public void AddDamageBoost(float amount)
    {
        AddDamageBoost(amount, 999999f);
    }

    public List<WeaponBase> GetEquippedWeaponData()
    {
        return equipped.ConvertAll(w => w.Data);
    }

    public List<WeaponInstance> GetEquippedWeaponInstances()
    {
        return new List<WeaponInstance>(equipped);
    }

    public List<string> GetActiveTags()
    {
        var tags = new List<string>();
        foreach (var w in equipped)
            if (w.Data?.tags != null)
                tags.AddRange(w.Data.tags);

        var upgradeSystem = FindObjectOfType<UpgradeSystem>();
        if (upgradeSystem != null)
        {
            try
            {
                var extra = upgradeSystem.GetActiveUpgradeTags();
                if (extra != null) tags.AddRange(extra);
            }
            catch { }
        }

        return tags.Distinct().ToList();
    }
}

public class WeaponInstance
{
    public WeaponBase Data { get; }
    public IWeaponBehavior Behavior { get; private set; }
    public int Level { get; private set; } = 1;

    public WeaponInstance(WeaponBase data) => Data = data;

    public void Initialize(GameObject owner, HeroModifierSystem mods, HeroCombat combat)
    {
        Behavior = Data.CreateBehavior(owner);
        if (Behavior != null)
        {
            try { Behavior.Initialize(owner, Data, mods, combat); } catch (Exception ex) { Debug.LogError($"WeaponInstance.Initialize: Behavior.Initialize threw: {ex.Message}"); }
            try { Behavior.OnUpgrade(Level); } catch (Exception ex) { Debug.LogWarning($"WeaponInstance.Initialize: Behavior.OnUpgrade threw: {ex.Message}"); }
        }
    }

    public void Upgrade()
    {
        Level = Mathf.Clamp(Level + 1, 1, Data != null ? Data.maxLevel : Level + 1);
        try { Behavior?.OnUpgrade(Level); } catch (Exception ex) { Debug.LogWarning($"WeaponInstance.Upgrade: Behavior.OnUpgrade threw: {ex.Message}"); }
    }

    public void Uninitialize()
    {
        if (Behavior == null) return;
        
        var mb = Behavior as MonoBehaviour;
        if (mb != null)
        {
            var forceDestroyMethod = mb.GetType().GetMethod("ForceDestroy");
            if (forceDestroyMethod != null)
            {
                forceDestroyMethod.Invoke(mb, null);
            }
            
            try { UnityEngine.Object.Destroy(mb); } catch { }
        }
        Behavior = null;
    }
}