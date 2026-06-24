using System;
using UnityEngine;
using UnityEngine.Serialization;

[Obsolete("Use FocusableObject plus ShotGunState. This component only migrates older scenes at runtime.")]
[DisallowMultipleComponent]
public sealed class ShotGunFocusInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField, FormerlySerializedAs("revolver")] private Transform shotGun;

    [Header("Interaction")]
    [SerializeField] private string itemDisplayName = "ShotGun";
    [SerializeField] private bool ensureCollider = true;

    [Header("Hover Outline")]
    [SerializeField] private Shader outlineShader;
    [SerializeField] private Shader outlineMaskShader;
    [SerializeField, ColorUsage(false, true)] private Color outlineColor = new Color(1f, 0.55f, 0.08f, 1f);
    [SerializeField, Range(0.0005f, 0.05f)] private float outlineWidth = 0.008f;

    [Header("Focused Transform (camera space)")]
    [SerializeField, Min(0.05f)] private float focusDistance = 0.8f;
    [SerializeField] private Vector3 focusPosition = new Vector3(0.2f, -0.12f, 0f);
    [SerializeField] private Vector3 focusRotation = new Vector3(5f, 100f, 5f);
    [SerializeField] private Vector3 focusScale = new Vector3(1.5f, 1.5f, 1.5f);

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.45f;
    [SerializeField] private AnimationCurve transitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    private void Awake()
    {
        if (shotGun == null)
        {
            shotGun = transform;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        FocusableObject focusableObject = shotGun.GetComponent<FocusableObject>();
        if (focusableObject == null)
        {
            focusableObject = shotGun.gameObject.AddComponent<FocusableObject>();
        }

        focusableObject.Configure(
            playerCamera,
            shotGun,
            true,
            focusDistance,
            focusPosition,
            focusRotation,
            focusScale,
            transitionDuration,
            transitionCurve);

        InspectableItem inspectableItem = shotGun.GetComponent<InspectableItem>();
        if (inspectableItem == null)
        {
            inspectableItem = shotGun.gameObject.AddComponent<InspectableItem>();
        }

        inspectableItem.Configure(itemDisplayName, outlineShader, outlineMaskShader);

        if (ensureCollider && shotGun.GetComponentInChildren<Collider>() == null)
        {
            AddFallbackCollider();
        }

        if (shotGun.GetComponent<ShotGunState>() == null)
        {
            shotGun.gameObject.AddComponent<ShotGunState>();
        }
    }

    private void AddFallbackCollider()
    {
        Renderer[] renderers = shotGun.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Bounds localBounds = new Bounds(shotGun.InverseTransformPoint(worldBounds.center), Vector3.zero);
        Vector3 extent = worldBounds.extents;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = worldBounds.center + Vector3.Scale(extent, new Vector3(x, y, z));
                    localBounds.Encapsulate(shotGun.InverseTransformPoint(corner));
                }
            }
        }

        BoxCollider clickCollider = shotGun.gameObject.AddComponent<BoxCollider>();
        clickCollider.center = localBounds.center;
        clickCollider.size = localBounds.size;
    }
}
