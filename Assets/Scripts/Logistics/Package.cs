using UnityEngine;

/// <summary>
/// Cube package. Six faces, independent colours. Optional texture on the top face.
/// Materials are instanced so Inspector / runtime changes do not dirty the prefab.
/// </summary>
[ExecuteAlways]
public class Package : MonoBehaviour
{
    [Header("Faces")]
    public Color topColor    = PackageConfig.DefaultTop;
    public Color bottomColor = PackageConfig.DefaultBottom;
    public Color frontColor  = PackageConfig.DefaultFront;
    public Color backColor   = PackageConfig.DefaultBack;
    public Color leftColor   = PackageConfig.DefaultLeft;
    public Color rightColor  = PackageConfig.DefaultRight;

    [Header("Top image (optional)")]
    public Texture topImage;
    [Range(0f, 1f)]
    public float topImageTint = 1f;

    MeshRenderer cachedRenderer;
    Material[] faceMats;
    bool instanced;

    void Awake()
    {
        EnsureInstances();
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    public void SetFaceColor(int face, Color color)
    {
        switch (face)
        {
            case PackageConfig.FaceTop:    topColor    = color; break;
            case PackageConfig.FaceBottom: bottomColor = color; break;
            case PackageConfig.FaceFront:  frontColor  = color; break;
            case PackageConfig.FaceBack:   backColor   = color; break;
            case PackageConfig.FaceLeft:   leftColor   = color; break;
            case PackageConfig.FaceRight:  rightColor  = color; break;
        }
        Apply();
    }

    public void SetTopImage(Texture texture)
    {
        topImage = texture;
        Apply();
    }

    public void Apply()
    {
        EnsureInstances();
        if (faceMats == null || faceMats.Length < 6) return;

        SetFace(PackageConfig.FaceTop,    topColor,    topImage);
        SetFace(PackageConfig.FaceBottom, bottomColor, null);
        SetFace(PackageConfig.FaceFront,  frontColor,  null);
        SetFace(PackageConfig.FaceBack,   backColor,   null);
        SetFace(PackageConfig.FaceLeft,   leftColor,   null);
        SetFace(PackageConfig.FaceRight,  rightColor,  null);

        if (cachedRenderer != null)
            cachedRenderer.sharedMaterials = faceMats;
    }

    void EnsureInstances()
    {
        if (cachedRenderer == null)
            cachedRenderer = GetComponent<MeshRenderer>();
        if (cachedRenderer == null) return;

        var shared = cachedRenderer.sharedMaterials;
        if (shared == null || shared.Length < 6) return;

        if (!instanced || faceMats == null || faceMats.Length != 6)
        {
            faceMats = new Material[6];
            for (int i = 0; i < 6; i++)
            {
                Material src = shared[Mathf.Clamp(i, 0, shared.Length - 1)];
                faceMats[i] = src != null ? new Material(src) : NewUnlit(Color.white);
                faceMats[i].name = $"PackageFace_{i}";
            }
            instanced = true;
        }
    }

    void SetFace(int index, Color color, Texture tex)
    {
        Material mat = faceMats[index];
        if (mat == null) return;

        Color tint = tex != null
            ? Color.Lerp(Color.white, color, topImageTint)
            : color;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", tint);
        mat.color = tint;

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
    }

    static Material NewUnlit(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return new Material(shader) { color = color };
    }
}