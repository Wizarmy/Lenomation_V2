#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TechNodeDataCreator
{
    [MenuItem("Automation/Create All Base Tech Nodes")]
    public static void CreateAllBaseTechNodes()
    {
        // Make sure the Tech folder exists
        string folderPath = "Assets/ScriptableObjects/Tech";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tech");
        }

        // Load items we need for research costs
        ItemData ironIngot   = LoadItem("IronIngot");
        ItemData copperIngot = LoadItem("CopperIngot");
        ItemData ironPlate   = LoadItem("IronPlate");
        ItemData copperWire  = LoadItem("CopperWire");

        // Load recipes
        RecipeData smeltIron       = LoadRecipe("SmeltIron");
        RecipeData smeltCopper     = LoadRecipe("SmeltCopper");
        RecipeData refineIronPlate = LoadRecipe("RefineIronPlate");
        RecipeData refineCopperPlate = LoadRecipe("RefineCopperPlate");
        RecipeData makeIronGear    = LoadRecipe("MakeIronGear");
        RecipeData makeCopperWire  = LoadRecipe("MakeCopperWire");

        // ========== BASIC TECHS ==========

        // 1. Basic Smelting (starting tech)
        CreateTech(
            "BasicSmelting",
            "Basic Smelting",
            null, // no prerequisites
            new List<ItemAmount>(), // free
            new List<RecipeData> { smeltIron, smeltCopper },
            new List<BonusData>()
        );

        // 2. Improved Smelting
        CreateTech(
            "ImprovedSmelting",
            "Improved Smelting",
            new string[] { "BasicSmelting" },
            new List<ItemAmount> {
                new ItemAmount { item = ironIngot, amount = 10 },
                new ItemAmount { item = copperIngot, amount = 10 }
            },
            new List<RecipeData>(),
            new List<BonusData> {
                new BonusData { type = BonusType.SmeltSpeed, value = 0.25f } // +25% smelting speed
            }
        );

        // 3. Basic Refining
        CreateTech(
            "BasicRefining",
            "Basic Refining",
            new string[] { "BasicSmelting" },
            new List<ItemAmount> {
                new ItemAmount { item = ironIngot, amount = 15 }
            },
            new List<RecipeData> { refineIronPlate, refineCopperPlate },
            new List<BonusData>()
        );

        // 4. Component Assembly
        CreateTech(
            "ComponentAssembly",
            "Component Assembly",
            new string[] { "BasicRefining" },
            new List<ItemAmount> {
                new ItemAmount { item = ironPlate, amount = 10 },
                new ItemAmount { item = copperWire, amount = 10 }
            },
            new List<RecipeData> { makeIronGear, makeCopperWire },
            new List<BonusData> {
                new BonusData { type = BonusType.MachineSpeed, value = 0.15f } // +15% machine speed
            }
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("All base Tech Nodes created successfully!");
    }

    private static ItemData LoadItem(string itemName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/ScriptableObjects/Items/{itemName}.asset");
    }

    private static RecipeData LoadRecipe(string recipeName)
    {
        return AssetDatabase.LoadAssetAtPath<RecipeData>($"Assets/ScriptableObjects/Recipes/{recipeName}.asset");
    }

    private static void CreateTech(
        string fileName,
        string displayName,
        string[] prerequisiteNames,
        List<ItemAmount> researchCost,
        List<RecipeData> unlockedRecipes,
        List<BonusData> bonuses)
    {
        string path = $"Assets/ScriptableObjects/Tech/{fileName}.asset";

        if (AssetDatabase.LoadAssetAtPath<TechNodeData>(path) != null)
        {
            Debug.Log($"Skipped {fileName} (already exists)");
            return;
        }

        TechNodeData tech = ScriptableObject.CreateInstance<TechNodeData>();
        tech.techName = displayName;
        tech.researchCost = researchCost;
        tech.unlockedRecipes = unlockedRecipes;
        tech.bonuses = bonuses;

        // Handle prerequisites
        tech.prerequisites = new List<TechNodeData>();
        if (prerequisiteNames != null)
        {
            foreach (string preName in prerequisiteNames)
            {
                TechNodeData pre = AssetDatabase.LoadAssetAtPath<TechNodeData>($"Assets/ScriptableObjects/Tech/{preName}.asset");
                if (pre != null)
                    tech.prerequisites.Add(pre);
            }
        }

        AssetDatabase.CreateAsset(tech, path);
        Debug.Log($"Created tech: {fileName}");
    }
}
#endif