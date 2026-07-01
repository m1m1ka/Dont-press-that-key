using UnityEngine;

[DisallowMultipleComponent]
public sealed class InspectableItem : MonoBehaviour, IHoverableTarget
{
    [Header("Item Info")]
    [SerializeField] private string displayName = "Inspectable Item";
    [SerializeField, TextArea] private string itemDescription;

    [Header("Outline")]
    [SerializeField] private Shader outlineShader;
    [SerializeField] private Shader outlineMaskShader;
    [SerializeField, ColorUsage(false, true)] private Color outlineColor = new Color(1f, 0.55f, 0.08f, 1f);
    [SerializeField, Range(0.0005f, 0.05f)] private float outlineWidth = 0.008f;
    [SerializeField] private bool ensureCollider = true;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material[] outlineMaterials;
    private Material outlineMaskMaterial;
    private bool highlighted;

    private static readonly Vector4[] OutlineDirections =
    {
        new Vector4(1f, 0f, 0f, 0f),
        new Vector4(0.9238795f, 0.3826834f, 0f, 0f),
        new Vector4(0.7071068f, 0.7071068f, 0f, 0f),
        new Vector4(0.3826834f, 0.9238795f, 0f, 0f),
        new Vector4(0f, 1f, 0f, 0f),
        new Vector4(-0.3826834f, 0.9238795f, 0f, 0f),
        new Vector4(-0.7071068f, 0.7071068f, 0f, 0f),
        new Vector4(-0.9238795f, 0.3826834f, 0f, 0f),
        new Vector4(-1f, 0f, 0f, 0f),
        new Vector4(-0.9238795f, -0.3826834f, 0f, 0f),
        new Vector4(-0.7071068f, -0.7071068f, 0f, 0f),
        new Vector4(-0.3826834f, -0.9238795f, 0f, 0f),
        new Vector4(0f, -1f, 0f, 0f),
        new Vector4(0.3826834f, -0.9238795f, 0f, 0f),
        new Vector4(0.7071068f, -0.7071068f, 0f, 0f),
        new Vector4(0.9238795f, -0.3826834f, 0f, 0f),
    };

    public string DisplayName => displayName;
    public string ItemName => displayName;
    public string ItemDescription => itemDescription;
    public UnityEngine.Object Owner => this;
    public bool CanHover => enabled;

    public void SetHovered(bool hovered)
    {
        SetHighlighted(hovered);
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = GetBaseMaterials(renderers[i]);
        }

        if (ensureCollider)
        {
            EnsureClickableCollider();
        }

