using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class ShotGunShellEjectEffectSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform ejectPoint;
    [SerializeField] private GameObject shellCasingPrefab;
    [SerializeField] private GameObject liveShellCasingPrefab;
    [SerializeField] private GameObject blankShellCasingPrefab;
    [SerializeField] private Transform spawnedParent;
    [SerializeField] private Vector3 localSpawnPositionOffset;
    [SerializeField] private Vector3 localSpawnEulerOffset;
    [SerializeField, Min(0f)] private float destroyAfterSeconds = 5f;

    [Header("Motion")]
    [SerializeField] private bool addRigidbodyIfMissing = true;
    [SerializeField] private Transform velocityReference;
    [SerializeField, FormerlySerializedAs("ejectVelocity")] private Vector3 localEjectVelocity = new Vector3(1.2f, 0.35f, -0.2f);
    [SerializeField] private Vector3 localRandomVelocity = new Vector3(0.25f, 0.12f, 0.25f);
    [SerializeField, FormerlySerializedAs("angularVelocity")] private Vector3 localAngularVelocity = new Vector3(0f, 18f, 6f);
    [SerializeField] private Vector3 localRandomAngularVelocity = new Vector3(3f, 6f, 3f);

    [Header("Testing")]
    [SerializeField] private bool enableKeyboardTest = true;
    [SerializeField] private KeyCode testEjectKey = KeyCode.F;
    [SerializeField] private ShotGunShellKind testShellKind = ShotGunShellKind.Live;

    private IDisposable shellEjectSubscription;

    private void OnEnable()
    {
        shellEjectSubscription = GameEventBus.Subscribe<ShotGunShellEjectRequestedEvent>(HandleShellEjectRequested);
    }

    private void OnDisable()
    {
        shellEjectSubscription?.Dispose();
        shellEjectSubscription = null;
    }

    private void Update()
    {
        if (!enableKeyboardTest || !Input.GetKeyDown(testEjectKey))
        {
            return;
        }

        SpawnShellCasing(testShellKind);
    }

    private void HandleShellEjectRequested(ShotGunShellEjectRequestedEvent evt)
    {
        SpawnShellCasing(evt.ShellKind);
    }

    private void SpawnShellCasing(ShotGunShellKind shellKind)
    {
        GameObject prefab = GetPrefab(shellKind);
        if (prefab == null)
        {
            Debug.LogWarning("Shell eject effect has no shell casing prefab assigned.", this);
            return;
        }

        Transform spawnTransform = ejectPoint != null ? ejectPoint : transform;
        Vector3 spawnPosition = spawnTransform.TransformPoint(localSpawnPositionOffset);
        Quaternion spawnRotation = spawnTransform.rotation * Quaternion.Euler(localSpawnEulerOffset);
        GameObject casing = Instantiate(prefab, spawnPosition, spawnRotation, spawnedParent);

        ApplyMotion(casing, spawnTransform);

        if (destroyAfterSeconds > 0f)
        {
            Destroy(casing, destroyAfterSeconds);
        }
    }

    private GameObject GetPrefab(ShotGunShellKind shellKind)
    {
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                return liveShellCasingPrefab != null ? liveShellCasingPrefab : shellCasingPrefab;
            case ShotGunShellKind.Blank:
                return blankShellCasingPrefab != null ? blankShellCasingPrefab : shellCasingPrefab;
            default:
                return shellCasingPrefab;
        }
    }

    private void ApplyMotion(GameObject casing, Transform spawnTransform)
    {
        Rigidbody body = casing.GetComponent<Rigidbody>();
        if (body == null && addRigidbodyIfMissing)
        {
            body = casing.AddComponent<Rigidbody>();
        }

        if (body == null)
        {
            return;
        }

        Transform referenceTransform = velocityReference != null ? velocityReference : spawnTransform;
        body.velocity = referenceTransform.TransformDirection(localEjectVelocity + GetRandomVector(localRandomVelocity));
        body.angularVelocity = referenceTransform.TransformDirection(localAngularVelocity + GetRandomVector(localRandomAngularVelocity));
    }

    private static Vector3 GetRandomVector(Vector3 range)
    {
        return new Vector3(
            UnityEngine.Random.Range(-range.x, range.x),
            UnityEngine.Random.Range(-range.y, range.y),
            UnityEngine.Random.Range(-range.z, range.z));
    }
}
