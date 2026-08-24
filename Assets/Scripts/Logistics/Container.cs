using UnityEngine;
using System.Collections.Generic;

public class Container : MonoBehaviour
{
    [Header("Inventory")]
    public int slotCount = 4;
    public List<ItemStack> slots = new List<ItemStack>();

    void Awake()
    {
        // Ensure we always have exactly slotCount entries
        while (slots.Count < slotCount)
            slots.Add(null);

        while (slots.Count > slotCount)
            slots.RemoveAt(slots.Count - 1);
    }

    public bool TryAddItem(ItemData item, int amount = 1, Package existingVisual = null)
    {
        if (item == null || amount <= 0) return false;

        int remaining = amount;

        // First try to stack into existing slots
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

        // Then find empty slots
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

        // If we accepted the item and were given a visual,
        // destroy it because chests currently have no world representation.
        if (fullyAccepted && existingVisual != null)
        {
            Destroy(existingVisual.gameObject);
        }

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

        // Chests don’t currently have a visual Package, so visual stays null.
        // (Later we can spawn one if we want floating items in chests.)
        return true;
    }
}