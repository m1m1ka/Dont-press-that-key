using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BulletSpawner : MonoBehaviour
{
    [SerializeField] private LevelBulletSpawnConfig levelConfig;
    [SerializeField] private bool clearExistingBeforeSpawn = true;

    [Header("Placement")]
    [SerializeField] private Vector3 initialSpawnPosition;
    [SerializeField, Min(0f)] private float xSpacing = 0.12f;
    [SerializeField] private Vector3 spawnRotation;
    [SerializeField] private Transform spawnedBulletsParent;

    private readonly List<GameObject> spawnedBullets = new List<GameObject>();
    private readonly List<ShotGunShellKind> spawnedShellKinds = new List<ShotGunShellKind>();
    private int spawnedLiveShellCount;
    private int spawnedBlankShellCount;

    public IReadOnlyList<GameObject> SpawnedBullets => spawnedBullets;
    public IReadOnlyList<ShotGunShellKind> SpawnedShellKinds => spawnedShellKinds;
    public int SpawnedBulletCount => spawnedBullets.Count;
    public int SpawnedLiveShellCount => spawnedLiveShellCount;
    public int SpawnedBlankShellCount => spawnedBlankShellCount;

    [ContextMenu("Spawn Level Bullets")]
    public void SpawnLevelBullets()
    {
        SpawnLevelBullets(levelConfig);
    }

    public void SetSpawnedBulletsParent(Transform parent)
    {
        spawnedBulletsParent = parent;
    }

    public void SpawnLevelBullets(LevelBulletSpawnConfig config)
    {
        if (clearExistingBeforeSpawn)
        {
            ClearSpawnedBullets();
        }

        if (config == null)
        {
            Debug.LogWarning("Bullet spawner has no level config assigned.", this);
            return;
        }

        int spawnIndex = 0;
        Quaternion rotation = Quaternion.Euler(spawnRotation);
        IReadOnlyList<BulletSpawnEntry> entries = config.BulletEntries;
        for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            BulletSpawnEntry entry = entries[entryIndex];
            if (entry == null || entry.BulletPrefab == null || entry.Count <= 0)
            {
                continue;
            }

            for (int countIndex = 0; countIndex < entry.Count; countIndex++)
            {
                ShotGunShellKind shellKind = entry.ResolveShellKind();
                Vector3 position = initialSpawnPosition + Vector3.right * (xSpacing * spawnIndex);
                GameObject bullet = Instantiate(
                    entry.BulletPrefab,
                    position,
                    rotation,
                    spawnedBulletsParent);

                spawnedBullets.Add(bullet);
                spawnedShellKinds.Add(shellKind);
                AddSpawnedShellCount(shellKind);
                spawnIndex++;
            }
        }
    }

    [ContextMenu("Clear Spawned Bullets")]
    public void ClearSpawnedBullets()
    {
        for (int i = spawnedBullets.Count - 1; i >= 0; i--)
        {
            GameObject bullet = spawnedBullets[i];
            if (bullet == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(bullet);
            }
            else
            {
                DestroyImmediate(bullet);
            }
        }

        spawnedBullets.Clear();
        spawnedShellKinds.Clear();
        spawnedLiveShellCount = 0;
        spawnedBlankShellCount = 0;
    }

    private void AddSpawnedShellCount(ShotGunShellKind shellKind)
    {
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                spawnedLiveShellCount++;
                break;
            case ShotGunShellKind.Blank:
                spawnedBlankShellCount++;
                break;
        }
    }
}
