using UnityEngine;

public enum ConnectionType
{
    Input,      // can only receive items
    Output,     // can only provide items
    Both        // can do either (most common for belts & chests)
}

public class ConnectionPoint : MonoBehaviour
{
    [Header("Settings")]
    public ConnectionType type = ConnectionType.Both;
    public float radius = 0.4f;

    [Header("Runtime (auto)")]
    public MonoBehaviour owner;                 // Conveyor, Container, Machine…

    public Conveyor AsConveyor => owner as Conveyor;
    public Container AsContainer => owner as Container;

    void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<Conveyor>()
                 ?? GetComponentInParent<Container>() as MonoBehaviour;
    }

    // ----- API that inserters will call -----

    public bool CanProvideItem()
    {
        if (type == ConnectionType.Input) return false;

        if (AsConveyor != null) return AsConveyor.items.Count > 0;
        if (AsContainer != null) return AsContainer.slots.Exists(s => s != null && s.item != null);
        return false;
    }

    public bool CanAcceptItem()
    {
        if (type == ConnectionType.Output) return false;

        if (AsConveyor != null) return AsConveyor.HasSpace();
        if (AsContainer != null) return AsContainer.slots.Exists(s => s == null || s.item == null);
        return false;
    }

    public bool TryTakeItem(out ItemStack stack, out Package visual)
    {
        stack = null;
        visual = null;

        if (type == ConnectionType.Input) return false;

        if (AsConveyor != null)
            return AsConveyor.TryTakeItem(out stack, out visual);

        if (AsContainer != null)
        {
            for (int i = 0; i < AsContainer.slots.Count; i++)
            {
                if (AsContainer.TryTakeItem(i, out stack, out visual))
                    return true;
            }
        }

        return false;
    }

    public bool TryAddItem(ItemData item, int amount, Package existingVisual = null)
    {
        if (item == null || amount <= 0) return false;
        if (type == ConnectionType.Output) return false;

        if (AsConveyor != null)
            return AsConveyor.TryAddItem(item, amount, existingVisual);

        if (AsContainer != null)
            // Container owns the visual-destruction policy
            return AsContainer.TryAddItem(item, amount, existingVisual);

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = type switch
        {
            ConnectionType.Input  => Color.green,
            ConnectionType.Output => Color.red,
            _                     => Color.yellow
        };
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}