using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Draws a world grid on the ground plane. Toggle with G.
/// </summary>
public class GridOverlay : MonoBehaviour
{
    [Header("State")]
    public bool showGrid = true;

    static Material lineMat;

    void Awake()
    {
        if (lineMat == null)
        {
            // Unlit transparent line material
            var shader = Shader.Find("Hidden/Internal-Colored");
            lineMat = new Material(shader);
            lineMat.hideFlags = HideFlags.HideAndDontSave;
            lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMat.SetInt("_ZWrite", 0);
        }
        
    }

    void Update()
    {
        if (WasGPressed())
            showGrid = !showGrid;
    }

    bool WasGPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }

    void OnRenderObject()
    {
        if (!showGrid || lineMat == null) return;

        lineMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);

        float start = GridConfig.FromSize * CoreConfig.TileSize;
        float end   = GridConfig.ToSize * CoreConfig.TileSize;

        for (int i = GridConfig.FromSize; i <= GridConfig.ToSize; i++)
        {
            bool major = (i % GridConfig.MajorEvery == 0);
            Color c = major ? GridConfig.MajorColor : GridConfig.MinorColor;
            GL.Color(c);

            float t = i * CoreConfig.TileSize;

            // Line along Z (constant X)
            GL.Vertex3(t, GridConfig.HeightOffset, start);
            GL.Vertex3(t, GridConfig.HeightOffset, end);

            // Line along X (constant Z)
            GL.Vertex3(start, GridConfig.HeightOffset, t);
            GL.Vertex3(end,   GridConfig.HeightOffset, t);
        }

        GL.End();
        GL.PopMatrix();
    }
}