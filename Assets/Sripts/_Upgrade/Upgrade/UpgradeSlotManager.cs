using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class UpgradeSlotManager : MonoBehaviour
{
    [SerializeField] private Image[] slots;
    private Dictionary<string, Image> activeIcons = new Dictionary<string, Image>();
    private string[] slotIds;

    private void Awake()
    {
        if (slots == null) slots = new Image[0];
        slotIds = new string[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (!slots[i].gameObject.activeSelf)
            {
                slots[i].sprite = null;
                slotIds[i] = null;
            }
        }

        RebuildActiveDictionary();
    }

    private void RebuildActiveDictionary()
    {
        activeIcons.Clear();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            var id = slotIds[i];
            if (!string.IsNullOrEmpty(id))
            {
                activeIcons[id] = slots[i];
            }
        }
    }

    public int TotalSlots => slots != null ? slots.Length : 0;
    public int UsedSlots => activeIcons.Count;
    public int FreeSlots => Mathf.Max(0, TotalSlots - UsedSlots);
    public bool IsFull() => FreeSlots <= 0;
    public bool Contains(string upgradeID) => !string.IsNullOrEmpty(upgradeID) && activeIcons.ContainsKey(upgradeID);

    public bool AddUpgradeIcon(string upgradeID, Sprite icon)
    {
        if (string.IsNullOrEmpty(upgradeID) || icon == null) return false;

        if (activeIcons.TryGetValue(upgradeID, out var existing) && existing != null)
        {
            existing.sprite = icon;
            existing.gameObject.SetActive(true);
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == existing) slotIds[i] = upgradeID;
            return true;
        }

        int foundIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (string.IsNullOrEmpty(slotIds[i]) || !slots[i].gameObject.activeSelf || slots[i].sprite == null)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex == -1) return false;

        slots[foundIndex].sprite = icon;
        slots[foundIndex].gameObject.SetActive(true);
        slotIds[foundIndex] = upgradeID;
        activeIcons[upgradeID] = slots[foundIndex];
        return true;
    }

    public void UpdateUpgradeIcon(string upgradeID, Sprite newIcon)
    {
        if (string.IsNullOrEmpty(upgradeID)) return;
        if (activeIcons.TryGetValue(upgradeID, out var slot))
        {
            slot.sprite = newIcon;
            slot.gameObject.SetActive(true);
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == slot) slotIds[i] = upgradeID;
        }
    }

    public bool ReplaceUpgradeId(string oldId, string newId, Sprite newIcon)
    {
        if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId)) return false;

        int idx = -1;
        for (int i = 0; i < slotIds.Length; i++)
        {
            if (slotIds[i] == oldId) { idx = i; break; }
        }

        if (idx == -1 && activeIcons.TryGetValue(oldId, out var slotImage))
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == slotImage) { idx = i; break; }
            }
        }

        if (idx == -1) return false;

        activeIcons.Remove(oldId);
        slotIds[idx] = newId;
        slots[idx].sprite = newIcon;
        slots[idx].gameObject.SetActive(true);
        activeIcons[newId] = slots[idx];
        return true;
    }

    public void ClearSlots()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null) continue;
            s.sprite = null;
            s.gameObject.SetActive(false);
            slotIds[i] = null;
        }
        activeIcons.Clear();
    }

    public void RemoveUpgrade(string upgradeID)
    {
        if (string.IsNullOrEmpty(upgradeID)) return;
        if (activeIcons.TryGetValue(upgradeID, out var slot))
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == slot)
                {
                    slots[i].sprite = null;
                    slots[i].gameObject.SetActive(false);
                    slotIds[i] = null;
                }
            }
            activeIcons.Remove(upgradeID);
        }
    }

    public override string ToString()
    {
        return $"UpgradeSlotManager: Total={TotalSlots}, Used={UsedSlots}, Free={FreeSlots}";
    }
}
