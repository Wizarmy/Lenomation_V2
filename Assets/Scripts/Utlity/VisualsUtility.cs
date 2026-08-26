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
    
    
}
