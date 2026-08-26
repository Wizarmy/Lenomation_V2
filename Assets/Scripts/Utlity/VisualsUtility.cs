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
    
}
