using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Automation/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType type;          // Ore, Ingot, Component, Machine, etc.
    public int maxStack = 100;
}

public enum ItemType
{
    Ore,
    Ingot,
    Refined,
    Component,
    Machine
}