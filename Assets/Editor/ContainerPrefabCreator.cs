#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ContainerPrefabCreator
{
    public static Vector2Int GetFootprint(int portsX, int portsZ) =>
        ContainerConfig.GetFootprint(portsX, portsZ);

    [MenuItem("Automation/Create Container Prefabs")]
    public static void CreateContainerPrefabs()
    {
        LogisticsPrefabUtility.EnsureAllFolders();

        Material shellMat = LogisticsPrefabUtility.GetOrCreateMaterial(
            "ChestShell", ContainerConfig.ShellColor);
        Material recessMat = LogisticsPrefabUtility.GetOrCreateMaterial(
            "ChestRecess", ContainerConfig.RecessColor);
        Material topMat = LogisticsPrefabUtility.GetOrCreateMaterial(
            "ChestTop", ContainerConfig.TopColor);

        foreach (var (portsX, portsZ) in ContainerConfig.ChestSizes)
        {
            string name = $"Chest_{portsX}x{portsZ}";
            Vector2Int footprint = GetFootprint(portsX, portsZ);

            LogisticsPrefabUtility.DeleteIfExists(
                $"{LogisticsConfig.ContainerFolder}{name}.prefab");
            LogisticsPrefabUtility.DeleteIfExists(
                $"{LogisticsConfig.ContainerFolder}{name}_Mesh.asset");

            Mesh mesh = CreatePortedChestMesh(portsX, portsZ);
            AssetDatabase.CreateAsset(
                mesh, $"{LogisticsConfig.ContainerFolder}{name}_Mesh.asset");

            CreateChestPrefab(name, mesh, shellMat, recessMat, topMat, portsX, portsZ, footprint);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Container prefabs created: {ContainerConfig.ChestSizes.Length} chest sizes.");
    }

    // ------------------------------------------------------------------
    // Prefab
    // ------------------------------------------------------------------
    private static void CreateChestPrefab(
        string name,
        Mesh mesh,
        Material shellMat,
        Material recessMat,
        Material topMat,
        int portsX,
        int portsZ,
        Vector2Int footprint)
    {
        string path = $"{LogisticsConfig.ContainerFolder}{name}.prefab";

        GameObject root = new GameObject(name);

        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = root.AddComponent<MeshRenderer>();
        // Submesh 0 = shell, 1 = recess, 2 = top
        mr.sharedMaterials = new Material[] { shellMat, recessMat, topMat };

        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(footprint.x, ContainerConfig.ChestHeight, footprint.y);
        col.center = new Vector3(0f, ContainerConfig.ChestHeight * 0.5f, 0f);

        var container = root.AddComponent<Container>();
        container.slotCount = ContainerConfig.SlotCountForPorts(portsX, portsZ);
        container.footprintX = footprint.x;
        container.footprintZ = footprint.y;

        CreateConnectionPoints(root, container, footprint.x, footprint.y, portsX, portsZ);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ------------------------------------------------------------------
    // Connection points
    // ------------------------------------------------------------------
    private static void CreateConnectionPoints(
        GameObject parent,
        Container container,
        float sizeX,
        float sizeZ,
        int portsX,
        int portsZ)
    {
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;
        float y = ContainerConfig.PortBottom;

        // Front / Back → portsX    Left / Right → portsZ
        CreatePortsOnSide(parent, container, "Front",
            new Vector3(0, 0, 1), halfZ, GetPortOffsets(portsX), portsX, y);
        CreatePortsOnSide(parent, container, "Back",
            new Vector3(0, 0, -1), halfZ, GetPortOffsets(portsX), portsX, y);
        CreatePortsOnSide(parent, container, "Right",
            new Vector3(1, 0, 0), halfX, GetPortOffsets(portsZ), portsZ, y);
        CreatePortsOnSide(parent, container, "Left",
            new Vector3(-1, 0, 0), halfX, GetPortOffsets(portsZ), portsZ, y);
    }

    private static float[] GetPortOffsets(int portCount)
    {
        float[] offsets = new float[portCount];
        if (portCount % 2 == 1)
        {
            int mid = portCount / 2;
            for (int i = 0; i < portCount; i++)
                offsets[i] = (i - mid) * ContainerConfig.TileSize;
        }
        else
        {
            float start = -((portCount - 1) * 0.5f) * ContainerConfig.TileSize;
            for (int i = 0; i < portCount; i++)
                offsets[i] = start + i * ContainerConfig.TileSize;
        }
        return offsets;
    }

    private static void CreatePortsOnSide(
        GameObject parent,
        Container container,
        string sideName,
        Vector3 dir,
        float halfAlongDir,
        float[] offsets,
        int portCount,
        float y)
    {
        Vector3 right = Vector3.Cross(Vector3.up, dir);

        for (int i = 0; i < portCount; i++)
        {
            Vector3 pos = dir * (halfAlongDir - ContainerConfig.PortSurfaceOffset)
                        + right * offsets[i]
                        + Vector3.up * y;

            GameObject cpGO = new GameObject($"Port_{sideName}_{i + 1}");
            cpGO.transform.SetParent(parent.transform, false);
            cpGO.transform.localPosition = pos;

            var cp = cpGO.AddComponent<ConnectionPoint>();
            cp.type = ConnectionType.Both;
            cp.owner = container;
            cp.radius = ContainerConfig.DefaultPortRadius;
        }
    }

    // ------------------------------------------------------------------
    // Edge positions (tile-centred ports)
    // ------------------------------------------------------------------
    private static List<float> GetEdgePositions(float sideLength, float portWidth, int portCount)
    {
        List<float> positions = new List<float>();
        float halfSlot = portWidth * 0.5f;

        List<float> portCentres = new List<float>();
        if (portCount % 2 == 1)
        {
            float mid = sideLength * 0.5f;
            int midIndex = portCount / 2;
            for (int i = 0; i < portCount; i++)
                portCentres.Add(mid + (i - midIndex) * ContainerConfig.TileSize);
        }
        else
        {
            float offset = -((portCount - 1) * 0.5f) * ContainerConfig.TileSize;
            for (int i = 0; i < portCount; i++)
                portCentres.Add(sideLength * 0.5f + offset + i * ContainerConfig.TileSize);
        }

        positions.Add(0f);

        for (int i = 0; i < portCount; i++)
        {
            float centre = portCentres[i];
            positions.Add(centre - halfSlot);
            positions.Add(centre + halfSlot);

            if (i < portCount - 1)
            {
                float nextStart = portCentres[i + 1] - halfSlot;
                positions.Add((centre + halfSlot + nextStart) * 0.5f);
            }
        }

        positions.Add(sideLength);
        return positions;
    }

    // ------------------------------------------------------------------
    // Mesh
    // ------------------------------------------------------------------
    private static Mesh CreatePortedChestMesh(int portsX, int portsZ)
    {
        float portWidth  = ContainerConfig.PortWidth;
        float portHeight = ContainerConfig.PortHeight;

        Vector2Int footprint = GetFootprint(portsX, portsZ);
        float sizeX = footprint.x;
        float sizeZ = footprint.y;

        float maxAllowedX = (sizeX - (portsX + 1) * ContainerConfig.PortPadding) / Mathf.Max(1, portsX);
        float maxAllowedZ = (sizeZ - (portsZ + 1) * ContainerConfig.PortPadding) / Mathf.Max(1, portsZ);

        if (portWidth > maxAllowedX + 0.001f || portWidth > maxAllowedZ + 0.001f)
        {
            Debug.LogError(
                $"CreatePortedChestMesh: Ports do not fit inside {sizeX}×{sizeZ} tiles.\n" +
                $"Ports: {portsX}×{portsZ}, PortWidth: {portWidth:F3}");
            return new Mesh();
        }

        float halfX  = sizeX * 0.5f;
        float halfZ  = sizeZ * 0.5f;
        float height = ContainerConfig.ChestHeight;
        float inset  = ContainerConfig.PortInset;

        float portBottom = ContainerConfig.PortBottom;
        float portTop    = portBottom + portHeight;

        List<float> edgesX = GetEdgePositions(sizeX, portWidth, portsX);
        List<float> edgesZ = GetEdgePositions(sizeZ, portWidth, portsZ);

        List<Vector3> vertices   = new List<Vector3>();
        List<int>     shellTris  = new List<int>();
        List<int>     recessTris = new List<int>();
        List<int>     topTris    = new List<int>();

        void AddQuad(List<int> tris, int bl, int br, int tl, int tr)
        {
            tris.Add(bl); tris.Add(tl); tris.Add(tr);
            tris.Add(bl); tris.Add(tr); tris.Add(br);
        }

        int AddVertex(Vector3 v)
        {
            vertices.Add(v);
            return vertices.Count - 1;
        }

        void BuildWallBand(
            List<float> edges, float y0, float y1,
            System.Func<float, float, Vector3> posFunc,
            bool skipOpenings)
        {
            for (int e = 0; e < edges.Count - 1; e++)
            {
                if (skipOpenings && (e % 3 == 1)) continue;

                float x0 = edges[e];
                float x1 = edges[e + 1];

                int bl = AddVertex(posFunc(x0, y0));
                int br = AddVertex(posFunc(x1, y0));
                int tl = AddVertex(posFunc(x0, y1));
                int tr = AddVertex(posFunc(x1, y1));

                AddQuad(shellTris, bl, br, tl, tr);
            }
        }

        // PASS 1 – Base solid walls (0 → portBottom)
        BuildWallBand(edgesX, 0f, portBottom, (e, y) => new Vector3( e - halfX, y, -halfZ), false);
        BuildWallBand(edgesZ, 0f, portBottom, (e, y) => new Vector3( halfX,     y,  e - halfZ), false);
        BuildWallBand(edgesX, 0f, portBottom, (e, y) => new Vector3(-(e - halfX), y,  halfZ), false);
        BuildWallBand(edgesZ, 0f, portBottom, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)), false);

        // PASS 2 – Port-level walls (leave openings)
        BuildWallBand(edgesX, portBottom, portTop, (e, y) => new Vector3( e - halfX, y, -halfZ), true);
        BuildWallBand(edgesZ, portBottom, portTop, (e, y) => new Vector3( halfX,     y,  e - halfZ), true);
        BuildWallBand(edgesX, portBottom, portTop, (e, y) => new Vector3(-(e - halfX), y,  halfZ), true);
        BuildWallBand(edgesZ, portBottom, portTop, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)), true);

        // PASS 3 – Recessed port boxes
        void BuildPortRecess(
            List<float> edges, int portCount,
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

                int o_bl = AddVertex(outerPos(start, portBottom));
                int o_br = AddVertex(outerPos(end,   portBottom));
                int o_tl = AddVertex(outerPos(start, portTop));
                int o_tr = AddVertex(outerPos(end,   portTop));

                int i_bl = AddVertex(innerPos(start, portBottom));
                int i_br = AddVertex(innerPos(end,   portBottom));
                int i_tl = AddVertex(innerPos(start, portTop));
                int i_tr = AddVertex(innerPos(end,   portTop));

                AddQuad(recessTris, o_bl, o_br, i_bl, i_br);
                AddQuad(recessTris, i_tl, i_tr, o_tl, o_tr);
                AddQuad(recessTris, o_bl, i_bl, o_tl, i_tl);
                AddQuad(recessTris, i_br, o_br, i_tr, o_tr);
                AddQuad(recessTris, i_bl, i_br, i_tl, i_tr);
            }
        }

        BuildPortRecess(edgesX, portsX,
            (e, y) => new Vector3(e - halfX, y, -halfZ),
            (e, y) => new Vector3(e - halfX, y, -halfZ + inset));
        BuildPortRecess(edgesZ, portsZ,
            (e, y) => new Vector3(halfX, y, e - halfZ),
            (e, y) => new Vector3(halfX - inset, y, e - halfZ));
        BuildPortRecess(edgesX, portsX,
            (e, y) => new Vector3(-(e - halfX), y, halfZ),
            (e, y) => new Vector3(-(e - halfX), y, halfZ - inset));
        BuildPortRecess(edgesZ, portsZ,
            (e, y) => new Vector3(-halfX, y, -(e - halfZ)),
            (e, y) => new Vector3(-halfX + inset, y, -(e - halfZ)));

        // PASS 4 – Upper walls (portTop → height)
        BuildWallBand(edgesX, portTop, height, (e, y) => new Vector3( e - halfX, y, -halfZ), true);
        BuildWallBand(edgesZ, portTop, height, (e, y) => new Vector3( halfX,     y,  e - halfZ), true);
        BuildWallBand(edgesX, portTop, height, (e, y) => new Vector3(-(e - halfX), y,  halfZ), true);
        BuildWallBand(edgesZ, portTop, height, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)), true);

        void BuildAbovePortWalls(
            List<float> edges, int portCount,
            System.Func<float, float, Vector3> posFunc)
        {
            for (int p = 0; p < portCount; p++)
            {
                int startIdx = 1 + p * 3;
                int endIdx   = startIdx + 1;
                if (endIdx >= edges.Count) continue;

                float start = edges[startIdx];
                float end   = edges[endIdx];

                int bl = AddVertex(posFunc(start, portTop));
                int br = AddVertex(posFunc(end,   portTop));
                int tl = AddVertex(posFunc(start, height));
                int tr = AddVertex(posFunc(end,   height));

                AddQuad(shellTris, bl, br, tl, tr);
            }
        }

        BuildAbovePortWalls(edgesX, portsX, (e, y) => new Vector3( e - halfX, y, -halfZ));
        BuildAbovePortWalls(edgesZ, portsZ, (e, y) => new Vector3( halfX,     y,  e - halfZ));
        BuildAbovePortWalls(edgesX, portsX, (e, y) => new Vector3(-(e - halfX), y,  halfZ));
        BuildAbovePortWalls(edgesZ, portsZ, (e, y) => new Vector3(-halfX,     y, -(e - halfZ)));

        // Bottom face → shell
        {
            int bl = AddVertex(new Vector3(-halfX, 0f, -halfZ));
            int br = AddVertex(new Vector3( halfX, 0f, -halfZ));
            int tl = AddVertex(new Vector3(-halfX, 0f,  halfZ));
            int tr = AddVertex(new Vector3( halfX, 0f,  halfZ));
            AddQuad(shellTris, bl, tl, br, tr);
        }

        // Top face → top submesh
        {
            int bl = AddVertex(new Vector3(-halfX, height, -halfZ));
            int br = AddVertex(new Vector3( halfX, height, -halfZ));
            int tl = AddVertex(new Vector3(-halfX, height,  halfZ));
            int tr = AddVertex(new Vector3( halfX, height,  halfZ));
            AddQuad(topTris, bl, br, tl, tr);
        }

        Mesh mesh = new Mesh { name = $"PortedChestMesh_{portsX}x{portsZ}" };
        mesh.subMeshCount = 3;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(shellTris, 0);
        mesh.SetTriangles(recessTris, 1);
        mesh.SetTriangles(topTris, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif