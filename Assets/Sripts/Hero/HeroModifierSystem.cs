using UnityEngine;
using System.Collections.Generic;

public class HeroModifierSystem : MonoBehaviour
{
    private class ModifierEntry
    {
        public float value;
        public float endTime; // время, когда бафф заканчивается; float.MaxValue = бесконечный
    }

    private Dictionary<StatType, List<ModifierEntry>> modifiers = new();

    public void AddModifier(StatType type, float value, float duration = -1f)
    {
        if (!modifiers.ContainsKey(type))
            modifiers[type] = new List<ModifierEntry>();

        float endTime = duration > 0 ? Time.time + duration : float.MaxValue;
        modifiers[type].Add(new ModifierEntry { value = value, endTime = endTime });
    }

    public float GetModifier(StatType type)
    {
        float sum = 0f;
        if (modifiers.TryGetValue(type, out var list))
        {
            // убираем истёкшие баффы
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].endTime <= Time.time)
                    list.RemoveAt(i);
            }

            foreach (var m in list)
                sum += m.value;
        }

        return 1f + sum; // итоговый множитель
    }
}

public enum StatType
{
    Damage,
    AttackSpeed,
    MoveSpeed,
    DashCooldown,
    Health,
    ExperienceRadius,
    CollectionSpeed
}