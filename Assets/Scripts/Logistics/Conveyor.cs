using UnityEngine;
using System.Collections.Generic;

public class Conveyor : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = ConveyorConfig.DefaultMoveSpeed;
    public int maxItems = ConveyorConfig.MaxItemsPerBelt;

    [Header("Belt Level")]
    [Range(1, 5)]
    public int beltLevel = 1;

    [Header("Type & Direction")]
    public bool isCorner = false;
    public BeltDirection direction = BeltDirection.Clockwise;

    [Header("Connection (auto-detected)")]
    public Conveyor nextConveyor;
    
    // Add this field
    private float pathLength;

    // -------------------------------------------------
    // Internal
    // -------------------------------------------------
    [System.Serializable]
    public class BeltItem
    {
        public Package package;
        public float progress;        // 0 → 1
        public ItemStack stack;
    }

    public List<BeltItem> items = new List<BeltItem>();
    private Transform cachedTransform;
    private bool arrowsNeedFlip = false;

    // -------------------------------------------------
    // Lifecycle
    // -------------------------------------------------
    void Awake()
    {
        cachedTransform = transform;
        moveSpeed = ConveyorConfig.DefaultMoveSpeed;
        maxItems  = ConveyorConfig.MaxItemsPerBelt;

        // Cache the real travel distance of this piece
        pathLength = isCorner 
            ? ConveyorConfig.CornerPathLength 
            : ConveyorConfig.StraightPathLength;
    }

    void OnEnable()
    {
        if (ConveyorManager.Instance != null)
            ConveyorManager.Instance.Register(this);
    }

    void OnDisable()
    {
        if (ConveyorManager.Instance != null)
            ConveyorManager.Instance.Unregister(this);
    }

    void Start()
    {
        ApplyDirectionVisuals();
        DetectNextConveyor();
    }

    void Update()
    {
        // Global start/stop
        if (ConveyorManager.Instance == null || !ConveyorManager.Instance.isRunning)
            return;

        if (items.Count == 0) return;

        // Constant linear speed (world units per second)
        float delta = (moveSpeed * Time.deltaTime) / pathLength;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            BeltItem item = items[i];

            bool atEnd = item.progress >= 0.999f;
            bool canTransfer = nextConveyor != null && nextConveyor.HasSpace();

            if (atEnd && !canTransfer)
            {
                item.progress = 1f;
                UpdateItemTransform(item);
                continue;
            }

            item.progress += delta;

            if (item.progress >= 1f)
            {
                if (nextConveyor != null && nextConveyor.TryReceiveItem(item))
                {
                    items.RemoveAt(i);
                }
                else
                {
                    item.progress = 1f;
                    UpdateItemTransform(item);
                }
                continue;
            }

            UpdateItemTransform(item);
        }
    }

    // -------------------------------------------------
    // Public API
    // -------------------------------------------------
    public bool HasSpace() => items.Count < maxItems;

   public bool TryAddItem(ItemData itemData, int amount = 1, Package existingPackage = null)
{
    if (itemData == null || amount <= 0 || !HasSpace()) return false;

    // Minimum gap between package centres (in progress units)
    // Package size 0.22 on a 1.0 belt → roughly 0.28–0.30 is safe
    const float minGap = 0.30f;

    float[] preferredSlots = ConveyorConfig.GetSlotProgresses(); // 0.20, 0.50, 0.80
    float targetProgress = -1f;

    // First try the preferred middle slots
    foreach (float slot in preferredSlots)
    {
        if (IsSlotFree(slot, minGap))
        {
            targetProgress = slot;
            break;
        }
    }

    // If none of the preferred slots are free, look for any free space
    if (targetProgress < 0f)
    {
        for (float t = 0.05f; t <= 0.95f; t += 0.05f)
        {
            if (IsSlotFree(t, minGap))
            {
                targetProgress = t;
                break;
            }
        }
    }

    if (targetProgress < 0f) return false; // no space anywhere

    // ----- create or re-parent the visual -----
    Package pkg = existingPackage;
    if (pkg == null)
    {
        if (PrefabManager.Instance == null || PrefabManager.Instance.packagePrefab == null)
            return false;

        GameObject pkgGO = Instantiate(PrefabManager.Instance.packagePrefab, cachedTransform);
        pkg = pkgGO.GetComponent<Package>();
    }
    else
    {
        pkg.transform.SetParent(cachedTransform, worldPositionStays: false);
    }

    pkg.SetItem(itemData, amount);

    BeltItem newItem = new BeltItem
    {
        package = pkg,
        progress = targetProgress,
        stack = new ItemStack { item = itemData, amount = amount }
    };

    items.Add(newItem);
    UpdateItemTransform(newItem);
    return true;
}

