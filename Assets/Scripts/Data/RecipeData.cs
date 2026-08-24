using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Automation/Recipe")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public List<ItemAmount> inputs;   // what you need
    public List<ItemAmount> outputs;  // what you get
    public float craftTime = 2f;      // seconds
    public MachineType requiredMachine; // Smelter, Refiner, Assembler...
    public TechNodeData requiredTech;   // null = always available
}

[System.Serializable]
public class ItemAmount
{
    public ItemData item;
    public int amount;
}

public enum MachineType
{
    Smelter,
    Refiner,
    Assembler,
    Miner
}