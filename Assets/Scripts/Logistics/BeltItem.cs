using UnityEngine;

[System.Serializable]
public class BeltItem
{
    public ItemStack stack;
    public float progress;          // 0 = start of belt, 1 = end of belt
    public Transform visual;        // the GameObject sitting on the belt
}