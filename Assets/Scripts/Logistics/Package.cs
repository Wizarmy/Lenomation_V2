using UnityEngine;

public class Package : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer iconRenderer;   // Assigned in the prefab

    [Header("Runtime")]
    public ItemStack currentStack;

    /// <summary>
    /// Set the item this package represents and update the visual icon
    /// </summary>
    public void SetItem(ItemData item, int amount = 1)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        currentStack = new ItemStack { item = item, amount = amount };

        if (iconRenderer != null)
        {
            iconRenderer.sprite = item.icon;
            iconRenderer.enabled = item.icon != null;
        }
    }

    public void Clear()
    {
        currentStack = null;
        if (iconRenderer != null)
        {
            iconRenderer.sprite = null;
            iconRenderer.enabled = false;
        }
    }
}