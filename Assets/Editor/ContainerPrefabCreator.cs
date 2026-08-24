#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ContainerPrefabCreator
{
    // === Chest dimensions (3×3 grid, slightly smaller visually) ===
    private const float Size   = 2.7f;   // visual size 2.3 original
    private const float Height = 1.00f; //1 original
    private const float HalfSize = Size * 0.5f;
    private const float HalfHeight = Height * 0.5f;

    [MenuItem("Automation/Create Container Prefabs")]
    public static void CreateContainerPrefabs()
    {
        LogisticsPrefabUtility.EnsureAllFolders();

        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.ContainerFolder}Chest.prefab");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.ContainerFolder}ChestMesh.asset");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.MaterialFolder}Chest.mat");
        LogisticsPrefabUtility.DeleteIfExists($"{ConveyorConfig.MaterialFolder}ChestPort.mat");
        
        Material shellMat = LogisticsPrefabUtility.GetOrCreateMaterial("ChestShell", new Color(0.35f, 0.28f, 0.22f));
        Material recessMat = LogisticsPrefabUtility.GetOrCreateMaterial("ChestRecess", Color.gray);

        Mesh mesh = CreatePortedChestMesh(3, 3);
        AssetDatabase.CreateAsset(mesh, $"{ConveyorConfig.ContainerFolder}ChestMesh.asset");

        CreateChestPrefab(mesh, shellMat, recessMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Container prefabs created successfully (Chest).");
    }
    

    private static void CreateChestPrefab(Mesh mesh, Material shellMat, Material recessMat)
    {
        string path = $"{ConveyorConfig.ContainerFolder}Chest.prefab";

        GameObject root = new GameObject("Chest");

        // === Main body ===
        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = root.AddComponent<MeshRenderer>();
    
        // Assign materials according to submesh order
        // Submesh 0 = Shell (outer body)
        // Submesh 1 = Recess (ports / inset parts)
        mr.sharedMaterials = new Material[] { shellMat, recessMat };

        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(Size, Height, Size);

        // Inventory
        var container = root.AddComponent<Container>();
        container.slotCount = 4;

        // === ConnectionPoints only (no visual cubes) ===
        CreateConnectionPoints(root, container);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateConnectionPoints(GameObject parent, Container container)
    {
        float y = 0.15f;
        float[] offsets = { -1f, 0f, 1f };

        Vector3[] sideDirs = {
            new Vector3( 0, 0,  1), // Front
            new Vector3( 0, 0, -1), // Back
            new Vector3( 1, 0,  0), // Right
            new Vector3(-1, 0,  0)  // Left
        };

        string[] sideNames = { "Front", "Back", "Right", "Left" };

        for (int s = 0; s < 4; s++)
        {
            Vector3 dir = sideDirs[s];
            Vector3 right = Vector3.Cross(Vector3.up, dir);

            for (int i = 0; i < 3; i++)
            {
                // Slightly recessed position (will match the final mesh later)
                Vector3 pos = dir * (HalfSize - 0.04f)
                              + right * offsets[i]
                              + Vector3.up * y;

                GameObject cpGO = new GameObject($"Port_{sideNames[s]}_{i + 1}");
                cpGO.transform.SetParent(parent.transform, false);
                cpGO.transform.localPosition = pos;

                var cp = cpGO.AddComponent<ConnectionPoint>();
                cp.type = ConnectionType.Both;
                cp.owner = container;
                cp.radius = 0.35f;
            }
        }
    }

    /// <summary>
    /// Returns all edge positions for one side (including outer corners).
    /// Ports are centred on 1×1 tiles. Works for odd and even port counts.
    /// </summary>
    private static List<float> GetEdgePositions(float sideLength, float packageSlotSize, int portCount)
    {
        List<float> positions = new List<float>();
        float halfSlot = packageSlotSize * 0.5f;

        // Port centres (always centred on logical 1×1 tiles)
        List<float> portCentres = new List<float>();

        if (portCount % 2 == 1)
        {
            // Odd: middle port exactly at centre
            float mid = sideLength * 0.5f;
            int midIndex = portCount / 2;
            for (int i = 0; i < portCount; i++)
                portCentres.Add(mid + (i - midIndex) * 1f);
        }
        else
        {
            // Even: symmetrical around centre
            float offset = -((portCount - 1) * 0.5f);
            for (int i = 0; i < portCount; i++)
                portCentres.Add(sideLength * 0.5f + (offset + i) * 1f);
        }

        // Build full side: left corner → ports → right corner
        positions.Add(0f); // left outer edge

        for (int i = 0; i < portCount; i++)
        {
            float centre = portCentres[i];
            positions.Add(centre - halfSlot); // port start
            positions.Add(centre + halfSlot); // port end

            if (i < portCount - 1)
            {
                // Mid-gap between this port and the next
                float nextStart = portCentres[i + 1] - halfSlot;
                positions.Add((centre + halfSlot + nextStart) * 0.5f);
            }
        }

        positions.Add(sideLength); // right outer edge
        return positions;
    }

    /// <summary>
    /// Creates a ported chest mesh.
    /// You only need to specify how many ports you want on each axis.
    /// The method calculates the minimum size required so the ports always fit.
    /// </summary>
    private static Mesh CreatePortedChestMesh(int portsX, int portsZ, float minPadding = 0.15f)
    {
        float packageSlot = ConveyorConfig.PackageSize * 1.05f * 3f;

        // -------------------------------------------------
        // Size = number of ports + 1 tile
        // -------------------------------------------------
        // 2 ports → 3 tiles
        // 3 ports → 4 tiles
        // 4 ports → 5 tiles
        float sizeX = portsX + 1;
        float sizeZ = portsZ + 1;

        // -------------------------------------------------
        // Safety check – make sure the ports still fit
        // -------------------------------------------------
        float maxAllowedSlotX = (sizeX - (portsX + 1) * minPadding) / portsX;
        float maxAllowedSlotZ = (sizeZ - (portsZ + 1) * minPadding) / portsZ;

        if (packageSlot > maxAllowedSlotX + 0.001f || packageSlot > maxAllowedSlotZ + 0.001f)
        {
            Debug.LogError($"CreatePortedChestMesh: Ports do not fit inside {sizeX}×{sizeZ} tiles.\n" +
                           $"Ports: {portsX}×{portsZ}, PackageSlot: {packageSlot:F3}");
            return new Mesh();
        }

        // -------------------------------------------------
        // Continue with normal generation
        // -------------------------------------------------
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;
        float height = Height;
        float inset  = 0.15f;

        List<float> edgesX = GetEdgePositions(sizeX, packageSlot, portsX);
        List<float> edgesZ = GetEdgePositions(sizeZ, packageSlot, portsZ);

    float[] heights = { 0f, packageSlot, height };

    List<Vector3> vertices = new List<Vector3>();
    List<int> shellTris  = new List<int>();
    List<int> recessTris = new List<int>();

    void AddQuad(List<int> tris, int bl, int br, int tl, int tr)
    {
        // Correct winding for outward-facing faces
        tris.Add(bl); tris.Add(tl); tris.Add(tr);
        tris.Add(bl); tris.Add(tr); tris.Add(br);
    }

    int AddVertex(Vector3 v)
    {
        vertices.Add(v);
        return vertices.Count - 1;
    }

    // -------------------------------------------------
    // PASS 1 – Solid outer walls (skip port openings)
    // -------------------------------------------------
    void BuildSolidWall(List<float> edges, System.Func<float, float, Vector3> posFunc)
    {
        // For each height band (bottom→mid, mid→top)
        for (int h = 0; h < 2; h++)
        {
            float y0 = heights[h];
            float y1 = heights[h + 1];

            for (int e = 0; e < edges.Count - 1; e++)
            {
                // Skip the port openings (the segment between start and end of a port)
                bool isOpening = (e % 3 == 1);
                if (isOpening) continue;

                float x0 = edges[e];
                float x1 = edges[e + 1];

                int bl = AddVertex(posFunc(x0, y0));
                int br = AddVertex(posFunc(x1, y0));
                int tl = AddVertex(posFunc(x0, y1));
                int tr = AddVertex(posFunc(x1, y1));

                AddQuad(shellTris, bl, br, tl, tr);
            }
        }
    }

    // Front (-Z)
    BuildSolidWall(edgesX, (e, y) => new Vector3(e - halfX, y, -halfZ));

// Right (+X)
    BuildSolidWall(edgesZ, (e, y) => new Vector3(halfX, y, e - halfZ));

// Back (+Z)
    BuildSolidWall(edgesX, (e, y) => new Vector3(-(e - halfX), y, halfZ));

// Left (-X)
    BuildSolidWall(edgesZ, (e, y) => new Vector3(-halfX, y, -(e - halfZ)));

    // -------------------------------------------------
// PASS 2 – Recessed port boxes
// -------------------------------------------------
    void BuildPortRecess(List<float> edges, int portCount,
        System.Func<float, float, Vector3> outerPos,
        System.Func<float, float, Vector3> innerPos)
    {
        for (int p = 0; p < portCount; p++)
        {
            // Each port occupies 3 entries in the edges list: gap, start, end
            // But because we built edges as: corner, start, end, midgap, start, end, ...
            // the start/end of port p are at indices:
            int startIdx = 1 + p * 3;
            int endIdx   = startIdx + 1;

            if (endIdx >= edges.Count) continue;

            float start = edges[startIdx];
            float end   = edges[endIdx];

            // We only recess the lower two height levels (bottom and middle)
            float y0 = heights[0]; // 0
            float y1 = heights[1]; // packageSlot height

            // Outer corners of the port opening
            int o_bl = AddVertex(outerPos(start, y0));
            int o_br = AddVertex(outerPos(end,   y0));
            int o_tl = AddVertex(outerPos(start, y1));
            int o_tr = AddVertex(outerPos(end,   y1));

            // Inner (recessed) corners
            int i_bl = AddVertex(innerPos(start, y0));
            int i_br = AddVertex(innerPos(end,   y0));
            int i_tl = AddVertex(innerPos(start, y1));
            int i_tr = AddVertex(innerPos(end,   y1));

            // ----- Recess faces -----

            // Bottom of recess
            AddQuad(recessTris, o_bl, o_br, i_bl, i_br);

            // Top of recess
            AddQuad(recessTris, i_tl, i_tr, o_tl, o_tr);

            // Left side of recess
            AddQuad(recessTris, o_bl, i_bl, o_tl, i_tl);

            // Right side of recess
            AddQuad(recessTris, i_br, o_br, i_tr, o_tr);

            // Back of recess (the actual inset face)
            AddQuad(recessTris, i_bl, i_br, i_tl, i_tr);
        }
    }
    
    // Call it for all four walls
    BuildPortRecess(edgesX, portsX,
        (e, y) => new Vector3(e - halfX, y, -halfZ),                 // Front outer
        (e, y) => new Vector3(e - halfX, y, -halfZ + inset));        // Front inner

    BuildPortRecess(edgesZ, portsZ,
        (e, y) => new Vector3(halfX, y, e - halfZ),                  // Right outer
        (e, y) => new Vector3(halfX - inset, y, e - halfZ));         // Right inner

    BuildPortRecess(edgesX, portsX,
        (e, y) => new Vector3(-(e - halfX), y, halfZ),               // Back outer
        (e, y) => new Vector3(-(e - halfX), y, halfZ - inset));      // Back inner

    BuildPortRecess(edgesZ, portsZ,
        (e, y) => new Vector3(-halfX, y, -(e - halfZ)),              // Left outer
        (e, y) => new Vector3(-halfX + inset, y, -(e - halfZ)));     // Left inner

    // -------------------------------------------------
// PASS 3 – Solid walls above the ports
// -------------------------------------------------
    void BuildAbovePortWalls(List<float> edges, int portCount,
        System.Func<float, float, Vector3> posFunc)
    {
        float y0 = heights[1]; // top of the port (packageSlot)
        float y1 = heights[2]; // top of the chest

        for (int p = 0; p < portCount; p++)
        {
            int startIdx = 1 + p * 3;
            int endIdx   = startIdx + 1;

            if (endIdx >= edges.Count) continue;

            float start = edges[startIdx];
            float end   = edges[endIdx];

            int bl = AddVertex(posFunc(start, y0));
            int br = AddVertex(posFunc(end,   y0));
            int tl = AddVertex(posFunc(start, y1));
            int tr = AddVertex(posFunc(end,   y1));

            AddQuad(shellTris, bl, br, tl, tr);
        }
    }

// Call for all four walls
    BuildAbovePortWalls(edgesX, portsX, (e, y) => new Vector3(e - halfX, y, -halfZ));          // Front
    BuildAbovePortWalls(edgesZ, portsZ, (e, y) => new Vector3(halfX, y, e - halfZ));           // Right
    BuildAbovePortWalls(edgesX, portsX, (e, y) => new Vector3(-(e - halfX), y, halfZ));        // Back
    BuildAbovePortWalls(edgesZ, portsZ, (e, y) => new Vector3(-halfX, y, -(e - halfZ)));       // Left
    
    // -------------------------------------------------
// PASS 4 – Top and Bottom faces
// -------------------------------------------------

// Bottom face (y = 0)
    {
        int bl = AddVertex(new Vector3(-halfX, 0f, -halfZ)); // front-left
        int br = AddVertex(new Vector3( halfX, 0f, -halfZ)); // front-right
        int tl = AddVertex(new Vector3(-halfX, 0f,  halfZ)); // back-left
        int tr = AddVertex(new Vector3( halfX, 0f,  halfZ)); // back-right

        // Note: winding is reversed so the normal points downwards
        AddQuad(shellTris, bl, tl, br, tr);
    }

// Top face (y = height)
    {
        int bl = AddVertex(new Vector3(-halfX, height, -halfZ)); // front-left
        int br = AddVertex(new Vector3( halfX, height, -halfZ)); // front-right
        int tl = AddVertex(new Vector3(-halfX, height,  halfZ)); // back-left
        int tr = AddVertex(new Vector3( halfX, height,  halfZ)); // back-right

        AddQuad(shellTris, bl, br, tl, tr);
    }
    
    // -------------------------------------------------
    // Finish mesh
    // -------------------------------------------------
    Mesh mesh = new Mesh { name = "PortedChestMesh" };
    mesh.subMeshCount = 2;
    mesh.SetVertices(vertices);
    mesh.SetTriangles(shellTris, 0);
    mesh.SetTriangles(recessTris, 1);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    return mesh;
}
  
  
  
 
private static void DebugShowAllVertices(List<Vector3> topVertices)
{
    // Remove old debug objects
    GameObject old = GameObject.Find("CornerDebugVertices");
    if (old != null) Object.DestroyImmediate(old);

    GameObject root = new GameObject("CornerDebugVertices");

    for (int i = 0; i < topVertices.Count; i++)
    {
        Vector3 pos = topVertices[i];

        // Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"V{i}";
        sphere.transform.SetParent(root.transform);
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * 0.045f;

        var rend = sphere.GetComponent<Renderer>();
        rend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        rend.sharedMaterial.color = Color.cyan;

        Object.DestroyImmediate(sphere.GetComponent<Collider>());

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(sphere.transform);
        labelGO.transform.localPosition = new Vector3(0f, 0.11f, 0f);

        TextMesh text = labelGO.AddComponent<TextMesh>();
        text.text = $"V{i}\n({pos.x:F2}, {pos.z:F2})";
        text.fontSize = 18;
        text.characterSize = 0.018f;
        text.anchor = TextAnchor.LowerCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;
    }

    Debug.Log($"Debug: Showing {topVertices.Count} vertices");
}
 
}
#endif