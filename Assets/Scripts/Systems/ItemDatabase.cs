using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;
    public List<ItemData> allItems;

    void Awake()
    {
        Instance = this;
    }

    public ItemData GetItem(string name)
    {
        return allItems.Find(i => i.itemName == name);
    }
}