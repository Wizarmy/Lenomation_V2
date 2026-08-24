#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ItemDataCreator
{
    [MenuItem("Automation/Create All Base Items")]
    public static void CreateAllBaseItems()
    {
        // Make sure the folder exists
        string folderPath = "Assets/ScriptableObjects/Items";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            // Create the folders if they don't exist
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");
        }

        // Create all the items
        CreateItem("IronOre",     "Iron Ore",     ItemType.Ore);
        CreateItem("CopperOre",   "Copper Ore",   ItemType.Ore);
        CreateItem("IronIngot",   "Iron Ingot",   ItemType.Ingot);
        CreateItem("CopperIngot", "Copper Ingot", ItemType.Ingot);
        CreateItem("IronPlate",   "Iron Plate",   ItemType.Refined);
        CreateItem("CopperPlate", "Copper Plate", ItemType.Refined);
        CreateItem("IronGear",    "Iron Gear",    ItemType.Component, 50);
        CreateItem("CopperWire",  "Copper Wire",  ItemType.Component, 50);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("All base ItemData assets created successfully!");
    }

    private static void CreateItem(string fileName, string displayName, ItemType type, int maxStack = 100)
    {
        string path = $"Assets/ScriptableObjects/Items/{fileName}.asset";

        // Skip if it already exists
        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null)
        {
            Debug.Log($"Skipped {fileName} (already exists)");
            return;
        }

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = displayName;
        item.type = type;
        item.maxStack = maxStack;

        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"Created: {fileName}");
    }
}
#endif