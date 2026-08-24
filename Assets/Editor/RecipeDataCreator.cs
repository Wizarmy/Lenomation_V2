#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RecipeDataCreator
{
    [MenuItem("Automation/Create All Base Recipes")]
    public static void CreateAllBaseRecipes()
    {
        // Make sure the Recipes folder exists
        string folderPath = "Assets/ScriptableObjects/Recipes";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Recipes");
        }

        // Load the items we created earlier
        ItemData ironOre     = LoadItem("IronOre");
        ItemData copperOre   = LoadItem("CopperOre");
        ItemData ironIngot   = LoadItem("IronIngot");
        ItemData copperIngot = LoadItem("CopperIngot");
        ItemData ironPlate   = LoadItem("IronPlate");
        ItemData copperPlate = LoadItem("CopperPlate");
        ItemData ironGear    = LoadItem("IronGear");
        ItemData copperWire  = LoadItem("CopperWire");

        // ========== SMELTING ==========
        CreateRecipe(
            "SmeltIron",
            "Smelt Iron Ore",
            new List<ItemAmount> { new ItemAmount { item = ironOre, amount = 1 } },
            new List<ItemAmount> { new ItemAmount { item = ironIngot, amount = 1 } },
            2f,
            MachineType.Smelter
        );

        CreateRecipe(
            "SmeltCopper",
            "Smelt Copper Ore",
            new List<ItemAmount> { new ItemAmount { item = copperOre, amount = 1 } },
            new List<ItemAmount> { new ItemAmount { item = copperIngot, amount = 1 } },
            2f,
            MachineType.Smelter
        );

        // ========== REFINING ==========
        CreateRecipe(
            "RefineIronPlate",
            "Refine Iron Plate",
            new List<ItemAmount> { new ItemAmount { item = ironIngot, amount = 1 } },
            new List<ItemAmount> { new ItemAmount { item = ironPlate, amount = 1 } },
            3f,
            MachineType.Refiner
        );

        CreateRecipe(
            "RefineCopperPlate",
            "Refine Copper Plate",
            new List<ItemAmount> { new ItemAmount { item = copperIngot, amount = 1 } },
            new List<ItemAmount> { new ItemAmount { item = copperPlate, amount = 1 } },
            3f,
            MachineType.Refiner
        );

        // ========== COMPONENTS ==========
        CreateRecipe(
            "MakeIronGear",
            "Make Iron Gear",
            new List<ItemAmount> { new ItemAmount { item = ironPlate, amount = 2 } },
            new List<ItemAmount> { new ItemAmount { item = ironGear, amount = 1 } },
            4f,
            MachineType.Assembler
        );

        CreateRecipe(
            "MakeCopperWire",
            "Make Copper Wire",
            new List<ItemAmount> { new ItemAmount { item = copperPlate, amount = 1 } },
            new List<ItemAmount> { new ItemAmount { item = copperWire, amount = 2 } },
            2.5f,
            MachineType.Assembler
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("All base recipes created successfully!");
    }

    private static ItemData LoadItem(string itemName)
    {
        string path = $"Assets/ScriptableObjects/Items/{itemName}.asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

        if (item == null)
            Debug.LogError($"Could not find item: {itemName}. Did you run 'Create All Base Items' first?");

        return item;
    }

    private static void CreateRecipe(
        string fileName,
        string displayName,
        List<ItemAmount> inputs,
        List<ItemAmount> outputs,
        float craftTime,
        MachineType machineType)
    {
        string path = $"Assets/ScriptableObjects/Recipes/{fileName}.asset";

        // Skip if it already exists
        if (AssetDatabase.LoadAssetAtPath<RecipeData>(path) != null)
        {
            Debug.Log($"Skipped {fileName} (already exists)");
            return;
        }

        RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
        recipe.recipeName = displayName;
        recipe.inputs = inputs;
        recipe.outputs = outputs;
        recipe.craftTime = craftTime;
        recipe.requiredMachine = machineType;
        // requiredTech is left null = always available for now

        AssetDatabase.CreateAsset(recipe, path);
        Debug.Log($"Created recipe: {fileName}");
    }
}
#endif