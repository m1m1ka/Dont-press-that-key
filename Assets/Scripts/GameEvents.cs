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
    {
        Focused = focused;
    }

    public bool Focused { get; }
}
