using UnityEngine;
using System.Collections.Generic;

public class Inserter : MonoBehaviour
{
    private enum Phase
    {
        Idle,
        SlewToPickup,
        ExtendTelescopePickup,
        ExtendCablePickup,
        Grab,
        RetractCable,
        RetractTelescope,
        SlewToDrop,
        ExtendTelescopeDrop,
        ExtendCableDrop,
        Drop,
        RetractCableAfterDrop,
        RetractTelescopeAfterDrop,
        SlewHomeToPickup,
        ExtendTelescopeHome,
        ExtendCableHome
    }

    [Header("Linked Ports")]
    public ConnectionPoint sourcePort;
    public ConnectionPoint destPort;

    private Phase phase = Phase.Idle;
    private float timer;

    private float armAngle;
    private float currentReach;
    private float currentCableLength;

    private bool holdingItem;
    private ItemStack heldStack;
    private Package heldVisual;

    private Transform slew, arm, telescope, tip, cable, hook, magnet;

    private readonly List<MeshRenderer> arrowRenderers = new List<MeshRenderer>();

    void Awake()
    {
        slew      = transform.Find("Slew");
        arm       = transform.Find("Slew/Arm");
        telescope = transform.Find("Slew/Arm/Telescope");
        tip       = transform.Find("Slew/Arm/Telescope/Tip");
        cable     = transform.Find("Slew/Arm/Telescope/Tip/Cable");
        hook      = transform.Find("Slew/Arm/Telescope/Tip/Hook");
        magnet    = transform.Find("Slew/Arm/Telescope/Tip/Hook/Magnet");

        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.name.Contains("Arrow") || mr.transform.name.Contains("Arrow"))
                arrowRenderers.Add(mr);
        }

        currentReach = InserterConfig.MinReach;
        currentCableLength = InserterConfig.CableMinLength;
        ApplyTransforms();
    }

    void Start()
    {
        if (sourcePort != null)
            phase = Phase.SlewHomeToPickup;
    }

    void Update()
    {
        if (ConveyorManager.Instance == null || !ConveyorManager.Instance.isRunning)
            return;

        UpdateArrowVisuals();
        timer -= Time.deltaTime;

        switch (phase)
        {
            case Phase.Idle:
                if (CanStartCycle())
                    phase = Phase.Grab;
                break;

            case Phase.SlewToPickup:
                if (SlewToward(sourcePort))
                    phase = Phase.ExtendTelescopePickup;
                break;

            case Phase.ExtendTelescopePickup:
                if (MoveReachToward(GetReachTo(sourcePort)))
                    phase = Phase.ExtendCablePickup;
                break;

            case Phase.ExtendCablePickup:
                if (MoveCableToward(PickupCableLength()))
                    phase = Phase.Grab;
                break;

            case Phase.Grab:
                if (timer > 0f) break;
                if (TryGrab())
                    phase = Phase.RetractCable;
                else
                    phase = Phase.Idle;
                break;

            case Phase.RetractCable:
                if (MoveCableToward(InserterConfig.CableMinLength))
                    phase = Phase.RetractTelescope;
                break;

            case Phase.RetractTelescope:
                if (MoveReachToward(InserterConfig.MinReach))
                    phase = Phase.SlewToDrop;
                break;

            case Phase.SlewToDrop:
                if (SlewToward(destPort))
                    phase = Phase.ExtendTelescopeDrop;
                break;

            case Phase.ExtendTelescopeDrop:
                if (MoveReachToward(GetReachTo(destPort)))
                    phase = Phase.ExtendCableDrop;
                break;

            case Phase.ExtendCableDrop:
                if (MoveCableToward(DropCableLength()))
                    phase = Phase.Drop;
                break;

            case Phase.Drop:
                if (TryRelease())
                {
                    timer = InserterConfig.Cooldown;
                    phase = Phase.RetractCableAfterDrop;
                }
                break;

            case Phase.RetractCableAfterDrop:
                if (MoveCableToward(InserterConfig.CableMinLength))
                    phase = Phase.RetractTelescopeAfterDrop;
                break;

            case Phase.RetractTelescopeAfterDrop:
                if (MoveReachToward(InserterConfig.MinReach))
                    phase = Phase.SlewHomeToPickup;
                break;

            case Phase.SlewHomeToPickup:
                if (SlewToward(sourcePort))
                    phase = Phase.ExtendTelescopeHome;
                break;

            case Phase.ExtendTelescopeHome:
                if (MoveReachToward(GetReachTo(sourcePort)))
                    phase = Phase.ExtendCableHome;
                break;

            case Phase.ExtendCableHome:
                if (MoveCableToward(PickupCableLength()))
                    phase = Phase.Idle;
                break;
        }

        ApplyTransforms();
    }

    // -------------------------------------------------
    // Movement primitives
    // -------------------------------------------------
    private bool SlewToward(ConnectionPoint port)
    {
        float target = port != null ? GetAngleTo(port.transform.position) : armAngle;
        armAngle = Mathf.MoveTowardsAngle(armAngle, target, InserterConfig.SwingSpeed * Time.deltaTime);
        return Mathf.Abs(Mathf.DeltaAngle(armAngle, target)) < InserterConfig.AngleTolerance;
    }

    private bool MoveReachToward(float target)
    {
        currentReach = Mathf.MoveTowards(currentReach, target, InserterConfig.ExtendSpeed * Time.deltaTime);
        return Mathf.Abs(currentReach - target) < InserterConfig.PosTolerance;
    }

    private bool MoveCableToward(float target)
    {
        currentCableLength = Mathf.MoveTowards(currentCableLength, target, InserterConfig.CableSpeed * Time.deltaTime);
        return Mathf.Abs(currentCableLength - target) < InserterConfig.PosTolerance;
    }

    private float GetReachTo(ConnectionPoint port)
    {
        if (port == null) return InserterConfig.MinReach;

        Vector3 origin = slew != null ? slew.position : transform.position;
        Vector3 delta = port.transform.position - origin;
        delta.y = 0f;
        return Mathf.Clamp(delta.magnitude, InserterConfig.MinReach, InserterConfig.FullArmLength);
    }

    private float PickupCableLength()
    {
        float tipY = tip != null ? tip.position.y
            : (slew != null ? slew.position.y : transform.position.y + 0.7f);
        float packageTopY = sourcePort != null
            ? sourcePort.transform.position.y + InserterConfig.PackageHeight * 0.5f
            : InserterConfig.PackageHeight;
        float needed = tipY - packageTopY - InserterConfig.PackageClearance;
        return Mathf.Clamp(needed, InserterConfig.CableMinLength, InserterConfig.CableLength);
    }

    private float DropCableLength()
    {
        float tipY = tip != null ? tip.position.y
            : (slew != null ? slew.position.y : transform.position.y + 0.7f);
        float dropY = destPort != null
            ? destPort.transform.position.y + InserterConfig.PackageHeight * 0.5f
            : InserterConfig.PackageHeight;
        float needed = tipY - dropY - InserterConfig.PackageClearance;
        return Mathf.Clamp(needed, InserterConfig.CableMinLength, InserterConfig.CableLength);
    }

    private float GetAngleTo(Vector3 worldPos)
    {
        Vector3 localDir = transform.InverseTransformPoint(worldPos);
        localDir.y = 0f;
        return Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
    }

    // -------------------------------------------------
    // Apply visuals
    // -------------------------------------------------
    private void ApplyTransforms()
    {
        if (slew != null)
            slew.localRotation = Quaternion.Euler(0f, armAngle, 0f);

        if (telescope != null)
        {
            float z = Mathf.Max(InserterConfig.TelescopeMinZ, currentReach - InserterConfig.InnerArmLength);
            telescope.localPosition = new Vector3(0f, 0f, z);
        }

        if (cable != null)
        {
            float scaleY = currentCableLength / Mathf.Max(0.001f, InserterConfig.CableLength);
            cable.localScale = new Vector3(1f, scaleY, 1f);
            cable.localPosition = Vector3.zero;
        }

        if (magnet != null)
            magnet.localPosition = new Vector3(0f, -currentCableLength, 0f);

        if (heldVisual != null && magnet != null)
        {
            heldVisual.transform.localPosition = new Vector3(0f, InserterConfig.PackageHoldOffsetY, 0f);
            heldVisual.transform.localScale = Vector3.one;
        }
    }

    // -------------------------------------------------
    // Grab / drop
    // -------------------------------------------------
    private bool CanStartCycle()
    {
        if (timer > 0f) return false;
        if (sourcePort == null || destPort == null) return false;
        if (!sourcePort.CanProvideItem()) return false;
        if (!destPort.CanAcceptItem()) return false;
        return true;
    }

    private bool TryGrab()
    {
        if (sourcePort == null || !sourcePort.CanProvideItem()) return false;

        if (sourcePort.TryTakeItem(out ItemStack taken, out Package visual))
        {
            heldStack = taken;
            heldVisual = visual;
            holdingItem = true;

            if (heldVisual != null && magnet != null)
            {
                heldVisual.transform.SetParent(magnet, worldPositionStays: true);
                heldVisual.transform.localPosition = new Vector3(0f, InserterConfig.PackageHoldOffsetY, 0f);
                heldVisual.transform.localRotation = Quaternion.identity;
                heldVisual.transform.localScale = Vector3.one;
            }
            return true;
        }
        return false;
    }

    private bool TryRelease()
    {
        if (destPort == null || heldStack == null) return false;

        if (destPort.TryAddItem(heldStack.item, heldStack.amount, heldVisual))
        {
            heldVisual = null;
            heldStack = null;
            holdingItem = false;
            return true;
        }
        return false;
    }

    // -------------------------------------------------
    // Linking
    // -------------------------------------------------
    public bool LinkSource(ConnectionPoint port) =>
        TryLinkPort(port, ConnectionType.Input, ref sourcePort, "source");

    public bool LinkDestination(ConnectionPoint port) =>
        TryLinkPort(port, ConnectionType.Output, ref destPort, "destination");

    private bool TryLinkPort(ConnectionPoint port, ConnectionType forbiddenType,
                             ref ConnectionPoint targetField, string role)
    {
        if (port == null)
        {
            targetField = null;
            return true;
        }

        if (port.type == forbiddenType)
        {
            Debug.LogWarning($"Cannot link {forbiddenType}-only port as {role} on {name}");
            return false;
        }

        float dist = Vector3.Distance(transform.position, port.transform.position);
        if (dist > InserterConfig.MaxLinkDistance)
        {
            Debug.LogWarning($"Port too far ({dist:F2}) for {name}");
            return false;
        }

        if (!IsCardinal(port))
        {
            Debug.LogWarning($"Port not cardinal from {name}");
            return false;
        }

        targetField = port;
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

    // -------------------------------------------------
    // Arrows
    // -------------------------------------------------
    private void UpdateArrowVisuals()
    {
        bool linked = sourcePort != null && destPort != null;
        Color c = InserterConfig.ArrowUnlinked;

        if (linked)
        {
            c = destPort.CanAcceptItem()
                ? InserterConfig.ArrowLinked
                : InserterConfig.ArrowBlocked;
            AlignArrowsToDirection(sourcePort.transform.position, destPort.transform.position);
        }

        foreach (var mr in arrowRenderers)
        {
            if (mr != null)
                mr.material.color = c;
        }
    }

    private void AlignArrowsToDirection(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        float y = Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y;
        foreach (var t in GetComponentsInChildren<Transform>())
        {
            if (t.name.Contains("Arrow"))
                t.rotation = Quaternion.Euler(0f, y, 90f);
        }
    }
}