/// <summary>
/// Returns true if the given progress position is far enough from every existing item.
/// </summary>
private bool IsSlotFree(float progress, float minGap)
{
    foreach (var existing in items)
    {
        if (Mathf.Abs(existing.progress - progress) < minGap)
            return false;
    }
    return true;
}

    public bool TryReceiveItem(BeltItem incoming)
    {
        if (!HasSpace()) return false;

        incoming.package.transform.SetParent(cachedTransform, worldPositionStays: true);
        incoming.progress = 0f;

        items.Add(incoming);
        UpdateItemTransform(incoming);
        return true;
    }

    public bool TryTakeItem(out ItemStack taken, out Package visual)
    {
        taken = null;
        visual = null;
        if (items.Count == 0) return false;

        // Prefer middle item
        int bestIndex = 0;
        float bestDist = Mathf.Abs(items[0].progress - 0.5f);
        for (int i = 1; i < items.Count; i++)
        {
            float dist = Mathf.Abs(items[i].progress - 0.5f);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        taken = items[bestIndex].stack;
        visual = items[bestIndex].package;          // hand over the visual

        // Remove from list but DO NOT destroy the GameObject
        items.RemoveAt(bestIndex);
        return true;
    }

    public void SetDirection(BeltDirection newDirection)
    {
        direction = newDirection;
        ApplyDirectionVisuals();

        // Tell the manager to update connections around this belt
        if (ConveyorManager.Instance != null)
            ConveyorManager.Instance.RebuildConnectionsAround(this);
    }

    // -------------------------------------------------
    // Direction & Arrows
    // -------------------------------------------------
    private void ApplyDirectionVisuals()
    {
        // Corners: natural = Clockwise
        // Straights: natural = AntiClockwise (opposite)
        bool shouldFlip = isCorner
            ? (direction == BeltDirection.AntiClockwise)
            : (direction == BeltDirection.Clockwise);

        if (shouldFlip != arrowsNeedFlip)
        {
            FlipAllArrows();
            arrowsNeedFlip = shouldFlip;
        }
    }

    private void FlipAllArrows()
    {
        foreach (Transform child in cachedTransform)
        {
            if (child.name.StartsWith("Arrow"))
            {
                child.localRotation *= Quaternion.Euler(0f, 180f, 0f);
            }
        }
    }

    public void DetectNextConveyor()
    {
        if (ConveyorManager.Instance != null)
            nextConveyor = ConveyorManager.Instance.GetNextConveyor(this);
        else
            nextConveyor = null;
    }

    public Vector3 GetExitWorldPositionPublic() => GetExitWorldPosition();
    public Vector3 GetExitWorldDirectionPublic() => GetExitWorldDirection();

    // -------------------------------------------------
    // Internal helpers
    // -------------------------------------------------
    private void RemoveItem(int index)
    {
        if (index < 0 || index >= items.Count) return;
        if (items[index].package != null)
            Destroy(items[index].package.gameObject);
        items.RemoveAt(index);
    }

    private void UpdateItemTransform(BeltItem item)
    {
        if (item.package == null) return;

        bool invert = isCorner
            ? (direction == BeltDirection.AntiClockwise)
            : (direction == BeltDirection.Clockwise);

        float t = invert ? (1f - item.progress) : item.progress;

        Vector3 localPos;
        Quaternion localRot;

        if (isCorner)
            GetCornerPosition(t, out localPos, out localRot);
        else
            GetStraightPosition(t, out localPos, out localRot);

        if (invert)
            localRot *= Quaternion.Euler(0f, 180f, 0f);

        item.package.transform.localPosition = localPos;
        item.package.transform.localRotation = localRot;
    }

    private void GetStraightPosition(float t, out Vector3 pos, out Quaternion rot)
    {
        float y = ConveyorConfig.HalfHeight + ConveyorConfig.PackageHalfSize + 0.001f;
        float z = Mathf.Lerp(-ConveyorConfig.HalfLength, ConveyorConfig.HalfLength, t);
        pos = new Vector3(0f, y, z);
        rot = Quaternion.identity;
    }

    private void GetCornerPosition(float t, out Vector3 pos, out Quaternion rot)
    {
        float radius = (ConveyorConfig.CornerInnerRadius + ConveyorConfig.CornerOuterRadius) * 0.5f;
        Vector3 centre = ConveyorConfig.CornerCentreOffset;

        float angle = Mathf.Lerp(180f, 90f, t) * Mathf.Deg2Rad;

        float x = centre.x + Mathf.Cos(angle) * radius;
        float z = centre.z + Mathf.Sin(angle) * radius;
        float y = ConveyorConfig.HalfHeight + ConveyorConfig.PackageHalfSize + 0.001f;

        pos = new Vector3(x, y, z);

        float tangentAngle = Mathf.Lerp(90f, 180f, t) + 90f;
        rot = Quaternion.Euler(0f, tangentAngle, 0f);
    }

    private Vector3 GetExitWorldPosition()
    {
        // Always sample the end of the *travel* path (progress = 1)
        bool invert = isCorner
            ? (direction == BeltDirection.AntiClockwise)
            : (direction == BeltDirection.Clockwise);

        float t = invert ? 0f : 1f;

        Vector3 localPos;
        Quaternion ignored;
        if (isCorner)
            GetCornerPosition(t, out localPos, out ignored);
        else
            GetStraightPosition(t, out localPos, out ignored);

        return cachedTransform.TransformPoint(localPos);
    }

    private Vector3 GetExitWorldDirection()
    {
        // Sample a point slightly before the exit and one at the exit,
        // then use the difference as the true travel direction.
        // This works for both Clockwise and Anti-Clockwise and for both straight & corner.

        bool invert = isCorner
            ? (direction == BeltDirection.AntiClockwise)
            : (direction == BeltDirection.Clockwise);

        float tEnd   = invert ? 0f : 1f;
        float tStart = invert ? 0.05f : 0.95f;   // a little before the end

        Vector3 posEnd, posStart;
        Quaternion ignored;

        if (isCorner)
        {
            GetCornerPosition(tEnd,   out posEnd,   out ignored);
            GetCornerPosition(tStart, out posStart, out ignored);
        }
        else
        {
            GetStraightPosition(tEnd,   out posEnd,   out ignored);
            GetStraightPosition(tStart, out posStart, out ignored);
        }

        Vector3 localDir = (posEnd - posStart).normalized;
        return cachedTransform.TransformDirection(localDir);
    }
    
}