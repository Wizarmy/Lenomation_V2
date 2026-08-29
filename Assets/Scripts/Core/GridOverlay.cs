using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Draws a world grid on the ground plane. Toggle with G.
/// Labels toggle with L (only within LabelRadiusTiles of camera).
/// Left-click a grid square to log its position.
///
/// Convention (TileSize = 1):
///   Cell (gx, gz) occupies world [gx, gx+1) × [gz, gz+1)
///   → centre of (0,0) is world (0.5, 0.5)
/// </summary>
public class GridOverlay : MonoBehaviour
{
    [Header("State")]
    public bool showGrid = true;
    public bool showLabels = false;

    static Material lineMat;

    // ------------------------------------------------------------------
    // Grid ↔ World helpers  (centre of (0,0) = (0.5, 0.5) * TileSize)
    // ------------------------------------------------------------------
    public static Vector3 GridToWorldCenter(int gx, int gz) =>
        CoreConfig.CellCenter(gx, gz, GroundConfig.GroundY + GridConfig.HeightOffset);

    public static void WorldToGrid(Vector3 world, out int gx, out int gz)
    {
        Vector2Int cell = CoreConfig.WorldToCell(world);
        gx = cell.x;
        gz = cell.y;
    }

    // ------------------------------------------------------------------

    void Awake()
    {
        if (lineMat == null)
        {
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
        if (WasGPressed()) showGrid = !showGrid;
        if (WasLPressed()) showLabels = !showLabels;
        if (WasLeftClickPressed()) TryLogClickedGridSquare();
    }

    bool WasGPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }

    bool WasLPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.L);
#endif
    }

    bool WasLeftClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    void TryLogClickedGridSquare()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 mousePos;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        mousePos = Mouse.current.position.ReadValue();
#else
        mousePos = Input.mousePosition;
#endif

        Ray ray = cam.ScreenPointToRay(mousePos);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, GroundConfig.GroundY, 0f));
        if (!ground.Raycast(ray, out float enter)) return;

        Vector3 hit = ray.GetPoint(enter);
        WorldToGrid(hit, out int gx, out int gz);

        if (gx < GridConfig.FromSize || gx >= GridConfig.ToSize ||
            gz < GridConfig.FromSize || gz >= GridConfig.ToSize)
        {
            Debug.Log($"Click outside grid range → world {hit}  (would be grid ({gx}, {gz}))");
            return;
        }

        Vector3 centre = GridToWorldCenter(gx, gz);
        Debug.Log($"GridSquare clicked: ({gx}, {gz})  |  world centre {centre}  |  hit {hit}");
    }

    void OnRenderObject()
    {
        if (!showGrid || lineMat == null) return;

        lineMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);

        float start = GridConfig.FromSize * CoreConfig.TileSize;
        float end   = GridConfig.ToSize   * CoreConfig.TileSize;

        // Lines on the integers → cell centres sit on the half-integers
        for (int i = GridConfig.FromSize; i <= GridConfig.ToSize; i++)
        {
            bool major = (i % GridConfig.MajorEvery == 0);
            GL.Color(major ? GridConfig.MajorColor : GridConfig.MinorColor);

            float t = i * CoreConfig.TileSize;

            // constant X
            GL.Vertex3(t, GridConfig.HeightOffset, start);
            GL.Vertex3(t, GridConfig.HeightOffset, end);

            // constant Z
            GL.Vertex3(start, GridConfig.HeightOffset, t);
            GL.Vertex3(end,   GridConfig.HeightOffset, t);
        }

        GL.End();
        GL.PopMatrix();
    }

    void OnGUI()
    {
        if (!showLabels || !showGrid) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        WorldToGrid(camPos, out int camGx, out int camGz);

        int radius = GridConfig.LabelRadiusTiles;
        int minX = Mathf.Max(GridConfig.FromSize, camGx - radius);
        int maxX = Mathf.Min(GridConfig.ToSize - 1, camGx + radius);
        int minZ = Mathf.Max(GridConfig.FromSize, camGz - radius);
        int maxZ = Mathf.Min(GridConfig.ToSize - 1, camGz + radius);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 11
        };
        style.normal.textColor = GridConfig.LabelColor;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                int dx = x - camGx;
                int dz = z - camGz;
                if (dx * dx + dz * dz > radius * radius) continue;

                Vector3 world = GridToWorldCenter(x, z);
                world.y += GridConfig.LabelOffset;   // slight lift for readability

                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z <= 0f) continue;

                float guiY = Screen.height - screen.y;
                Rect r = new Rect(screen.x - 30f, guiY - 10f, 60f, 20f);
                GUI.Label(r, $"{x},{z}", style);
            }
        }
    }
}