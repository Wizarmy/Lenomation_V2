using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTech", menuName = "Automation/Tech")]
public class TechNodeData : ScriptableObject
{
    public string techName;
    public List<TechNodeData> prerequisites;
    public List<ItemAmount> researchCost;
    public List<RecipeData> unlockedRecipes;
    public List<BonusData> bonuses;    // see below
}