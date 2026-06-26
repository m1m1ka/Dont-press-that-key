using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SoundEffectManager : MonoBehaviour
{
    public enum GameEventSound
    {
        // Unity serializes enum fields as integers. Keep these values stable;
        // add future events with new values instead of inserting/reordering.
        GameFlowStarted = 0,
        PlayerInputLocked = 1,
        PlayerInputUnlocked = 2,
        AmmoBoxSpawned = 3,
        BulletsSpawnRequested = 4,
        BulletsSpawned = 5,
        ReloadButtonClicked = 6,
        ReloadStarted = 7,
        ReloadCompleted = 8,
        ShootPlayerButtonClicked = 9,
        ShootEnemyButtonClicked = 10,
        ShootPlayerStarted = 11,
        ShootEnemyStarted = 12,
        ShotGunFiredLive = 13,
        ShotGunFiredBlank = 14,
        ShotGunShellEjectRequested = 15,
        ShotGunShellLoadStarted = 16,
        ShotGunShellLoadCompleted = 17,
        ShotGunBoltPulled = 18,
        ShotGunBoltPulledWithShellEject = 19,
        ShotGunBoltPulledWithoutShellEject = 20,
        ShotGunMoveStarted = 21,
        ShotGunMoveCompleted = 22,
        ShotGunReloadMoveStarted = 23,
        ShotGunReloadReturnMoveStarted = 24,
        ShotGunShootAimMoveStarted = 25,
        ShotGunEjectMoveStarted = 26,
        ShotGunEjectReturnMoveStarted = 27,
        ShootPlayerCompleted = 28,
        ShootEnemyCompleted = 29,
        InspectableHovered = 30,
        InspectableHoverCleared = 31,
        ShotGunFocusStarted = 32,
        ShotGunFocusEnded = 33
    }

    [Serializable]
    private sealed class SoundEffect
    {
        [SerializeField] private string id;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField] private bool spatial;

        private float lastPlayedTime = float.NegativeInfinity;

        public string Id => id;
        public bool HasClips => clips != null && clips.Length > 0;
        public bool Spatial => spatial;

        public bool CanPlay()
        {
            return cooldown <= 0f || Time.unscaledTime >= lastPlayedTime + cooldown;
        }

        public AudioClip GetClip()
        {
            if (!HasClips)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                return clips[0];
            }

            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        public float GetVolume(float volumeScale)
        {
            return Mathf.Clamp01(volume * volumeScale);
        }

        public float GetPitch()
        {
            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            if (Mathf.Approximately(minPitch, maxPitch))
            {
                return minPitch;
            }

            return UnityEngine.Random.Range(minPitch, maxPitch);
        }

        public void MarkPlayed()
        {
            lastPlayedTime = Time.unscaledTime;
        }
    }

    [Serializable]
    private sealed class GameEventSoundBinding
    {
        [SerializeField] private GameEventSound gameEvent;
        [SerializeField] private string soundId;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;

        public GameEventSound GameEvent => gameEvent;
        public string SoundId => soundId;
        public float VolumeScale => volumeScale;
    }

    public static SoundEffectManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sourcePrefab;
    [SerializeField, Min(1)] private int initialPoolSize = 8;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Sound Library")]
    [SerializeField] private SoundEffect[] sounds;

    [Header("Game Event Bindings")]
    [SerializeField] private GameEventSoundBinding[] gameEventBindings;

    private readonly Dictionary<string, SoundEffect> soundsById = new Dictionary<string, SoundEffect>();
    private readonly Dictionary<GameEventSound, List<GameEventSoundBinding>> bindingsByEvent =
        new Dictionary<GameEventSound, List<GameEventSoundBinding>>();
    private readonly Queue<AudioSource> availableSources = new Queue<AudioSource>();
    private readonly List<AudioSource> activeSources = new List<AudioSource>();
    private readonly List<IDisposable> subscriptions = new List<IDisposable>();

    private AudioSource fallbackSourcePrefab;

    public float MasterVolume
    {
        get => masterVolume;
        set => masterVolume = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        RebuildLookups();
        WarmPool();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        ClearSubscriptions();
    }

    private void OnDestroy()
    {
        ClearSubscriptions();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        RecycleFinishedSources();
    }

    [ContextMenu("Rebuild Sound Lookup")]
    public void RebuildLookups()
    {
        soundsById.Clear();
        if (sounds != null)
        {
            for (int i = 0; i < sounds.Length; i++)
            {
                SoundEffect sound = sounds[i];
                if (sound == null || string.IsNullOrWhiteSpace(sound.Id))
                {
                    continue;
                }

                soundsById[sound.Id] = sound;
            }
        }

        bindingsByEvent.Clear();
        if (gameEventBindings == null)
        {
            return;
        }

        for (int i = 0; i < gameEventBindings.Length; i++)
        {
            GameEventSoundBinding binding = gameEventBindings[i];
            if (binding == null || string.IsNullOrWhiteSpace(binding.SoundId))
            {
                continue;
            }

            if (!bindingsByEvent.TryGetValue(binding.GameEvent, out List<GameEventSoundBinding> eventBindings))
            {
                eventBindings = new List<GameEventSoundBinding>();
                bindingsByEvent.Add(binding.GameEvent, eventBindings);
            }

            eventBindings.Add(binding);
        }
    }

    public void Play(string soundId)
    {
        Play(soundId, null, 1f);
    }

    public void Play(string soundId, float volumeScale)
    {
        Play(soundId, null, volumeScale);
    }

    public void PlayAtPosition(string soundId, Vector3 worldPosition)
    {
        Play(soundId, worldPosition, 1f);
    }

    public void PlayAtPosition(string soundId, Vector3 worldPosition, float volumeScale)
    {
        Play(soundId, worldPosition, volumeScale);
    }

    public void PlayClip(AudioClip clip)
    {
        PlayClip(clip, null, 1f, 1f, false);
    }

    public void PlayClip(AudioClip clip, float volumeScale)
    {
        PlayClip(clip, null, volumeScale, 1f, false);
    }

    public void PlayClipAtPosition(AudioClip clip, Vector3 worldPosition)
    {
        PlayClip(clip, worldPosition, 1f, 1f, true);
    }

    public void PlayClipAtPosition(AudioClip clip, Vector3 worldPosition, float volumeScale)
    {
        PlayClip(clip, worldPosition, volumeScale, 1f, true);
    }

    public void Play(string soundId, Vector3? worldPosition, float volumeScale)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            return;
        }

        if (!soundsById.TryGetValue(soundId, out SoundEffect sound))
        {
            Debug.LogWarning($"SoundEffectManager has no sound with id '{soundId}'.", this);
            return;
        }

        if (!sound.HasClips || !sound.CanPlay())
        {
            return;
        }

        AudioClip clip = sound.GetClip();
        if (clip == null)
        {
            return;
        }

        PlayClip(clip, worldPosition, sound.GetVolume(volumeScale), sound.GetPitch(), sound.Spatial || worldPosition.HasValue);
        sound.MarkPlayed();
    }

    public static void Request(string soundId)
    {
        GameEventBus.Publish(new SoundEffectRequestedEvent(soundId));
    }

    public static void RequestAtPosition(string soundId, Vector3 worldPosition)
    {
        GameEventBus.Publish(new SoundEffectRequestedEvent(soundId, worldPosition));
    }

    private void PlayClip(AudioClip clip, Vector3? worldPosition, float volumeScale, float pitch, bool spatial)
    {
        if (clip == null || masterVolume <= 0f || volumeScale <= 0f)
        {
            return;
        }

        AudioSource source = GetSource();
        source.transform.position = worldPosition ?? transform.position;
        source.clip = clip;
        source.volume = Mathf.Clamp01(volumeScale) * masterVolume;
        source.pitch = pitch;
        source.spatialBlend = spatial ? 1f : 0f;
        source.gameObject.SetActive(true);
        source.Play();
        activeSources.Add(source);
    }

    private AudioSource GetSource()
    {
        if (availableSources.Count > 0)
        {
            return availableSources.Dequeue();
        }

        return CreateSource();
    }

    private AudioSource CreateSource()
    {
        AudioSource prefab = sourcePrefab != null ? sourcePrefab : GetFallbackSourcePrefab();
        AudioSource source = Instantiate(prefab, transform);
        source.playOnAwake = false;
        source.loop = false;
        source.gameObject.SetActive(false);
        return source;
    }

    private AudioSource GetFallbackSourcePrefab()
    {
        if (fallbackSourcePrefab != null)
        {
            return fallbackSourcePrefab;
        }

        GameObject sourceObject = new GameObject("SFX Source Prefab");
        sourceObject.hideFlags = HideFlags.HideAndDontSave;
        fallbackSourcePrefab = sourceObject.AddComponent<AudioSource>();
        fallbackSourcePrefab.playOnAwake = false;
        fallbackSourcePrefab.loop = false;
        return fallbackSourcePrefab;
    }

    private void WarmPool()
    {
        for (int i = availableSources.Count + activeSources.Count; i < initialPoolSize; i++)
        {
            availableSources.Enqueue(CreateSource());
        }
    }

    private void RecycleFinishedSources()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeSources[i];
            if (source == null)
            {
                activeSources.RemoveAt(i);
                continue;
            }

            if (source.isPlaying)
            {
                continue;
            }

            source.clip = null;
            source.gameObject.SetActive(false);
            activeSources.RemoveAt(i);
            availableSources.Enqueue(source);
        }
    }

    private void SubscribeToEvents()
    {
        ClearSubscriptions();
        subscriptions.Add(GameEventBus.Subscribe<SoundEffectRequestedEvent>(HandleSoundEffectRequested));
        subscriptions.Add(GameEventBus.Subscribe<GameFlowStartedEvent>(_ => PlayBindings(GameEventSound.GameFlowStarted)));
        subscriptions.Add(GameEventBus.Subscribe<PlayerInputLockChangedEvent>(HandlePlayerInputLockChanged));
        subscriptions.Add(GameEventBus.Subscribe<AmmoBoxSpawnedEvent>(evt => PlayBindings(GameEventSound.AmmoBoxSpawned, evt.AmmoBox != null ? evt.AmmoBox.transform.position : (Vector3?)null)));
        subscriptions.Add(GameEventBus.Subscribe<BulletsSpawnRequestedEvent>(_ => PlayBindings(GameEventSound.BulletsSpawnRequested)));
        subscriptions.Add(GameEventBus.Subscribe<BulletsSpawnedEvent>(_ => PlayBindings(GameEventSound.BulletsSpawned)));
        subscriptions.Add(GameEventBus.Subscribe<ReloadButtonClickedEvent>(_ => PlayBindings(GameEventSound.ReloadButtonClicked)));
        subscriptions.Add(GameEventBus.Subscribe<ReloadStartedEvent>(_ => PlayBindings(GameEventSound.ReloadStarted)));
        subscriptions.Add(GameEventBus.Subscribe<ReloadCompletedEvent>(_ => PlayBindings(GameEventSound.ReloadCompleted)));
        subscriptions.Add(GameEventBus.Subscribe<ShootPlayerButtonClickedEvent>(_ => PlayBindings(GameEventSound.ShootPlayerButtonClicked)));
        subscriptions.Add(GameEventBus.Subscribe<ShootEnemyButtonClickedEvent>(_ => PlayBindings(GameEventSound.ShootEnemyButtonClicked)));
        subscriptions.Add(GameEventBus.Subscribe<ShootPlayerStartedEvent>(_ => PlayBindings(GameEventSound.ShootPlayerStarted)));
        subscriptions.Add(GameEventBus.Subscribe<ShootEnemyStartedEvent>(_ => PlayBindings(GameEventSound.ShootEnemyStarted)));
        subscriptions.Add(GameEventBus.Subscribe<ShotGunFiredEvent>(HandleShotGunFired));
        subscriptions.Add(GameEventBus.Subscribe<ShotGunShellEjectRequestedEvent>(HandleShotGunShellEjectRequested));
        subscriptions.Add(GameEventBus.Subscribe<ShotGunShellLoadStartedEvent>(HandleShotGunShellLoadStarted));
        subscriptions.Add(GameEventBus.Subscribe<ShotGunShellLoadCompletedEvent>(HandleShotGunShellLoadCompleted));
        subscriptions.Add(GameEventBus.Subscribe<ShotGunBoltPulledEvent>(HandleShotGunBoltPulled));
        subscriptions.Add(GameEventBus.Subscribe<ShotGunMoveStartedEvent>(HandleShotGunMoveStarted));
        subscriptions.Add(GameEventBus.Subscribe<ShotGunMoveCompletedEvent>(HandleShotGunMoveCompleted));
        subscriptions.Add(GameEventBus.Subscribe<ShootPlayerCompletedEvent>(_ => PlayBindings(GameEventSound.ShootPlayerCompleted)));
        subscriptions.Add(GameEventBus.Subscribe<ShootEnemyCompletedEvent>(_ => PlayBindings(GameEventSound.ShootEnemyCompleted)));
        subscriptions.Add(GameEventBus.Subscribe<InspectableHoveredEvent>(evt => PlayBindings(GameEventSound.InspectableHovered, evt.Item != null ? evt.Item.transform.position : (Vector3?)null)));
        subscriptions.Add(GameEventBus.Subscribe<InspectableHoverClearedEvent>(_ => PlayBindings(GameEventSound.InspectableHoverCleared)));
        subscriptions.Add(GameEventBus.Subscribe<FocusStateChangedEvent>(HandleFocusStateChanged));
    }

    private void ClearSubscriptions()
    {
        for (int i = 0; i < subscriptions.Count; i++)
        {
            subscriptions[i]?.Dispose();
        }

        subscriptions.Clear();
    }

    private void HandleSoundEffectRequested(SoundEffectRequestedEvent evt)
    {
        Play(evt.SoundId, evt.WorldPosition, evt.VolumeScale);
    }

    private void HandlePlayerInputLockChanged(PlayerInputLockChangedEvent evt)
    {
        PlayBindings(evt.Locked ? GameEventSound.PlayerInputLocked : GameEventSound.PlayerInputUnlocked);
    }

    private void HandleShotGunFired(ShotGunFiredEvent evt)
    {
        PlayBindings(evt.ShellKind == ShotGunShellKind.Live
            ? GameEventSound.ShotGunFiredLive
            : GameEventSound.ShotGunFiredBlank);
    }

    private void HandleShotGunShellEjectRequested(ShotGunShellEjectRequestedEvent evt)
    {
        PlayBindings(GameEventSound.ShotGunShellEjectRequested, evt.ShotGun != null ? evt.ShotGun.position : (Vector3?)null);
    }

    private void HandleShotGunShellLoadStarted(ShotGunShellLoadStartedEvent evt)
    {
        PlayBindings(GameEventSound.ShotGunShellLoadStarted, evt.ShotGun != null ? evt.ShotGun.position : (Vector3?)null);
    }

    private void HandleShotGunShellLoadCompleted(ShotGunShellLoadCompletedEvent evt)
    {
        PlayBindings(GameEventSound.ShotGunShellLoadCompleted, evt.ShotGun != null ? evt.ShotGun.position : (Vector3?)null);
    }

    private void HandleShotGunBoltPulled(ShotGunBoltPulledEvent evt)
    {
        Vector3? position = evt.ShotGun != null ? evt.ShotGun.position : (Vector3?)null;
        PlayBindings(GameEventSound.ShotGunBoltPulled, position);
        PlayBindings(
            evt.EjectsShell
                ? GameEventSound.ShotGunBoltPulledWithShellEject
                : GameEventSound.ShotGunBoltPulledWithoutShellEject,
            position);
    }

    private void HandleShotGunMoveStarted(ShotGunMoveStartedEvent evt)
    {
        Vector3? position = evt.ShotGun != null ? evt.ShotGun.position : (Vector3?)null;
        PlayBindings(GameEventSound.ShotGunMoveStarted, position);
        PlayBindings(GetShotGunMoveStartedSound(evt.Purpose), position);
    }

    private void HandleShotGunMoveCompleted(ShotGunMoveCompletedEvent evt)
    {
        PlayBindings(GameEventSound.ShotGunMoveCompleted, evt.ShotGun != null ? evt.ShotGun.position : (Vector3?)null);
    }

    private static GameEventSound GetShotGunMoveStartedSound(ShotGunMovePurpose purpose)
    {
        switch (purpose)
        {
            case ShotGunMovePurpose.ReloadMove:
                return GameEventSound.ShotGunReloadMoveStarted;
            case ShotGunMovePurpose.ReloadReturn:
                return GameEventSound.ShotGunReloadReturnMoveStarted;
            case ShotGunMovePurpose.ShootAimMove:
                return GameEventSound.ShotGunShootAimMoveStarted;
            case ShotGunMovePurpose.EjectMove:
                return GameEventSound.ShotGunEjectMoveStarted;
            case ShotGunMovePurpose.EjectReturn:
                return GameEventSound.ShotGunEjectReturnMoveStarted;
            default:
                return GameEventSound.ShotGunMoveStarted;
        }
    }

    private void HandleFocusStateChanged(FocusStateChangedEvent evt)
    {
        if (!IsShotGunFocusTarget(evt.FocusTarget))
        {
            return;
        }

        PlayBindings(evt.Focused ? GameEventSound.ShotGunFocusStarted : GameEventSound.ShotGunFocusEnded);
    }

    private static bool IsShotGunFocusTarget(UnityEngine.Object focusTarget)
    {
        if (focusTarget is ShotGunState)
        {
            return true;
        }

        if (focusTarget is FocusableObject focusableObject)
        {
            Transform target = focusableObject.Target;
            return target != null && target.GetComponentInParent<ShotGunState>() != null;
        }

        if (focusTarget is Component component)
        {
            return component.GetComponentInParent<ShotGunState>() != null;
        }

        if (focusTarget is GameObject gameObject)
        {
            return gameObject.GetComponentInParent<ShotGunState>() != null;
        }

        return false;
    }

    private void PlayBindings(GameEventSound gameEvent)
    {
        PlayBindings(gameEvent, null);
    }

    private void PlayBindings(GameEventSound gameEvent, Vector3? worldPosition)
    {
        if (!bindingsByEvent.TryGetValue(gameEvent, out List<GameEventSoundBinding> bindings))
        {
            return;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            GameEventSoundBinding binding = bindings[i];
            Play(binding.SoundId, worldPosition, binding.VolumeScale);
        }
    }
}
