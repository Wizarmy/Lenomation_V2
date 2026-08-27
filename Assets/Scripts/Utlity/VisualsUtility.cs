#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class VisualsUtility
{
    public static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"{PathingConfig.MaterialFolder}{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
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

    public static Mesh FinishMesh(string name, Vector3[] verts, int[] tris)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ------------------------------------------------------------------
    // Ellipse samples (tile-centred, bulge +X)
    // ------------------------------------------------------------------
    public static Vector3 EllipsePoint(float angle, float radiusX, float radiusZ)
    {
        return new Vector3(
            radiusX * Mathf.Cos(angle) - 0.5f,
            0f,
            0.5f + radiusZ * Mathf.Sin(angle) - 0.5f);
    }

    public static List<Vector3> SampleHalfEllipse(float radiusX, float radiusZ, int segments)
    {
        var pts = new List<Vector3>(segments + 1);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, t);
            pts.Add(EllipsePoint(angle, radiusX, radiusZ));
        }
        return pts;
    }

    static void AddQuad(List<int> tris, int a, int b, int c, int d)
    {
        tris.Add(a); tris.Add(c); tris.Add(b);
        tris.Add(a); tris.Add(d); tris.Add(c);
    }

    // ------------------------------------------------------------------
    // Arrows
    // ------------------------------------------------------------------
    public static Mesh CreateStraightArrowMesh()
    {
        float size = ArrowConfig.ArrowSize;
        float halfD = ArrowConfig.ArrowDepth * 0.5f;

        Vector3[] verts =
        {
            new Vector3(0,  halfD,  size * 0.9f),
            new Vector3(-size * 0.55f,  halfD, -size * 0.7f),
            new Vector3( size * 0.55f,  halfD, -size * 0.7f),
            new Vector3(0, -halfD,  size * 0.9f),
            new Vector3(-size * 0.55f, -halfD, -size * 0.7f),
            new Vector3( size * 0.55f, -halfD, -size * 0.7f)
        };

        int[] tris =
        {
            0, 2, 1,  3, 4, 5,
            0, 1, 4,  0, 4, 3,
            1, 2, 5,  1, 5, 4,
            2, 0, 3,  2, 3, 5
        };

        return FinishMesh("StraightArrowMesh", verts, tris);
    }
    
    /// <summary>Horizontal quad in XZ, normal +Y. Optional UVs.</summary>
    public static Mesh CreateQuadMesh(float sizeX, float sizeZ, float y, string name, bool withUVs = true)
    {
        float hx = sizeX * 0.5f;
        float hz = sizeZ * 0.5f;

        Vector3[] verts =
        {
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
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1),
            };
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ------------------------------------------------------------------
    // Boxes
    // ------------------------------------------------------------------
    public static Mesh CreateBoxMeshSubmeshes(Vector3 size, Vector3 centre, string name)
    {
        Vector3 h = size * 0.5f;
        Vector3[] verts =
        {
            centre + new Vector3(-h.x, -h.y, -h.z),
            centre + new Vector3( h.x, -h.y, -h.z),
            centre + new Vector3( h.x, -h.y,  h.z),
            centre + new Vector3(-h.x, -h.y,  h.z),
            centre + new Vector3(-h.x,  h.y, -h.z),
            centre + new Vector3( h.x,  h.y, -h.z),
            centre + new Vector3( h.x,  h.y,  h.z),
            centre + new Vector3(-h.x,  h.y,  h.z),
        };

        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.subMeshCount = 3;
        mesh.SetTriangles(new[] { 4, 6, 5, 4, 7, 6 }, 0);
        mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 1);
        mesh.SetTriangles(new[] { 0, 3, 7, 0, 7, 4, 1, 6, 2, 1, 5, 6 }, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CreateBoxMesh(Vector3 size, Vector3 centre, string name)
        => CreateBoxMeshSubmeshes(size, centre, name);

    static void AppendBox(List<Vector3> verts, List<int> tris, Vector3 c, Vector3 h)
    {
        int o = verts.Count;
        verts.Add(c + new Vector3(-h.x, -h.y, -h.z));
        verts.Add(c + new Vector3( h.x, -h.y, -h.z));
        verts.Add(c + new Vector3( h.x, -h.y,  h.z));
        verts.Add(c + new Vector3(-h.x, -h.y,  h.z));
        verts.Add(c + new Vector3(-h.x,  h.y, -h.z));
        verts.Add(c + new Vector3( h.x,  h.y, -h.z));
        verts.Add(c + new Vector3( h.x,  h.y,  h.z));
        verts.Add(c + new Vector3(-h.x,  h.y,  h.z));

        int[] local =
        {
            4,6,5, 4,7,6,  0,1,2, 0,2,3,
            0,3,7, 0,7,4,  1,5,6, 1,6,2,
            0,4,5, 0,5,1,  3,2,6, 3,6,7
        };
        foreach (int i in local) tris.Add(o + i);
    }

    public static Mesh CreateStraightGuardRailMesh()
    {
        float hw = ConveyorConfig.HalfBeltWidth;
        float rw = ConveyorConfig.GuardRailWidth;
        float rh = ConveyorConfig.GuardRailHeight;
        float hz = ConveyorConfig.HalfBeltLength;
        Vector3 half = new Vector3(rw * 0.5f, rh * 0.5f, hz);

        var verts = new List<Vector3>(16);
        var tris  = new List<int>(72);
        AppendBox(verts, tris, new Vector3(-hw + rw * 0.5f, 0f, 0f), half);
        AppendBox(verts, tris, new Vector3( hw - rw * 0.5f, 0f, 0f), half);
        return FinishMesh("StraightGuardRailMesh", verts.ToArray(), tris.ToArray());
    }

    // ------------------------------------------------------------------
    // Endcap + curved rail
    // ------------------------------------------------------------------
   public static Mesh CreateEndCapMeshEllipse(string name)
{
    float height   = ConveyorConfig.BeltHeight;
    int   segments = Mathf.Max(4, ConveyorConfig.CurveSegments);

    var ring = SampleHalfEllipse(
        ConveyorConfig.EndCapArcCurve,
        ConveyorConfig.HalfBeltWidth,
        segments);

    var verts = new List<Vector3>();
    var top   = new List<int>();
    var bot   = new List<int>();
    var side  = new List<int>();

    int bottomArc = verts.Count;
    foreach (var p in ring)
        verts.Add(new Vector3(p.x, 0f, p.z));

    int bottomCentre = verts.Count;
    verts.Add(new Vector3(-0.5f, 0f, 0f));

    int topArc = verts.Count;
    foreach (var p in ring)
        verts.Add(new Vector3(p.x, height, p.z));

    int topCentre = verts.Count;
    verts.Add(new Vector3(-0.5f, height, 0f));

    for (int i = 0; i < segments; i++)
    {
        top.Add(topCentre); top.Add(topArc + i + 1); top.Add(topArc + i);
        bot.Add(bottomCentre); bot.Add(bottomArc + i); bot.Add(bottomArc + i + 1);

        int b0 = bottomArc + i, b1 = bottomArc + i + 1;
        int t0 = topArc + i,    t1 = topArc + i + 1;
        side.Add(b0); side.Add(t0); side.Add(t1);
        side.Add(b0); side.Add(t1); side.Add(b1);
    }

    var mesh = new Mesh { name = name };
    mesh.SetVertices(verts);
    mesh.subMeshCount = 3;
    mesh.SetTriangles(top, 0);
    mesh.SetTriangles(bot, 1);
    mesh.SetTriangles(side, 2);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    return mesh;
}

public static Mesh CreateEndCapGuardRailMesh()
{
    int   segments = Mathf.Max(4, ConveyorConfig.CurveSegments);
    float thick    = ConveyorConfig.GuardRailWidth;
    float hy       = ConveyorConfig.GuardRailHeight * 0.5f;

    float outerR = ConveyorConfig.EndCapArcCurve;
    float innerR = Mathf.Max(0f, outerR - thick);
    float outerA = ConveyorConfig.HalfBeltWidth;
    float innerA = Mathf.Max(0f, outerA - thick);

    var outer = SampleHalfEllipse(outerR, outerA, segments);
    var inner = SampleHalfEllipse(innerR, innerA, segments);

    var verts = new List<Vector3>();
    var tris  = new List<int>();

    for (int i = 0; i <= segments; i++)
    {
        verts.Add(outer[i] + Vector3.up * -hy);
        verts.Add(inner[i] + Vector3.up * -hy);
        verts.Add(inner[i] + Vector3.up *  hy);
        verts.Add(outer[i] + Vector3.up *  hy);
    }

    void Quad(int a0, int b0, int c0, int d0)
    {
        tris.Add(a0); tris.Add(c0); tris.Add(b0);
        tris.Add(a0); tris.Add(d0); tris.Add(c0);
    }

    for (int i = 0; i < segments; i++)
    {
        int p = i * 4, q = (i + 1) * 4;
        Quad(p + 3, q + 3, q + 2, p + 2);
        Quad(p + 0, p + 1, q + 1, q + 0);
        Quad(p + 0, q + 0, q + 3, p + 3);
        Quad(p + 1, p + 2, q + 2, q + 1);
    }

    int s = 0, e = segments * 4;
    Quad(s + 0, s + 3, s + 2, s + 1);
    Quad(e + 0, e + 1, e + 2, e + 3);

    return FinishMesh("EndCapGuardRailMesh", verts.ToArray(), tris.ToArray());
}

public static Mesh CreateCornerMesh(string name)
{
    float h      = ConveyorConfig.BeltHeight;
    int   segs   = Mathf.Max(4, ConveyorConfig.CurveSegments);
    float inner  = ConveyorConfig.CornerInnerRadius;
    float outer  = ConveyorConfig.CornerOuterRadius;
    Vector3 c    = new Vector3(0.5f, 0f, -0.5f); // SE corner, tile-centred

    var verts = new List<Vector3>();
    var top   = new List<int>();
    var bot   = new List<int>();
    var side  = new List<int>();

    // Per sample: inner-bottom, outer-bottom, inner-top, outer-top
    for (int i = 0; i <= segs; i++)
    {
        float t     = i / (float)segs;
        float angle = Mathf.Lerp(Mathf.PI, Mathf.PI * 0.5f, t); // south → east
        float ca = Mathf.Cos(angle);
        float sa = Mathf.Sin(angle);

        Vector3 inn = c + new Vector3(ca * inner, 0f, sa * inner);
        Vector3 outp = c + new Vector3(ca * outer, 0f, sa * outer);

        verts.Add(inn + Vector3.up * 0f);
        verts.Add(outp + Vector3.up * 0f);
        verts.Add(inn + Vector3.up * h);
        verts.Add(outp + Vector3.up * h);
    }

    for (int i = 0; i < segs; i++)
    {
        int p = i * 4, q = (i + 1) * 4;
        top.Add(p + 2); top.Add(q + 3); top.Add(q + 2);
        top.Add(p + 2); top.Add(p + 3); top.Add(q + 3);

        bot.Add(p + 0); bot.Add(q + 1); bot.Add(p + 1);
        bot.Add(p + 0); bot.Add(q + 0); bot.Add(q + 1);

        // inner wall
        side.Add(p + 0); side.Add(q + 2); side.Add(q + 0);
        side.Add(p + 0); side.Add(p + 2); side.Add(q + 2);
        // outer wall
        side.Add(p + 1); side.Add(q + 3); side.Add(p + 3);
        side.Add(p + 1); side.Add(q + 1); side.Add(q + 3);
    }

    var mesh = new Mesh { name = name };
    mesh.SetVertices(verts);
    mesh.subMeshCount = 3;
    mesh.SetTriangles(top, 0);
    mesh.SetTriangles(bot, 1);
    mesh.SetTriangles(side, 2);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    return mesh;
}

public static Mesh CreateCornerGuardRailMesh()
{
    int   segs  = Mathf.Max(4, ConveyorConfig.CurveSegments);
    float hy    = ConveyorConfig.GuardRailHeight * 0.5f;
    float hw    = ConveyorConfig.GuardRailWidth  * 0.5f;
    float inner = ConveyorConfig.CornerInnerRadius+hw;
    float outer = ConveyorConfig.CornerOuterRadius-hw;
    Vector3 c   = new Vector3(0.5f, 0f, -0.5f);

    var verts = new List<Vector3>();
    var tris  = new List<int>();

    void Ring(float radius)
    {
        int start = verts.Count;
        for (int i = 0; i <= segs; i++)
        {
            float angle = Mathf.Lerp(Mathf.PI, Mathf.PI * 0.5f, i / (float)segs);
            Vector3 mid = c + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 n   = (mid - c); n.y = 0f; n.Normalize();
           

            verts.Add(mid - n * hw + Vector3.up * -hy);
            verts.Add(mid + n * hw + Vector3.up * -hy);
            verts.Add(mid + n * hw + Vector3.up *  hy);
            verts.Add(mid - n * hw + Vector3.up *  hy);
        }

        void Quad(int a, int b, int c0, int d)
        {
            tris.Add(a); tris.Add(c0); tris.Add(b);
            tris.Add(a); tris.Add(d);  tris.Add(c0);
        }

        for (int i = 0; i < segs; i++)
        {
            int p = start + i * 4, q = start + (i + 1) * 4;
            Quad(p + 3, q + 3, q + 2, p + 2);
            Quad(p + 0, p + 1, q + 1, q + 0);
            Quad(p + 0, q + 0, q + 3, p + 3);
            Quad(p + 1, p + 2, q + 2, q + 1);
        }
    }

    Ring(inner);
    Ring(outer);
    return FinishMesh("CornerGuardRailMesh", verts.ToArray(), tris.ToArray());
}
}
#endif