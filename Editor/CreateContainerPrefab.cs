#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ContainerPrefabCreator
{
    [MenuItem("Automation/Create Container Prefabs")]
    public static void CreateContainerPrefabs()
    {
        PrefabBuildUtility.BeginBuild(PathingConfig.ContainerFolder);

        var shell  = VisualsUtility.GetOrCreateMaterial("ChestShell",  ContainerConfig.ShellColor);
        var recess = VisualsUtility.GetOrCreateMaterial("ChestRecess", ContainerConfig.RecessColor);
        var top    = VisualsUtility.GetOrCreateMaterial("ChestTop",    ContainerConfig.TopColor);

        for (int i = 0; i < ContainerConfig.ChestSizes.Length; i++)
        {
            Vector2Int ports = ContainerConfig.ChestSizes[i];
            CreateChestPrefab(ports.x, ports.y, shell, recess, top);
        }

        PrefabBuildUtility.FinishBuild(
            $"Container prefabs created: {ContainerConfig.ChestSizes.Length} chest sizes.");
    }

    static Vector2 WorldSize(Vector2Int footprint)
    {
        float trim = CoreConfig.DistanceFromTileEdge * 2f;
        return new Vector2(
            footprint.x * CoreConfig.TileSize - trim,
            footprint.y * CoreConfig.TileSize - trim);
    }

    static void CreateChestPrefab(int portsX, int portsZ, Material shell, Material recess, Material top)
    {
        string name = $"Chest_{portsX}x{portsZ}";
        Vector2Int footprint = ContainerConfig.GetFootprint(portsX, portsZ);
        Vector2 size = WorldSize(footprint);

        Mesh mesh = PrefabBuildUtility.WriteMesh(
            ContainerConfig.MeshPath(portsX, portsZ),
            CreatePortedChestMesh(portsX, portsZ));

        var root = PrefabBuildUtility.CreateRoot(name, mesh, shell, recess, top);
        PrefabBuildUtility.AddBoxCollider(
            root,
            new Vector3(size.x, ContainerConfig.ChestHeight, size.y),
            new Vector3(0f, ContainerConfig.ChestHeight * 0.5f, 0f));

        var container = root.AddComponent<Container>();
        container.slotCount  = ContainerConfig.SlotCountForPorts(portsX, portsZ);
        container.footprintX = footprint.x;
        container.footprintZ = footprint.y;
        container.ports = CreateConnectionPoints(root.transform, size.x, size.y, portsX, portsZ);

        root.AddComponent<Placeable>();

        PrefabBuildUtility.SavePrefab(root, ContainerConfig.PrefabPath(portsX, portsZ));
    }

    static ConnectionPoint[] CreateConnectionPoints(
        Transform parent, float sizeX, float sizeZ, int portsX, int portsZ)
    {
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;
        float y = ContainerConfig.PortBottom + ContainerConfig.PortHeight * 0.5f;

        var list = new List<ConnectionPoint>();
        AddSide(list, parent, "Front", new Vector3( 0f, 0f,  1f), halfZ, PortOffsets(portsX), y);
        AddSide(list, parent, "Back",  new Vector3( 0f, 0f, -1f), halfZ, PortOffsets(portsX), y);
        AddSide(list, parent, "Right", new Vector3( 1f, 0f,  0f), halfX, PortOffsets(portsZ), y);
        AddSide(list, parent, "Left",  new Vector3(-1f, 0f,  0f), halfX, PortOffsets(portsZ), y);
        return list.ToArray();
    }

    static float[] PortOffsets(int portCount)
    {
        var offsets = new float[portCount];
        float start = -((portCount - 1) * 0.5f) * CoreConfig.TileSize;
        for (int i = 0; i < portCount; i++)
            offsets[i] = start + i * CoreConfig.TileSize;
        return offsets;
    }

    static void AddSide(
        List<ConnectionPoint> list, Transform parent, string sideName,
        Vector3 dir, float halfAlongDir, float[] offsets, float y)
    {
        Vector3 right = Vector3.Cross(Vector3.up, dir);

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 pos = dir * (halfAlongDir - ContainerConfig.PortInset * 0.5f)
                        + right * offsets[i]
                        + Vector3.up * y;

            var t = PrefabBuildUtility.AddChild(parent, $"Port_{sideName}_{i + 1}", pos);
            var cp = t.gameObject.AddComponent<ConnectionPoint>();
            cp.kind = ConnectionType.Both;
            cp.size = ContainerConfig.PortSize;
            list.Add(cp);
        }
    }

    static List<float> GetEdgePositions(float sideLength, float portWidth, int portCount)
    {
        var positions = new List<float>();
        float halfSlot = portWidth * 0.5f;
        var centres = new List<float>(portCount);
        float mid = sideLength * 0.5f;
        float start = -((portCount - 1) * 0.5f) * CoreConfig.TileSize;
        for (int i = 0; i < portCount; i++)
            centres.Add(mid + start + i * CoreConfig.TileSize);

        positions.Add(0f);
        for (int i = 0; i < portCount; i++)
        {
            float centre = centres[i];
            positions.Add(centre - halfSlot);
            positions.Add(centre + halfSlot);
            if (i < portCount - 1)
            {
                float nextStart = centres[i + 1] - halfSlot;
                positions.Add((centre + halfSlot + nextStart) * 0.5f);
            }
        }
        positions.Add(sideLength);
        return positions;
    }

    static Mesh CreatePortedChestMesh(int portsX, int portsZ)
    {
        float portWidth  = ContainerConfig.PortWidth;
        float portHeight = ContainerConfig.PortHeight;
        Vector2Int footprint = ContainerConfig.GetFootprint(portsX, portsZ);
        Vector2 size = WorldSize(footprint);
        float sizeX = size.x;
        float sizeZ = size.y;

        float maxAllowedX = (sizeX - (portsX + 1) * ContainerConfig.PortPadding) / Mathf.Max(1, portsX);
        float maxAllowedZ = (sizeZ - (portsZ + 1) * ContainerConfig.PortPadding) / Mathf.Max(1, portsZ);
        if (portWidth > maxAllowedX + 0.001f || portWidth > maxAllowedZ + 0.001f)
        {
            Debug.LogError($"Ports do not fit inside {sizeX}×{sizeZ} ({portsX}×{portsZ}).");
            return new Mesh();
        }

        float halfX  = sizeX * 0.5f;
        float halfZ  = sizeZ * 0.5f;
        float height = ContainerConfig.ChestHeight;
        float inset  = ContainerConfig.PortInset;
        float portBottom = ContainerConfig.PortBottom;
        float portTop    = portBottom + portHeight;

        var edgesX = GetEdgePositions(sizeX, portWidth, portsX);
        var edgesZ = GetEdgePositions(sizeZ, portWidth, portsZ);

        var verts  = new List<Vector3>();
        var shell  = new List<int>();
        var recess = new List<int>();
        var top    = new List<int>();

        int Add(Vector3 v)
        {
            verts.Add(v);
            return verts.Count - 1;
        }

        void Band(List<float> edges, float y0, float y1, System.Func<float, float, Vector3> pos, bool skipOpenings)
        {
            for (int e = 0; e < edges.Count - 1; e++)
            {
                if (skipOpenings && (e % 3 == 1)) continue;
                float a = edges[e];
                float b = edges[e + 1];
                int bl = Add(pos(a, y0));
                int br = Add(pos(b, y0));
                int tl = Add(pos(a, y1));
                int tr = Add(pos(b, y1));
                AddQuad(shell, bl, br, tl, tr);
            }
        }

        Band(edgesX, 0f, portBottom, (e, y) => new Vector3( e - halfX, y, -halfZ), false);
        Band(edgesZ, 0f, portBottom, (e, y) => new Vector3( halfX,     y,  e - halfZ), false);
        Band(edgesX, 0f, portBottom, (e, y) => new Vector3(-(e - halfX), y,  halfZ), false);
        Band(edgesZ, 0f, portBottom, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)), false);

        Band(edgesX, portBottom, portTop, (e, y) => new Vector3( e - halfX, y, -halfZ), true);
        Band(edgesZ, portBottom, portTop, (e, y) => new Vector3( halfX,     y,  e - halfZ), true);
        Band(edgesX, portBottom, portTop, (e, y) => new Vector3(-(e - halfX), y,  halfZ), true);
        Band(edgesZ, portBottom, portTop, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)), true);

        void RecessBand(List<float> edges, int portCount,
            System.Func<float, float, Vector3> outerPos,
            System.Func<float, float, Vector3> innerPos)
        {
            for (int p = 0; p < portCount; p++)
            {
                int startIdx = 1 + p * 3;
                int endIdx   = startIdx + 1;
                if (endIdx >= edges.Count) continue;

                float start = edges[startIdx];
                float end   = edges[endIdx];

                int oBl = Add(outerPos(start, portBottom));
                int oBr = Add(outerPos(end,   portBottom));
                int oTl = Add(outerPos(start, portTop));
                int oTr = Add(outerPos(end,   portTop));
                int iBl = Add(innerPos(start, portBottom));
                int iBr = Add(innerPos(end,   portBottom));
                int iTl = Add(innerPos(start, portTop));
                int iTr = Add(innerPos(end,   portTop));

                AddQuad(recess, oBl, oBr, iBl, iBr);
                AddQuad(recess, iTl, iTr, oTl, oTr);
                AddQuad(recess, oBl, iBl, oTl, iTl);
                AddQuad(recess, iBr, oBr, iTr, oTr);
                AddQuad(recess, iBl, iBr, iTl, iTr);
            }
        }

        RecessBand(edgesX, portsX,
            (e, y) => new Vector3(e - halfX, y, -halfZ),
            (e, y) => new Vector3(e - halfX, y, -halfZ + inset));
        RecessBand(edgesZ, portsZ,
            (e, y) => new Vector3(halfX, y, e - halfZ),
            (e, y) => new Vector3(halfX - inset, y, e - halfZ));
        RecessBand(edgesX, portsX,
            (e, y) => new Vector3(-(e - halfX), y, halfZ),
            (e, y) => new Vector3(-(e - halfX), y, halfZ - inset));
        RecessBand(edgesZ, portsZ,
            (e, y) => new Vector3(-halfX, y, -(e - halfZ)),
            (e, y) => new Vector3(-halfX + inset, y, -(e - halfZ)));

        Band(edgesX, portTop, height, (e, y) => new Vector3( e - halfX, y, -halfZ), true);
        Band(edgesZ, portTop, height, (e, y) => new Vector3( halfX,     y,  e - halfZ), true);
        Band(edgesX, portTop, height, (e, y) => new Vector3(-(e - halfX), y,  halfZ), true);
        Band(edgesZ, portTop, height, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)), true);

        void Above(List<float> edges, int portCount, System.Func<float, float, Vector3> pos)
        {
            for (int p = 0; p < portCount; p++)
            {
                int startIdx = 1 + p * 3;
                int endIdx   = startIdx + 1;
                if (endIdx >= edges.Count) continue;
                int bl = Add(pos(edges[startIdx], portTop));
                int br = Add(pos(edges[endIdx],   portTop));
                int tl = Add(pos(edges[startIdx], height));
                int tr = Add(pos(edges[endIdx],   height));
                AddQuad(shell, bl, br, tl, tr);
            }
        }

        Above(edgesX, portsX, (e, y) => new Vector3( e - halfX, y, -halfZ));
        Above(edgesZ, portsZ, (e, y) => new Vector3( halfX,     y,  e - halfZ));
        Above(edgesX, portsX, (e, y) => new Vector3(-(e - halfX), y,  halfZ));
        Above(edgesZ, portsZ, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)));

        {
            int bl = Add(new Vector3(-halfX, 0f, -halfZ));
            int br = Add(new Vector3( halfX, 0f, -halfZ));
            int tl = Add(new Vector3(-halfX, 0f,  halfZ));
            int tr = Add(new Vector3( halfX, 0f,  halfZ));
            AddQuad(shell, bl, tl, br, tr);
        }
        {
            int bl = Add(new Vector3(-halfX, height, -halfZ));
            int br = Add(new Vector3( halfX, height, -halfZ));
            int tl = Add(new Vector3(-halfX, height,  halfZ));
            int tr = Add(new Vector3( halfX, height,  halfZ));
            AddQuad(top, bl, br, tl, tr);
        }

        return VisualsUtility.FinishSubmeshes(
            $"PortedChestMesh_{portsX}x{portsZ}", verts, shell, recess, top);
    }

    static void AddQuad(List<int> tris, int bl, int br, int tl, int tr)
    {
        tris.Add(bl); tris.Add(tl); tris.Add(tr);
        tris.Add(bl); tris.Add(tr); tris.Add(br);
    }
}
#endif