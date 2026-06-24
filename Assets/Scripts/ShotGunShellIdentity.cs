using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShotGunShellIdentity : MonoBehaviour
{
    [SerializeField] private ShotGunShellKind shellKind = ShotGunShellKind.Live;

    public ShotGunShellKind ShellKind => shellKind;
}
