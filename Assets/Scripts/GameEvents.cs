using UnityEngine;
using System.Collections.Generic;

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

public enum NotificationMessageKind
{
    AmmoDepleted,
    TurnChanged
}

public struct NotificationFlowStartedEvent
{
    public NotificationFlowStartedEvent(NotificationMessageKind messageKind, string message)
    {
        MessageKind = messageKind;
        Message = message;
    }

    public NotificationMessageKind MessageKind { get; }
    public string Message { get; }
}

public struct NotificationCameraArrivedEvent
{
    public NotificationCameraArrivedEvent(NotificationMessageKind messageKind, string message)
    {
        MessageKind = messageKind;
        Message = message;
    }

    public NotificationMessageKind MessageKind { get; }
    public string Message { get; }
}

public struct NotificationFlowCompletedEvent
{
    public NotificationFlowCompletedEvent(NotificationMessageKind messageKind, string message)
    {
        MessageKind = messageKind;
        Message = message;
    }

    public NotificationMessageKind MessageKind { get; }
    public string Message { get; }
}

public struct ReloadStartedEvent
{
}

public struct ReloadCompletedEvent
{
}

public struct ShotGunShellsLoadedEvent
{
    public ShotGunShellsLoadedEvent(ShotGunState shotGunState, IReadOnlyList<ShotGunShellKind> shellKinds)
    {
        ShotGunState = shotGunState;
        ShellKinds = shellKinds;
    }

    public ShotGunState ShotGunState { get; }
    public IReadOnlyList<ShotGunShellKind> ShellKinds { get; }
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

public struct UseFocusedItemButtonClickedEvent
{
}

public struct RevealCurrentShotGunShellRequestedEvent
{
}

public struct RevealCurrentShotGunShellConsumedEvent
{
    public RevealCurrentShotGunShellConsumedEvent(UnityEngine.Object source)
    {
        Source = source;
    }

    public UnityEngine.Object Source { get; }
}

public struct SwapCurrentAndNextShotGunShellRequestedEvent
{
}

public struct InvertCurrentShotGunShellRequestedEvent
{
}

public struct FocusedTargetRemovedEvent
{
    public FocusedTargetRemovedEvent(UnityEngine.Object focusTarget)
    {
        FocusTarget = focusTarget;
    }

    public UnityEngine.Object FocusTarget { get; }
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
        : this(shellKind, default, false)
    {
    }

    public ShotGunFiredEvent(ShotGunShellKind shellKind, ShotGunFireEffectContext fireContext)
        : this(shellKind, fireContext, true)
    {
    }

    private ShotGunFiredEvent(ShotGunShellKind shellKind, ShotGunFireEffectContext fireContext, bool hasFireContext)
    {
        ShellKind = shellKind;
        FireContext = fireContext;
        HasFireContext = hasFireContext;
    }

    public ShotGunShellKind ShellKind { get; }
    public ShotGunFireEffectContext FireContext { get; }
    public bool HasFireContext { get; }
}

public struct PlayerHitScreenEffectStartedEvent
{
}

public struct PlayerHitScreenEffectCompletedEvent
{
}

public enum GameCharacter
{
    Player,
    Enemy
}

public struct ShotGunHitResolvedEvent
{
    public ShotGunHitResolvedEvent(
        ShotGunShellKind shellKind,
        ShotGunFireEffectContext fireContext,
        GameCharacter target,
        int damage)
    {
        ShellKind = shellKind;
        FireContext = fireContext;
        Target = target;
        Damage = damage;
    }

    public ShotGunShellKind ShellKind { get; }
    public ShotGunFireEffectContext FireContext { get; }
    public GameCharacter Target { get; }
    public int Damage { get; }
}

public struct CharacterHealthChangedEvent
{
    public CharacterHealthChangedEvent(GameCharacter character, int currentHealth, int maxHealth, int delta)
    {
        Character = character;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        Delta = delta;
    }

    public GameCharacter Character { get; }
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public int Delta { get; }
}

public struct CharacterDiedEvent
{
    public CharacterDiedEvent(GameCharacter character)
    {
        Character = character;
    }

    public GameCharacter Character { get; }
}

public struct LevelProgressChangedEvent
{
    public LevelProgressChangedEvent(int currentLevel, int completedLevelCount)
    {
        CurrentLevel = currentLevel;
        CompletedLevelCount = completedLevelCount;
    }

    public int CurrentLevel { get; }
    public int CompletedLevelCount { get; }
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
