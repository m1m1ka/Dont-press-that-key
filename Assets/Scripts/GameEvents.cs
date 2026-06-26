using UnityEngine;

public struct GameFlowStartedEvent
{
}

public struct PlayerInputLockChangedEvent
{
    public PlayerInputLockChangedEvent(bool locked)
    {
        Locked = locked;
    }

    public bool Locked { get; }
}

public struct AmmoBoxSpawnedEvent
{
    public AmmoBoxSpawnedEvent(GameObject ammoBox)
    {
        AmmoBox = ammoBox;
    }

    public GameObject AmmoBox { get; }
}

public struct BulletsSpawnRequestedEvent
{
}

public struct BulletsSpawnedEvent
{
}

public struct ReloadStartedEvent
{
}

public struct ReloadCompletedEvent
{
}

public struct ReloadButtonClickedEvent
{
}

public struct ShootPlayerButtonClickedEvent
{
}

public struct ShootEnemyButtonClickedEvent
{
}

public struct ShootPlayerStartedEvent
{
}

public struct ShootEnemyStartedEvent
{
}

public struct ShotGunFiredEvent
{
    public ShotGunFiredEvent(ShotGunShellKind shellKind)
    {
        ShellKind = shellKind;
    }

    public ShotGunShellKind ShellKind { get; }
}

public enum ShotGunFireEffectContext
{
    PlayerShootsPlayer,
    PlayerShootsEnemy,
    EnemyShootsPlayer,
    EnemyShootsEnemy
}

public struct ShotGunShellEjectRequestedEvent
{
    public ShotGunShellEjectRequestedEvent(ShotGunShellKind shellKind, Transform shotGun)
    {
        ShellKind = shellKind;
        ShotGun = shotGun;
    }

    public ShotGunShellKind ShellKind { get; }
    public Transform ShotGun { get; }
}

public struct ShotGunShellLoadStartedEvent
{
    public ShotGunShellLoadStartedEvent(Transform shotGun, int shellIndex, int shellCount)
    {
        ShotGun = shotGun;
        ShellIndex = shellIndex;
        ShellCount = shellCount;
    }

    public Transform ShotGun { get; }
    public int ShellIndex { get; }
    public int ShellCount { get; }
}

public struct ShotGunShellLoadCompletedEvent
{
    public ShotGunShellLoadCompletedEvent(Transform shotGun, int shellIndex, int shellCount)
    {
        ShotGun = shotGun;
        ShellIndex = shellIndex;
        ShellCount = shellCount;
    }

    public Transform ShotGun { get; }
    public int ShellIndex { get; }
    public int ShellCount { get; }
}

public struct ShotGunBoltPulledEvent
{
    public ShotGunBoltPulledEvent(Transform shotGun, bool ejectsShell)
    {
        ShotGun = shotGun;
        EjectsShell = ejectsShell;
    }

    public Transform ShotGun { get; }
    public bool EjectsShell { get; }
}

public enum ShotGunMovePurpose
{
    ReloadMove,
    ReloadReturn,
    ShootAimMove,
    EjectMove,
    EjectReturn
}

public struct ShotGunMoveStartedEvent
{
    public ShotGunMoveStartedEvent(
        Transform shotGun,
        ShotGunMovePurpose purpose,
        Vector3 fromPosition,
        Vector3 toPosition,
        float duration)
    {
        ShotGun = shotGun;
        Purpose = purpose;
        FromPosition = fromPosition;
        ToPosition = toPosition;
        Duration = duration;
    }

    public Transform ShotGun { get; }
    public ShotGunMovePurpose Purpose { get; }
    public Vector3 FromPosition { get; }
    public Vector3 ToPosition { get; }
    public float Duration { get; }
}

public struct ShotGunMoveCompletedEvent
{
    public ShotGunMoveCompletedEvent(Transform shotGun, ShotGunMovePurpose purpose)
    {
        ShotGun = shotGun;
        Purpose = purpose;
    }

    public Transform ShotGun { get; }
    public ShotGunMovePurpose Purpose { get; }
}

public struct ShootPlayerCompletedEvent
{
}

public struct ShootEnemyCompletedEvent
{
}

public struct ShotGunLoadedStateChangedEvent
{
    public ShotGunLoadedStateChangedEvent(ShotGunState shotGunState, bool isLoaded)
        : this(shotGunState, isLoaded, 0, 0)
    {
    }

    public ShotGunLoadedStateChangedEvent(
        ShotGunState shotGunState,
        bool isLoaded,
        int loadedLiveShellCount,
        int loadedBlankShellCount)
    {
        ShotGunState = shotGunState;
        IsLoaded = isLoaded;
        LoadedLiveShellCount = loadedLiveShellCount;
        LoadedBlankShellCount = loadedBlankShellCount;
    }

    public ShotGunState ShotGunState { get; }
    public bool IsLoaded { get; }
    public int LoadedLiveShellCount { get; }
    public int LoadedBlankShellCount { get; }
}

public struct InspectableHoveredEvent
{
    public InspectableHoveredEvent(InspectableItem item)
    {
        Item = item;
    }

    public InspectableItem Item { get; }
}

public struct InspectableHoverClearedEvent
{
}

public struct FocusStateChangedEvent
{
    public FocusStateChangedEvent(bool focused)
        : this(focused, null)
    {
    }

    public FocusStateChangedEvent(bool focused, UnityEngine.Object focusTarget)
    {
        Focused = focused;
        FocusTarget = focusTarget;
    }

    public bool Focused { get; }
    public UnityEngine.Object FocusTarget { get; }
}

public struct SoundEffectRequestedEvent
{
    public SoundEffectRequestedEvent(string soundId)
        : this(soundId, null, 1f)
    {
    }

    public SoundEffectRequestedEvent(string soundId, Vector3? worldPosition)
        : this(soundId, worldPosition, 1f)
    {
    }

    public SoundEffectRequestedEvent(string soundId, Vector3? worldPosition, float volumeScale)
    {
        SoundId = soundId;
        WorldPosition = worldPosition;
        VolumeScale = volumeScale;
    }

    public string SoundId { get; }
    public Vector3? WorldPosition { get; }
    public float VolumeScale { get; }
}
