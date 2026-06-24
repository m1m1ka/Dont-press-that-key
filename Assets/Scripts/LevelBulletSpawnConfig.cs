using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Level Bullet Spawn Config",
    menuName = "Dont Press That Key/Level Bullet Spawn Config")]
public sealed class LevelBulletSpawnConfig : ScriptableObject
{
    [SerializeField] private List<BulletSpawnEntry> bulletEntries = new List<BulletSpawnEntry>();

    public IReadOnlyList<BulletSpawnEntry> BulletEntries => bulletEntries;
}

[Serializable]
public sealed class BulletSpawnEntry
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ShotGunShellKind shellKind = ShotGunShellKind.Live;
    [SerializeField, Min(0)] private int count = 1;

    public GameObject BulletPrefab => bulletPrefab;
    public ShotGunShellKind ShellKind => shellKind;
    public int Count => count;

    public ShotGunShellKind ResolveShellKind()
    {
        if (bulletPrefab != null && bulletPrefab.TryGetComponent(out ShotGunShellIdentity shellIdentity))
        {
            return shellIdentity.ShellKind;
        }

        return shellKind;
    }
}
