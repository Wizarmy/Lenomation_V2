using UnityEngine;

[ExecuteAlways]
public class Package : MonoBehaviour
{
    [Header("Item")]
    public ItemId itemId = ItemId.None;

    public void SetItem(ItemId id)
    {
        itemId = id;
        SetColor(ItemConfig.Color(id));
    }
    
    [Header("Colour")]
    public Color color = PackageConfig.DefaultColor;

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

    public void SetColor(Color c)
    {
        color = c;
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

        for (int i = 0; i < 6; i++)
            SetFace(i, color, i == PackageConfig.FaceTop ? topImage : null);

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

    void SetFace(int index, Color faceColor, Texture tex)
    {
        Material mat = faceMats[index];
        if (mat == null) return;

        Color tint = tex != null
            ? Color.Lerp(Color.white, faceColor, topImageTint)
            : faceColor;

        mat.color = tint;
        mat.mainTexture = tex;
    }

    static Material NewUnlit(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        return new Material(shader) { color = c };
    }
}