        InitializeOutline(false);
    }

    public void SetHighlighted(bool visible)
    {
        if (highlighted == visible || outlineMaterials == null || outlineMaterials.Length == 0 || outlineMaskMaterial == null)
        {
            return;
        }

        highlighted = visible;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            originalMaterials[i] = GetBaseMaterials(targetRenderer);
            if (!visible)
            {
                targetRenderer.sharedMaterials = originalMaterials[i];
                continue;
            }

            Material[] sourceMaterials = originalMaterials[i];
            Material[] highlightedMaterials = new Material[sourceMaterials.Length + 1 + outlineMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                highlightedMaterials[materialIndex] = sourceMaterials[materialIndex];
            }

            highlightedMaterials[sourceMaterials.Length] = outlineMaskMaterial;
            for (int outlineIndex = 0; outlineIndex < outlineMaterials.Length; outlineIndex++)
            {
                highlightedMaterials[sourceMaterials.Length + 1 + outlineIndex] = outlineMaterials[outlineIndex];
            }

            targetRenderer.sharedMaterials = highlightedMaterials;
        }
    }

    private Material[] GetBaseMaterials(Renderer targetRenderer)
    {
        if (targetRenderer == null)
        {
            return new Material[0];
        }

        Material[] sharedMaterials = targetRenderer.sharedMaterials;
        int baseMaterialCount = 0;
        for (int i = 0; i < sharedMaterials.Length; i++)
        {
            if (!IsRuntimeOutlineMaterial(sharedMaterials[i]))
            {
                baseMaterialCount++;
            }
        }

        Material[] baseMaterials = new Material[baseMaterialCount];
        int baseMaterialIndex = 0;
        for (int i = 0; i < sharedMaterials.Length; i++)
        {
            Material material = sharedMaterials[i];
            if (IsRuntimeOutlineMaterial(material))
            {
                continue;
            }

            baseMaterials[baseMaterialIndex] = material;
            baseMaterialIndex++;
        }

        return baseMaterials;
    }

    private bool IsRuntimeOutlineMaterial(Material material)
    {
        return material == outlineMaskMaterial || IsRuntimeOutlineDirectionMaterial(material);
    }

    private bool IsRuntimeOutlineDirectionMaterial(Material material)
    {
        if (outlineMaterials == null || material == null)
        {
            return false;
        }

        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            if (material == outlineMaterials[i])
            {
                return true;
            }
        }

        return false;
    }

    public void ApplyOutlineDefaults(Shader defaultOutlineShader, Shader defaultOutlineMaskShader)
    {
        if (outlineShader == null)
        {
            outlineShader = defaultOutlineShader;
        }

        if (outlineMaskShader == null)
        {
            outlineMaskShader = defaultOutlineMaskShader;
        }

        InitializeOutline(false);
    }

    public void Configure(string itemDisplayName, Shader defaultOutlineShader, Shader defaultOutlineMaskShader)
    {
        if (!string.IsNullOrWhiteSpace(itemDisplayName))
        {
            displayName = itemDisplayName;
        }

        if (defaultOutlineShader != null)
        {
            outlineShader = defaultOutlineShader;
        }

        if (defaultOutlineMaskShader != null)
        {
            outlineMaskShader = defaultOutlineMaskShader;
        }

        InitializeOutline(false);
    }

    private void InitializeOutline(bool warnIfMissing)
    {
        if (outlineMaterials != null && outlineMaterials.Length > 0 && outlineMaskMaterial != null)
        {
            return;
        }

        if (outlineShader == null || outlineMaskShader == null)
        {
            if (warnIfMissing)
            {
                Debug.LogWarning("Inspectable item outline shaders are not assigned.", this);
            }

            return;
        }

        outlineMaterials = new Material[OutlineDirections.Length];
        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            Material outlineMaterial = new Material(outlineShader)
            {
                name = $"{name} Outline {i} (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
            outlineMaterial.SetVector("_OutlineDirection", OutlineDirections[i]);
            outlineMaterials[i] = outlineMaterial;
        }

        outlineMaskMaterial = new Material(outlineMaskShader)
        {
            name = $"{name} Outline Mask (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private void EnsureClickableCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            foreach (Collider itemCollider in colliders)
            {
                itemCollider.enabled = true;
            }

            return;
        }

        if (renderers.Length == 0)
        {
            Debug.LogWarning("Inspectable item has no renderer from which to create a click collider.", this);
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Bounds localBounds = new Bounds(transform.InverseTransformPoint(worldBounds.center), Vector3.zero);
        Vector3 extent = worldBounds.extents;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = worldBounds.center + Vector3.Scale(extent, new Vector3(x, y, z));
                    localBounds.Encapsulate(transform.InverseTransformPoint(corner));
                }
            }
        }

        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.center = localBounds.center;
        boxCollider.size = localBounds.size;
    }

    private void OnDisable()
    {
        SetHighlighted(false);
    }

    private void OnDestroy()
    {
        if (outlineMaterials != null)
        {
            foreach (Material outlineMaterial in outlineMaterials)
            {
                if (outlineMaterial != null)
                {
                    Destroy(outlineMaterial);
                }
            }
        }

        if (outlineMaskMaterial != null)
        {
            Destroy(outlineMaskMaterial);
        }
    }
}
