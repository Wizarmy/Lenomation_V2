using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VisualsUtility : MonoBehaviour
{
    
    public static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"{PathingConfig.MaterialFolder}{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }
    
    public static Mesh CreateStraightArrowMesh()
    {
        float size  = ArrowConfig.ArrowSize;
        float depth = ArrowConfig.ArrowDepth;
        float halfD = depth * 0.5f;

        Vector3[] verts = {
            new Vector3(0,  halfD,  size * 0.9f),
            new Vector3(-size * 0.55f,  halfD, -size * 0.7f),
            new Vector3( size * 0.55f,  halfD, -size * 0.7f),
            new Vector3(0, -halfD,  size * 0.9f),
            new Vector3(-size * 0.55f, -halfD, -size * 0.7f),
            new Vector3( size * 0.55f, -halfD, -size * 0.7f)
        };

        int[] tris = {
            0, 2, 1,
            3, 4, 5,
            0, 1, 4, 0, 4, 3,
            1, 2, 5, 1, 5, 4,
            2, 0, 3, 2, 3, 5
        };

        return FinishMesh("StraightArrowMesh", verts, tris);
    }
    
    public static Mesh FinishMesh(string name, Vector3[] verts, int[] tris)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    
    /// <summary>Horizontal quad in XZ, normal +Y. Optional UVs.</summary>
    public static Mesh CreateQuadMesh(float sizeX, float sizeZ, float y, string name, bool withUVs = true)
    {
        float hx = sizeX * 0.5f;
        float hz = sizeZ * 0.5f;

        Vector3[] verts = {
            new Vector3(-hx, y, -hz),
            new Vector3( hx, y, -hz),
            new Vector3( hx, y,  hz),
            new Vector3(-hx, y,  hz),
        };

        int[] tris = { 0, 2, 1, 0, 3, 2 };

        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.triangles = tris;
        if (withUVs)
        {
            mesh.uv = new Vector2[] {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1),
            };
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    
    /// <summary>
    /// Box with 3 submeshes: 0 = top, 1 = bottom, 2 = sides.
    /// Used by straight conveyors.
    /// </summary>
    public static Mesh CreateBoxMeshSubmeshes(Vector3 size, Vector3 centre, string name)
    {
        Vector3 h = size * 0.5f;

        Vector3[] verts = {
            centre + new Vector3(-h.x, -h.y, -h.z),
            centre + new Vector3( h.x, -h.y, -h.z),
            centre + new Vector3( h.x, -h.y,  h.z),
            centre + new Vector3(-h.x, -h.y,  h.z),
            centre + new Vector3(-h.x,  h.y, -h.z),
            centre + new Vector3( h.x,  h.y, -h.z),
            centre + new Vector3( h.x,  h.y,  h.z),
            centre + new Vector3(-h.x,  h.y,  h.z),
        };

        int[] top    = { 4, 6, 5, 4, 7, 6 };
        int[] bottom = { 0, 1, 2, 0, 2, 3 };
        // -X, +X only (matches previous straight belt sides set)
        int[] sides  = { 0, 3, 7, 0, 7, 4, 1, 6, 2, 1, 5, 6 };

        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.subMeshCount = 3;
        mesh.SetTriangles(top, 0);
        mesh.SetTriangles(bottom, 1);
        mesh.SetTriangles(sides, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    
public static Mesh CreateEndCapMeshEllipse(string name)
{
    float height     = ConveyorConfig.BeltHeight;
    float curveMax = ConveyorConfig.EndCapArcCurve;
    float a          = 0.5f;
    int   segments   = Mathf.Max(4, ConveyorConfig.CurveSegments);

    const float ox = 0.5f;
    const float oz = 0.5f;

    // ------------------------------------------------------------------
    // Ellipse XZ samples (y = 0), tile-centred — computed once
    // ------------------------------------------------------------------
    var ellipseXZ = new List<Vector3>(segments + 1);
    for (int i = 0; i <= segments; i++)
    {
        float t     = i / (float)segments;
        float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, t);

        float x = curveMax * Mathf.Cos(angle);
        float z = 0.5f + a * Mathf.Sin(angle);
        ellipseXZ.Add(new Vector3(x - ox, 0f, z - oz));
    }

    Vector3 WithY(Vector3 xz, float y) => new Vector3(xz.x, y, xz.z);

    var verts      = new List<Vector3>();
    var topTris    = new List<int>();
    var bottomTris = new List<int>();
    var sideTris   = new List<int>();

    // ------------------------------------------------------------------
    // Vertices from shared XZ list
    // ------------------------------------------------------------------
    int bottomArcStart = verts.Count;
    foreach (var p in ellipseXZ)
        verts.Add(WithY(p, 0f));

    int bottomCentre = verts.Count;
    verts.Add(new Vector3(0f - ox, 0f, 0.5f - oz));

    int topArcStart = verts.Count;
    foreach (var p in ellipseXZ)
        verts.Add(WithY(p, height));

    int topCentre = verts.Count;
    verts.Add(new Vector3(0f - ox, height, 0.5f - oz));
    
    // ------------------------------------------------------------------
    // Top deck fan
    // ------------------------------------------------------------------
    for (int i = 0; i < segments; i++)
    {
        topTris.Add(topCentre);
        topTris.Add(topArcStart + i + 1);
        topTris.Add(topArcStart + i);
    }

    // ------------------------------------------------------------------
    // Bottom fan
    // ------------------------------------------------------------------
    for (int i = 0; i < segments; i++)
    {
        bottomTris.Add(bottomCentre);
        bottomTris.Add(bottomArcStart + i);
        bottomTris.Add(bottomArcStart + i + 1);
    }

    // ------------------------------------------------------------------
    // Curved wall (bottom → deck)
    // ------------------------------------------------------------------
    for (int i = 0; i < segments; i++)
    {
        int b0 = bottomArcStart + i;
        int b1 = bottomArcStart + i + 1;
        int t0 = topArcStart + i;
        int t1 = topArcStart + i + 1;

        sideTris.Add(b0); sideTris.Add(t0); sideTris.Add(t1);
        sideTris.Add(b0); sideTris.Add(t1); sideTris.Add(b1);
    }
    
    var mesh = new Mesh { name = name };
    mesh.SetVertices(verts);
    mesh.subMeshCount = 3;
    mesh.SetTriangles(topTris, 0);
    mesh.SetTriangles(bottomTris, 1);
    mesh.SetTriangles(sideTris, 2);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    return mesh;
}
    
    
}
