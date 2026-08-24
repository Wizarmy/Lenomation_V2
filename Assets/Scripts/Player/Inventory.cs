using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    public List<ItemStack> items = new List<ItemStack>();

    public bool AddItem(ItemData item, int amount)
    {
        // find existing stack or create new
        // keep it simple – no max size check for now
        var stack = items.Find(s => s.item == item);
        if (stack != null)
            stack.amount += amount;
        else
            items.Add(new ItemStack { item = item, amount = amount });
        return true;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        var stack = items.Find(s => s.item == item);
        if (stack == null || stack.amount < amount) return false;
        stack.amount -= amount;
        if (stack.amount <= 0) items.Remove(stack);
        return true;
    }

    public int GetAmount(ItemData item)
    {
        var stack = items.Find(s => s.item == item);
        return stack != null ? stack.amount : 0;
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemData item;
    public int amount;
}