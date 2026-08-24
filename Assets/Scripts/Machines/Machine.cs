using UnityEngine;
using System.Collections.Generic;

public class Machine : MonoBehaviour
{
    [Header("Machine Setup")]
    public MachineData data;
    public RecipeData currentRecipe;

    [Header("Inventories")]
    public Inventory inputInventory = new Inventory();
    public Inventory outputInventory = new Inventory();

    [Header("Runtime")]
    public float progress = 0f;
    public bool isWorking = false;

    void Update()
    {
        if (data == null) return;

        // Special case: Miner (produces without needing a recipe for now)
        if (data.type == MachineType.Miner)
        {
            HandleMiner();
            return;
        }

        // Normal crafting machines
        if (currentRecipe == null) return;
        if (!HasEnoughInputs()) return;

        // Calculate speed with bonuses
        float speedMultiplier = 1f;
        if (TechTreeManager.Instance != null)
        {
            speedMultiplier = TechTreeManager.Instance.GetBonus(BonusType.MachineSpeed);
        }

        float finalSpeed = data.baseSpeed * speedMultiplier;

        progress += Time.deltaTime * finalSpeed;
        isWorking = true;

        if (progress >= currentRecipe.craftTime)
        {
            Craft();
            progress = 0f;
        }
    }

    void HandleMiner()
    {
        // Very simple miner for now – produces Iron Ore every few seconds
        // You can improve this later with resource nodes
        progress += Time.deltaTime * data.baseSpeed;

        if (progress >= 3f) // 3 seconds per ore
        {
            ItemData ironOre = null;

            // Try to find Iron Ore from the database if you have one
            // For now we hardcode a simple version
            if (ItemDatabase.Instance != null)
            {
                ironOre = ItemDatabase.Instance.GetItem("Iron Ore");
            }

            if (ironOre != null)
            {
                outputInventory.AddItem(ironOre, 1);
            }

            progress = 0f;
        }
    }

    bool HasEnoughInputs()
    {
        if (currentRecipe == null || currentRecipe.inputs == null) return false;

        foreach (ItemAmount need in currentRecipe.inputs)
        {
            if (need.item == null) continue;

            if (inputInventory.GetAmount(need.item) < need.amount)
                return false;
        }
        return true;
    }

    void Craft()
    {
        if (currentRecipe == null) return;

        // 1. Remove inputs
        foreach (ItemAmount need in currentRecipe.inputs)
        {
            inputInventory.RemoveItem(need.item, need.amount);
        }

        // 2. Calculate yield bonus
        float yieldMultiplier = 1f;
        if (TechTreeManager.Instance != null)
        {
            yieldMultiplier = TechTreeManager.Instance.GetBonus(BonusType.RefineYield);
        }

        // 3. Add outputs
        foreach (ItemAmount output in currentRecipe.outputs)
        {
            int finalAmount = Mathf.RoundToInt(output.amount * yieldMultiplier);
            outputInventory.AddItem(output.item, finalAmount);
        }

        isWorking = false;
    }

    // ========== Public methods used by Inserters ==========

    /// <summary>
    /// Used by inserters to put items into this machine
    /// </summary>
    public bool TryAddToInput(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;
        return inputInventory.AddItem(item, amount);
    }

    /// <summary>
    /// Used by inserters to take items from this machine's output
    /// </summary>
    public bool TryTakeFromOutput(out ItemStack taken)
    {
        taken = null;

        if (outputInventory.items.Count == 0) return false;

        taken = outputInventory.items[0];
        outputInventory.RemoveItem(taken.item, taken.amount);
        return true;
    }

    // ========== Recipe Management ==========

    public void SetRecipe(RecipeData newRecipe)
    {
        if (newRecipe == null) return;

        // Only allow correct machine type
        if (newRecipe.requiredMachine != data.type)
        {
            Debug.LogWarning($"Recipe {newRecipe.recipeName} cannot be used on {data.machineName}");
            return;
        }

        currentRecipe = newRecipe;
        progress = 0f;
        isWorking = false;
    }

    public void ClearRecipe()
    {
        currentRecipe = null;
        progress = 0f;
        isWorking = false;
    }
}