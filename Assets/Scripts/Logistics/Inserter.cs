using UnityEngine;
using System.Collections.Generic;

public class Inserter : MonoBehaviour
{
    [Header("Settings")]
    public float swingSpeed = 180f;
    public float cooldown = 0.6f;
    public float maxLinkDistance = 1.2f;

    [Header("Linked Ports")]
    public ConnectionPoint sourcePort;
    public ConnectionPoint destPort;

    [Header("Arm Extension")]
    public float fullArmLength = 0.90f;      // matches the mesh
    public float retractedPercent = 0.30f;   // 30% while swinging
    public float extendSpeed = 4f;           // how fast it extends/retracts

    private float currentArmLength;
    private bool armExtended = false;
    
    // Internal
    private float timer = 0f;
    private bool holdingItem = false;
    private ItemStack heldStack;
    private Transform arm;
    private float armAngle = 0f;
    
    private Package heldVisual;
    
    private float restAngle = 0f;      // angle that points at source
    private float dropAngle = 180f;    // angle that points at destination

    // Arrow visuals
    private List<MeshRenderer> arrowRenderers = new List<MeshRenderer>();
    private static readonly Color ColorUnlinked = new Color(1f, 0.85f, 0.2f);   // yellow
    private static readonly Color ColorLinked   = new Color(0.2f, 0.9f, 0.3f);   // green
    private static readonly Color ColorBlocked  = new Color(0.95f, 0.25f, 0.2f); // red

