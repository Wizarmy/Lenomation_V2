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

    public static Mesh FinishMesh(string name, List<Vector3> verts, List<int> tris)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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

    public static Mesh FinishSubmeshes(string name, List<Vector3> verts, params List<int>[] submeshes)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.subMeshCount = submeshes.Length;
        for (int i = 0; i < submeshes.Length; i++)
            mesh.SetTriangles(submeshes[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh FinishSubmeshes(string name, Vector3[] verts, params int[][] submeshes)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.subMeshCount = submeshes.Length;
        for (int i = 0; i < submeshes.Length; i++)
            mesh.SetTriangles(submeshes[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddQuad(List<int> tris, int a, int b, int c, int d)
    {
        tris.Add(a); tris.Add(c); tris.Add(b);
        tris.Add(a); tris.Add(d); tris.Add(c);
    }

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

    public static Vector3 EllipsePoint(float angle, float radiusX, float radiusZ)
    {
        return new Vector3(
            radiusX * Mathf.Cos(angle) - 0.5f,
            0f,
            radiusZ * Mathf.Sin(angle));
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

    static Vector3 CornerCentre => new Vector3(0.5f, 0f, -0.5f);

    static float CornerAngle(int i, int segs) =>
        Mathf.Lerp(Mathf.PI, Mathf.PI * 0.5f, i / (float)segs);

    static Vector3 CornerPoint(float radius, float angle) =>
        CornerCentre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

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
        var mesh = FinishMesh(name, verts, tris);
        if (withUVs)
        {
            mesh.uv = new[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1),
            };
        }
        return mesh;
    }

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

        return FinishSubmeshes(name, verts,
            new[] { 4, 6, 5, 4, 7, 6 },
            new[] { 0, 1, 2, 0, 2, 3 },
            new[] { 0, 3, 7, 0, 7, 4, 1, 6, 2, 1, 5, 6 });
    }

    public static Mesh CreateBoxMesh(Vector3 size, Vector3 centre, string name) =>
        CreateBoxMeshSubmeshes(size, centre, name);

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
        return FinishMesh("StraightGuardRailMesh", verts, tris);
    }

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
        foreach (var p in ring) verts.Add(new Vector3(p.x, 0f, p.z));
        int bottomCentre = verts.Count;
        verts.Add(new Vector3(-0.5f, 0f, 0f));

        int topArc = verts.Count;
        foreach (var p in ring) verts.Add(new Vector3(p.x, height, p.z));
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

        return FinishSubmeshes(name, verts, top, bot, side);
    }

    public static Mesh CreateEndCapGuardRailMesh()
    {
        int   segments = Mathf.Max(4, ConveyorConfig.CurveSegments);
        float thick    = ConveyorConfig.GuardRailWidth;
        float hy       = ConveyorConfig.GuardRailHeight * 0.5f;

        var outer = SampleHalfEllipse(
            ConveyorConfig.EndCapArcCurve,
            ConveyorConfig.HalfBeltWidth,
            segments);
        var inner = SampleHalfEllipse(
            Mathf.Max(0f, ConveyorConfig.EndCapArcCurve - thick),
            Mathf.Max(0f, ConveyorConfig.HalfBeltWidth - thick),
            segments);

        var verts = new List<Vector3>();
        var tris  = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            verts.Add(outer[i] + Vector3.up * -hy);
            verts.Add(inner[i] + Vector3.up * -hy);
            verts.Add(inner[i] + Vector3.up *  hy);
            verts.Add(outer[i] + Vector3.up *  hy);
        }

        for (int i = 0; i < segments; i++)
        {
            int p = i * 4, q = (i + 1) * 4;
            AddQuad(tris, p + 3, q + 3, q + 2, p + 2);
            AddQuad(tris, p + 0, p + 1, q + 1, q + 0);
            AddQuad(tris, p + 0, q + 0, q + 3, p + 3);
            AddQuad(tris, p + 1, p + 2, q + 2, q + 1);
        }

        int s = 0, e = segments * 4;
        AddQuad(tris, s + 0, s + 3, s + 2, s + 1);
        AddQuad(tris, e + 0, e + 1, e + 2, e + 3);

        return FinishMesh("EndCapGuardRailMesh", verts, tris);
    }

    public static Mesh CreateCornerMesh(string name)
    {
        float h     = ConveyorConfig.BeltHeight;
        int   segs  = Mathf.Max(4, ConveyorConfig.CurveSegments);
        float inner = ConveyorConfig.CornerInnerRadius;
        float outer = ConveyorConfig.CornerOuterRadius;

        var verts = new List<Vector3>();
        var top   = new List<int>();
        var bot   = new List<int>();
        var side  = new List<int>();

        for (int i = 0; i <= segs; i++)
        {
            float angle = CornerAngle(i, segs);
            Vector3 inn  = CornerPoint(inner, angle);
            Vector3 outp = CornerPoint(outer, angle);
            verts.Add(inn);
            verts.Add(outp);
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
            AddQuad(side, p + 0, q + 0, q + 2, p + 2); // inner
            AddQuad(side, p + 1, p + 3, q + 3, q + 1); // outer
        }

        return FinishSubmeshes(name, verts, top, bot, side);
    }

    public static Mesh CreateCornerGuardRailMesh()
    {
        int   segs  = Mathf.Max(4, ConveyorConfig.CurveSegments);
        float hy    = ConveyorConfig.GuardRailHeight * 0.5f;
        float hw    = ConveyorConfig.GuardRailWidth * 0.5f;
        float inner = ConveyorConfig.CornerInnerRadius + hw;
        float outer = ConveyorConfig.CornerOuterRadius - hw;

        var verts = new List<Vector3>();
        var tris  = new List<int>();

        void Ring(float radius)
        {
            int start = verts.Count;
            for (int i = 0; i <= segs; i++)
            {
                float angle = CornerAngle(i, segs);
                Vector3 mid = CornerPoint(radius, angle);
                Vector3 n = mid - CornerCentre;
                n.y = 0f;
                n.Normalize();
                verts.Add(mid - n * hw + Vector3.up * -hy);
                verts.Add(mid + n * hw + Vector3.up * -hy);
                verts.Add(mid + n * hw + Vector3.up *  hy);
                verts.Add(mid - n * hw + Vector3.up *  hy);
            }

            for (int i = 0; i < segs; i++)
            {
                int p = start + i * 4, q = start + (i + 1) * 4;
                AddQuad(tris, p + 3, q + 3, q + 2, p + 2);
                AddQuad(tris, p + 0, p + 1, q + 1, q + 0);
                AddQuad(tris, p + 0, q + 0, q + 3, p + 3);
                AddQuad(tris, p + 1, p + 2, q + 2, q + 1);
            }
        }

        Ring(inner);
        Ring(outer);
        return FinishMesh("CornerGuardRailMesh", verts, tris);
    }

    public static Mesh CreateCubeMeshSixFaces(Vector3 size, string name)
    {
        Vector3 h = size * 0.5f;
        Vector3[] verts =
        {
            new Vector3(-h.x,  h.y, -h.z), new Vector3(-h.x,  h.y,  h.z),
            new Vector3( h.x,  h.y,  h.z), new Vector3( h.x,  h.y, -h.z),
            new Vector3(-h.x, -h.y,  h.z), new Vector3(-h.x, -h.y, -h.z),
            new Vector3( h.x, -h.y, -h.z), new Vector3( h.x, -h.y,  h.z),
            new Vector3(-h.x, -h.y,  h.z), new Vector3( h.x, -h.y,  h.z),
            new Vector3( h.x,  h.y,  h.z), new Vector3(-h.x,  h.y,  h.z),
            new Vector3( h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z),
            new Vector3(-h.x,  h.y, -h.z), new Vector3( h.x,  h.y, -h.z),
            new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y,  h.z),
            new Vector3(-h.x,  h.y,  h.z), new Vector3(-h.x,  h.y, -h.z),
            new Vector3( h.x, -h.y,  h.z), new Vector3( h.x, -h.y, -h.z),
            new Vector3( h.x,  h.y, -h.z), new Vector3( h.x,  h.y,  h.z),
        };

        Vector2[] uvs =
        {
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
        };

        var faces = new int[6][];
        for (int i = 0; i < 6; i++)
        {
            int o = i * 4;
            faces[i] = new[] { o, o + 1, o + 2, o, o + 2, o + 3 };
        }

        var mesh = FinishSubmeshes(name, verts, faces);
        mesh.uv = uvs;
        return mesh;
    }
    
    public static Mesh CreateCylinderMesh(float radius, float height, int segments, string name)
    {
        segments = Mathf.Max(8, segments);
        var verts = new List<Vector3>();
        var tris  = new List<int>();

        float hy = height * 0.5f;
        verts.Add(new Vector3(0f, -hy, 0f));
        verts.Add(new Vector3(0f,  hy, 0f));
        int botC = 0, topC = 1;

        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius;
            verts.Add(new Vector3(x, -hy, z));
            verts.Add(new Vector3(x,  hy, z));
        }

        for (int i = 0; i < segments; i++)
        {
            int b0 = 2 + i * 2, b1 = b0 + 2;
            int t0 = b0 + 1,    t1 = b1 + 1;
            tris.Add(botC); tris.Add(b0); tris.Add(b1);
            tris.Add(topC); tris.Add(t1); tris.Add(t0);
            tris.Add(b0); tris.Add(t0); tris.Add(t1);
            tris.Add(b0); tris.Add(t1); tris.Add(b1);
        }

        return FinishMesh(name, verts, tris);
    }
    
    public static Mesh CreateSolidBoxMesh(Vector3 size, Vector3 centre, string name)
    {
        var verts = new List<Vector3>(8);
        var tris  = new List<int>(36);
        AppendBox(verts, tris, centre, size * 0.5f);
        return FinishMesh(name, verts, tris);
    }
}
#endif