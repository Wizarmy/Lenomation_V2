using UnityEngine;
using System.Collections.Generic;

public class Container : MonoBehaviour
{
    [Header("Inventory")]
    public int slotCount = 4;
    public List<ItemStack> slots = new List<ItemStack>();

    [Header("Footprint (tiles)")]
    public int footprintX = 2;
    public int footprintZ = 2;

    public Vector2Int Footprint => new Vector2Int(footprintX, footprintZ);

    void Awake()
    {
        while (slots.Count < slotCount)
            slots.Add(null);

        while (slots.Count > slotCount)
            slots.RemoveAt(slots.Count - 1);
    }

    public bool TryAddItem(ItemData item, int amount = 1, Package existingVisual = null)
    {
        if (item == null || amount <= 0) return false;

        int remaining = amount;

        // Stack into existing slots
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].item == item && slots[i].amount < item.maxStack)
            {
                int space = item.maxStack - slots[i].amount;
                int add = Mathf.Min(space, remaining);
                slots[i].amount += add;
                remaining -= add;
                if (remaining <= 0) break;
            }
        }

        // Empty slots
        if (remaining > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null || slots[i].item == null)
                {
                    int add = Mathf.Min(item.maxStack, remaining);
                    slots[i] = new ItemStack { item = item, amount = add };
                    remaining -= add;
                    if (remaining <= 0) break;
                }
            }
        }

        bool fullyAccepted = remaining <= 0;

        if (fullyAccepted && existingVisual != null)
            Destroy(existingVisual.gameObject);

        return fullyAccepted;
    }

    public bool TryTakeItem(int slotIndex, out ItemStack taken, out Package visual)
    {
        taken = null;
        visual = null;

        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        if (slots[slotIndex] == null || slots[slotIndex].item == null) return false;

        taken = slots[slotIndex];
        slots[slotIndex] = null;

        // Chests have no world visual – create one for transport
        if (PrefabManager.Instance != null && PrefabManager.Instance.packagePrefab != null)
        {
            GameObject go = Object.Instantiate(PrefabManager.Instance.packagePrefab);
            visual = go.GetComponent<Package>();
            if (visual != null)
                visual.SetItem(taken.item, taken.amount);
        }

        return true;
    }
}