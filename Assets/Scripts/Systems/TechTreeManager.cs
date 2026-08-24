using System.Collections.Generic;
using UnityEngine;

public class TechTreeManager : MonoBehaviour
{
    public static TechTreeManager Instance;
    public List<TechNodeData> allTechs;
    public List<TechNodeData> unlockedTechs = new List<TechNodeData>();

    // This is now private → Unity will stop complaining
    private Dictionary<BonusType, float> currentBonuses;

    void Awake()
    {
        Instance = this;
        // Initialize all bonuses to 1.0 (normal)
        foreach (BonusType b in System.Enum.GetValues(typeof(BonusType)))
            currentBonuses[b] = 1f;
    }

    public bool CanUnlock(TechNodeData tech)
    {
        // check prerequisites + research cost from player inventory
        // (you will fill this later)
        return true; // temporary
    }

    public void Unlock(TechNodeData tech)
    {
        if (unlockedTechs.Contains(tech)) return;
        unlockedTechs.Add(tech);

        // Apply bonuses
        foreach (var bonus in tech.bonuses)
        {
            currentBonuses[bonus.type] += bonus.value;
        }

        // Unlock recipes (you can store unlocked recipes in a list too)
    }

    public float GetBonus(BonusType type)
    {
        return currentBonuses.ContainsKey(type) ? currentBonuses[type] : 1f;
    }
}