    void Awake()
    {
        arm = transform.Find("Arm");

        // Cache all arrow renderers
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Arrow") || child.name.Contains("SideArrow"))
            {
                var mr = child.GetComponentInChildren<MeshRenderer>();
                if (mr != null) arrowRenderers.Add(mr);
            }
        }
        currentArmLength = fullArmLength * retractedPercent;
    }

  void Update()
{
    if (ConveyorManager.Instance == null || !ConveyorManager.Instance.isRunning)
        return;

    UpdateArrowVisuals();
    UpdateArmTargetAngles();

    timer -= Time.deltaTime;

    const float angleTolerance = 4f;

    if (!holdingItem)
    {
        // ----- Move to source -----
        armAngle = Mathf.MoveTowardsAngle(armAngle, restAngle, swingSpeed * Time.deltaTime);
        bool atPickupAngle = Mathf.Abs(Mathf.DeltaAngle(armAngle, restAngle)) < angleTolerance;

        if (atPickupAngle)
        {
            // Extend
            currentArmLength = Mathf.MoveTowards(currentArmLength, fullArmLength, extendSpeed * Time.deltaTime);
            armExtended = Mathf.Abs(currentArmLength - fullArmLength) < 0.01f;

            if (armExtended && timer <= 0f)
                TryPickup();
        }
        else
        {
            // Retract while swinging
            currentArmLength = Mathf.MoveTowards(currentArmLength, fullArmLength * retractedPercent, extendSpeed * Time.deltaTime);
            armExtended = false;
        }
    }
    else
    {
        // ----- Move to destination -----
        armAngle = Mathf.MoveTowardsAngle(armAngle, dropAngle, swingSpeed * Time.deltaTime);
        bool atDropAngle = Mathf.Abs(Mathf.DeltaAngle(armAngle, dropAngle)) < angleTolerance;

        if (atDropAngle)
        {
            // Extend
            currentArmLength = Mathf.MoveTowards(currentArmLength, fullArmLength, extendSpeed * Time.deltaTime);
            armExtended = Mathf.Abs(currentArmLength - fullArmLength) < 0.01f;

            if (armExtended)
                TryDrop();
        }
        else
        {
            // Retract while swinging
            currentArmLength = Mathf.MoveTowards(currentArmLength, fullArmLength * retractedPercent, extendSpeed * Time.deltaTime);
            armExtended = false;
        }
    }

    // Apply both rotation and length to the arm
    if (arm != null)
    {
        arm.localRotation = Quaternion.Euler(0f, armAngle, 0f);

        // Scale the arm on Z (length) while keeping thickness
        Vector3 scale = arm.localScale;
        scale.z = currentArmLength / fullArmLength;
        arm.localScale = scale;

        // Keep the held package at the tip
        if (heldVisual != null)
        {
            heldVisual.transform.localPosition = new Vector3(0f, -0.05f, currentArmLength * 0.95f);
        }
    }
}
    
    /// <summary>
    /// Recalculates the resting angle (source) and drop angle (destination)
    /// from the currently linked ports.
    /// </summary>
    private void UpdateArmTargetAngles()
    {
        if (sourcePort != null)
            restAngle = GetAngleTo(sourcePort.transform.position);
        else
            restAngle = 0f;

        if (destPort != null)
            dropAngle = GetAngleTo(destPort.transform.position);
        else
            dropAngle = restAngle + 180f;   // fallback
    }

    private float GetAngleTo(Vector3 worldPos)
    {
        Vector3 localDir = transform.InverseTransformPoint(worldPos);
        localDir.y = 0f;
        return Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
    }

    // -------------------------------------------------
    // Arrow visuals
    // -------------------------------------------------
    private void UpdateArrowVisuals()
    {
        bool hasSource = sourcePort != null;
        bool hasDest   = destPort != null;
        bool linked    = hasSource && hasDest;

        Color targetColor = ColorUnlinked;

        if (linked)
        {
            // Check if destination can currently accept items
            bool destReady = destPort.CanAcceptItem();
            targetColor = destReady ? ColorLinked : ColorBlocked;

            // Point arrows from source toward destination
            AlignArrowsToDirection(sourcePort.transform.position, destPort.transform.position);
        }
        else
        {
            // Reset to default orientation when unlinked (optional)
            // AlignArrowsToDirection(transform.position + transform.forward, transform.position - transform.forward);
        }

        foreach (var mr in arrowRenderers)
        {
            if (mr != null)
                mr.material.color = targetColor;
        }
    }

    private void AlignArrowsToDirection(Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Arrow") || child.name.Contains("SideArrow"))
            {
                // Keep the original Z tilt (90°) that makes the arrow flat/readable
                Vector3 euler = look.eulerAngles;
                child.rotation = Quaternion.Euler(0f, euler.y, 90f);
            }
        }
    }

    // -------------------------------------------------
    // Linking API (with cardinal + distance checks)
    // -------------------------------------------------
    public bool LinkSource(ConnectionPoint port)
    {
        if (port == null) { sourcePort = null; return true; }

        if (port.type == ConnectionType.Input)
        {
            Debug.LogWarning($"Cannot link Input-only port as source on {name}");
            return false;
        }

        float dist = Vector3.Distance(transform.position, port.transform.position);
        if (dist > maxLinkDistance)
        {
            Debug.LogWarning($"Port too far ({dist:F2}) for {name}");
            return false;
        }

        if (!IsCardinal(port))
        {
            Debug.LogWarning($"Port not cardinal from {name}");
            return false;
        }

        sourcePort = port;
        return true;
    }

    public bool LinkDestination(ConnectionPoint port)
    {
        if (port == null) { destPort = null; return true; }

        if (port.type == ConnectionType.Output)
        {
            Debug.LogWarning($"Cannot link Output-only port as destination on {name}");
            return false;
        }

        float dist = Vector3.Distance(transform.position, port.transform.position);
        if (dist > maxLinkDistance)
        {
            Debug.LogWarning($"Port too far ({dist:F2}) for {name}");
            return false;
        }

        if (!IsCardinal(port))
        {
            Debug.LogWarning($"Port not cardinal from {name}");
            return false;
        }

        destPort = port;
        return true;
    }

    private bool IsCardinal(ConnectionPoint port)
    {
        Vector3 delta = port.transform.position - transform.position;
        delta.y = 0f;
        float absX = Mathf.Abs(delta.x);
        float absZ = Mathf.Abs(delta.z);
        return (absX > 0.3f && absZ < 0.3f) || (absZ > 0.3f && absX < 0.3f);
    }

    private void TryPickup()
    {
        if (sourcePort == null || !sourcePort.CanProvideItem()) return;

        if (sourcePort.TryTakeItem(out ItemStack taken, out Package visual))
        {
            heldStack = taken;
            heldVisual = visual;
            holdingItem = true;
            timer = cooldown;

            if (heldVisual != null && arm != null)
            {
                heldVisual.transform.SetParent(arm, worldPositionStays: true);
                heldVisual.transform.localPosition = new Vector3(0f, -0.05f, currentArmLength * 0.95f);
                heldVisual.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void TryDrop()
    {
        if (destPort == null || heldStack == null) return;

        // Pass the existing visual to the destination
        if (destPort.TryAddItem(heldStack.item, heldStack.amount, heldVisual))
        {
            // Destination now owns the visual – we just clear our references
            heldVisual = null;
            heldStack = null;
            holdingItem = false;
        }
    }
}