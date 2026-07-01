using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class GameFlowController : MonoBehaviour
{
    private static readonly FieldInfo UsableItemEffectsField =
        typeof(UsableItem).GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic);

    private enum GameFlowState
    {
        NotStarted,
        InputLocked,
        AmmoBoxFalling,
        SpawningBullets,
        OpeningAmmoBox,
        Gameplay
    }

    [Header("Player Input")]
    [SerializeField] private WASDCameraMovement cameraMovement;
    [SerializeField] private LimitedCameraLook cameraLook;
    [SerializeField, FormerlySerializedAs("shotGunInteraction")] private FocusableObject shotGunFocus;
    [SerializeField] private InteractionUIController interactionUI;

    [Header("Ammo Box")]
    [SerializeField] private GameObject ammoBoxPrefab;
    [SerializeField] private Vector3 ammoBoxSpawnPosition;
    [SerializeField] private Vector3 ammoBoxSpawnRotation;
    [SerializeField] private Vector3 ammoBoxLandingPosition;
    [SerializeField] private Vector3 ammoBoxLandingRotation;
    [SerializeField, Min(0.01f)] private float ammoBoxDropDuration = 0.8f;
    [SerializeField] private AnimationCurve ammoBoxDropCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    [SerializeField] private Transform ammoBoxParent;
    [SerializeField] private string coverChildName = "Cover";

    [Header("Bullets")]
    [SerializeField] private BulletSpawner bulletSpawner;
    [SerializeField, Min(0f)] private float bulletSpawnDelayAfterAmmoBoxSpawn = 1f;

    [Header("Notification Flow")]
    [SerializeField] private NotificationScreenController notificationScreen;
    [SerializeField] private Vector3 notificationCameraPosition;
    [SerializeField] private Vector3 notificationCameraEulerAngles;
    [SerializeField, Min(0.01f)] private float notificationCameraMoveDuration = 0.45f;
    [SerializeField] private bool returnCameraAfterNotification = true;
    [SerializeField, Min(0.01f)] private float notificationCameraReturnDuration = 0.45f;
    [SerializeField, Min(0f)] private float notificationDisplayDuration = 1.2f;
    [SerializeField] private AnimationCurve notificationTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    [SerializeField, TextArea] private string ammoDepletedNotificationMessage = "Ammo depleted.";
    [SerializeField, TextArea] private string turnChangedNotificationMessage = "Enemy turn.";

    [Header("Reveal Current Shell Flow")]
    [SerializeField] private Vector3 revealCurrentShellCameraPosition;
    [SerializeField] private Vector3 revealCurrentShellCameraEulerAngles;
    [SerializeField, Min(0.01f)] private float revealCurrentShellCameraMoveDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float revealCurrentShellCameraReturnDuration = 0.45f;
    [SerializeField, Min(0f)] private float revealCurrentShellDisplayDuration = 1.2f;
    [SerializeField] private AnimationCurve revealCurrentShellTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    [SerializeField, TextArea] private string liveShellRevealMessage = "Current shell: Live.";
    [SerializeField, TextArea] private string blankShellRevealMessage = "Current shell: Blank.";

    [Header("Open Animation")]
    [SerializeField] private Vector3 closedCoverEulerAngles = new Vector3(-90f, 0f, 0f);
    [SerializeField] private Vector3 openCoverEulerAngles = new Vector3(-210f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float openDuration = 0.8f;
    [SerializeField] private AnimationCurve openCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Reload Flow")]
    [SerializeField, FormerlySerializedAs("revolver")] private Transform shotGun;
    [SerializeField] private ShotGunState shotGunState;
    [SerializeField, FormerlySerializedAs("revolverReloadPosition")] private Vector3 shotGunReloadPosition;
    [SerializeField, FormerlySerializedAs("revolverReloadEulerAngles")] private Vector3 shotGunReloadEulerAngles;
    [SerializeField, FormerlySerializedAs("revolverReloadScale")] private Vector3 shotGunReloadScale = Vector3.one;
    [SerializeField] private Vector3 ammoBoxReloadPosition;
    [SerializeField] private Vector3 ammoBoxReloadEulerAngles;
    [SerializeField] private Vector3 ammoBoxReloadScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float reloadCameraMoveDuration = 0.45f;
    [SerializeField, Min(0.01f), FormerlySerializedAs("reloadRevolverMoveDuration")] private float reloadShotGunMoveDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float reloadAmmoBoxMoveDuration = 0.45f;
    [SerializeField, Min(0.01f), FormerlySerializedAs("reloadRevolverReturnDuration")] private float reloadShotGunReturnDuration = 0.45f;
    [SerializeField] private AnimationCurve reloadTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    [SerializeField, Min(0f)] private float skippedLoadAnimationDelay;

    [Header("Reload Animation")]
    [SerializeField, FormerlySerializedAs("revolverReloadAnimator")] private ShotGunReloadAnimator shotGunReloadAnimator;
    [SerializeField, Min(0f)] private float postBoltReturnDelay = 0.2f;

    [Header("Ammo Box Dissolve")]
    [SerializeField] private bool dissolveAmmoBoxAfterReload = true;

    [Header("Shoot Player Flow")]
    [SerializeField] private ShotGunShootPlayerAnimator shotGunShootPlayerAnimator;
    [SerializeField] private Vector3 shotGunShootPlayerCameraPosition;
    [SerializeField] private Vector3 shotGunShootPlayerCameraEulerAngles;
    [SerializeField] private Vector3 shotGunShootPlayerScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float shootPlayerShotGunMoveDuration = 0.45f;
    [SerializeField] private AnimationCurve shootPlayerTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Shoot Enemy Flow")]
    [SerializeField] private ShotGunShootEnemyAnimator shotGunShootEnemyAnimator;
    [SerializeField] private Vector3 shotGunShootEnemyCameraPosition;
    [SerializeField] private Vector3 shotGunShootEnemyCameraEulerAngles;
    [SerializeField] private Vector3 shotGunShootEnemyScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float shootEnemyShotGunMoveDuration = 0.45f;
    [SerializeField] private AnimationCurve shootEnemyTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Enemy Shoot Player Flow")]
    [SerializeField, FormerlySerializedAs("enemyShootPlayerShotGunCameraPosition")] private Vector3 enemyShootPlayerShotGunWorldPosition;
    [SerializeField, FormerlySerializedAs("enemyShootPlayerShotGunCameraEulerAngles")] private Vector3 enemyShootPlayerShotGunWorldEulerAngles;
    [SerializeField] private Vector3 enemyShootPlayerShotGunScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float enemyShootPlayerShotGunMoveDuration = 0.45f;
    [SerializeField] private AnimationCurve enemyShootPlayerTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Enemy Shoot Enemy Flow")]
    [SerializeField, FormerlySerializedAs("enemyShootEnemyShotGunCameraPosition")] private Vector3 enemyShootEnemyShotGunWorldPosition;
    [SerializeField, FormerlySerializedAs("enemyShootEnemyShotGunCameraEulerAngles")] private Vector3 enemyShootEnemyShotGunWorldEulerAngles;
    [SerializeField] private Vector3 enemyShootEnemyShotGunScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float enemyShootEnemyShotGunMoveDuration = 0.45f;
    [SerializeField] private AnimationCurve enemyShootEnemyTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Enemy Shot Eject Flow")]
    [SerializeField] private Vector3 enemyShotGunEjectWorldPosition;
    [SerializeField] private Vector3 enemyShotGunEjectWorldEulerAngles;
    [SerializeField] private Vector3 enemyShotGunEjectScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float enemyShotGunEjectMoveDuration = 0.45f;
    [SerializeField] private AnimationCurve enemyEjectTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Shoot Player Eject Flow")]
    [SerializeField] private Vector3 shotGunEjectCameraPosition;
    [SerializeField] private Vector3 shotGunEjectCameraEulerAngles;
    [SerializeField] private Vector3 shotGunEjectScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float ejectShotGunMoveDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float ejectShotGunReturnDuration = 0.45f;
    [SerializeField] private AnimationCurve ejectTransitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Flow")]
    [SerializeField] private bool startFlowOnStart = true;
    [SerializeField, FormerlySerializedAs("enemyShootsPlayerAfterPlayerShootsEnemy")] private bool enemyActsAfterPlayerShootsEnemy = true;
    [SerializeField, Range(0f, 1f)] private float enemyShootPlayerChance = 0.5f;
    [SerializeField, Min(0f)] private float playerHitScreenEffectStartGraceDuration = 0.2f;
    [SerializeField] private bool logShellInventory;

    private GameFlowState state = GameFlowState.NotStarted;
    private GameObject spawnedAmmoBox;
    private AmmoBoxAnimator spawnedAmmoBoxAnimator;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private Vector3 initialShotGunPosition;
    private Quaternion initialShotGunRotation;
    private Vector3 initialShotGunScale;
    private IDisposable reloadButtonClickedSubscription;
    private IDisposable shootPlayerButtonClickedSubscription;
    private IDisposable shootEnemyButtonClickedSubscription;
    private IDisposable revealCurrentShellConsumedSubscription;
    private IDisposable focusedTargetRemovedSubscription;
    private Coroutine reloadCoroutine;
    private Coroutine shootPlayerCoroutine;
    private Coroutine shootEnemyCoroutine;
    private Coroutine revealCurrentShellCoroutine;
    private Coroutine revealCurrentShellConsumeWaitCoroutine;
    private Sequence activeTransformTween;
    private bool shotGunDepletedAfterLastShot;

    public GameObject SpawnedAmmoBox => spawnedAmmoBox;

    private void Awake()
    {
        if (cameraMovement == null)
        {
            cameraMovement = FindObjectOfType<WASDCameraMovement>();
        }

        if (cameraLook == null)
        {
            cameraLook = FindObjectOfType<LimitedCameraLook>();
        }

        if (shotGunState == null)
        {
            shotGunState = FindObjectOfType<ShotGunState>();
        }

        if (shotGunFocus == null && shotGun != null)
        {
            shotGunFocus = shotGun.GetComponent<FocusableObject>();
            if (shotGunFocus == null)
            {
                shotGunFocus = shotGun.gameObject.AddComponent<FocusableObject>();
            }
        }

        if (shotGunFocus == null && shotGunState != null)
        {
            shotGunFocus = shotGunState.GetComponent<FocusableObject>();
        }

        if (shotGunFocus == null)
        {
            shotGunFocus = FindObjectOfType<FocusableObject>();
        }

        if (shotGun == null && shotGunFocus != null)
        {
            shotGun = shotGunFocus.Target;
        }

        if (shotGun == null && shotGunState != null)
        {
            shotGun = shotGunState.transform;
        }

        if (shotGunState == null && shotGun != null)
        {
            shotGunState = shotGun.GetComponent<ShotGunState>();
            if (shotGunState == null)
            {
                shotGunState = shotGun.gameObject.AddComponent<ShotGunState>();
            }
        }

        if (interactionUI == null)
        {
            interactionUI = FindObjectOfType<InteractionUIController>();
        }

        if (bulletSpawner == null)
        {
            bulletSpawner = FindObjectOfType<BulletSpawner>();
        }

        if (notificationScreen == null)
        {
            notificationScreen = FindObjectOfType<NotificationScreenController>();
        }

        if (shotGunReloadAnimator == null && shotGun != null)
        {
            shotGunReloadAnimator = shotGun.GetComponent<ShotGunReloadAnimator>();
            if (shotGunReloadAnimator == null)
            {
                shotGunReloadAnimator = shotGun.gameObject.AddComponent<ShotGunReloadAnimator>();
            }
        }

        if (shotGunShootPlayerAnimator == null && shotGun != null)
        {
            shotGunShootPlayerAnimator = shotGun.GetComponent<ShotGunShootPlayerAnimator>();
            if (shotGunShootPlayerAnimator == null)
            {
                shotGunShootPlayerAnimator = shotGun.gameObject.AddComponent<ShotGunShootPlayerAnimator>();
            }
        }

        if (shotGunShootEnemyAnimator == null && shotGun != null)
        {
            shotGunShootEnemyAnimator = shotGun.GetComponent<ShotGunShootEnemyAnimator>();
            if (shotGunShootEnemyAnimator == null)
            {
                shotGunShootEnemyAnimator = shotGun.gameObject.AddComponent<ShotGunShootEnemyAnimator>();
            }
        }

        CaptureInitialReloadTransforms();
    }

    private void OnEnable()
    {
        reloadButtonClickedSubscription = GameEventBus.Subscribe<ReloadButtonClickedEvent>(HandleReloadButtonClicked);
        shootPlayerButtonClickedSubscription = GameEventBus.Subscribe<ShootPlayerButtonClickedEvent>(HandleShootPlayerButtonClicked);
        shootEnemyButtonClickedSubscription = GameEventBus.Subscribe<ShootEnemyButtonClickedEvent>(HandleShootEnemyButtonClicked);
        revealCurrentShellConsumedSubscription =
            GameEventBus.Subscribe<RevealCurrentShotGunShellConsumedEvent>(HandleRevealCurrentShellConsumed);
        focusedTargetRemovedSubscription = GameEventBus.Subscribe<FocusedTargetRemovedEvent>(HandleFocusedTargetRemoved);
    }

    private void OnDisable()
    {
        reloadButtonClickedSubscription?.Dispose();
        reloadButtonClickedSubscription = null;
        shootPlayerButtonClickedSubscription?.Dispose();
        shootPlayerButtonClickedSubscription = null;
        shootEnemyButtonClickedSubscription?.Dispose();
        shootEnemyButtonClickedSubscription = null;
        revealCurrentShellConsumedSubscription?.Dispose();
        revealCurrentShellConsumedSubscription = null;
        focusedTargetRemovedSubscription?.Dispose();
        focusedTargetRemovedSubscription = null;
        KillOwnedTweens();
    }

    private void Start()
    {
        if (startFlowOnStart)
        {
            StartGameFlow();
        }
    }

    [ContextMenu("Start Game Flow")]
    public void StartGameFlow()
    {
        StopAllCoroutines();
        reloadCoroutine = null;
        shootPlayerCoroutine = null;
        shootEnemyCoroutine = null;
        revealCurrentShellCoroutine = null;
        KillOwnedTweens();
        GameEventBus.Publish(new GameFlowStartedEvent());
        StartCoroutine(RunGameFlow());
    }

    private IEnumerator RunGameFlow()
    {
        SetPlayerInputLocked(true);
        yield return RunAmmoBoxSupplyFlow();
        state = GameFlowState.Gameplay;
        SetPlayerInputLocked(false);
    }

    private IEnumerator RunAmmoBoxSupplyFlow()
    {
        state = GameFlowState.InputLocked;

        state = GameFlowState.AmmoBoxFalling;
        SpawnAmmoBox();
        if (spawnedAmmoBox == null)
        {
            yield break;
        }

        yield return MoveAmmoBoxToLandingPosition();

        if (bulletSpawnDelayAfterAmmoBoxSpawn > 0f)
        {
            yield return new WaitForSeconds(bulletSpawnDelayAfterAmmoBoxSpawn);
        }

        state = GameFlowState.SpawningBullets;
        if (bulletSpawner != null)
        {
            GameEventBus.Publish(new BulletsSpawnRequestedEvent());
            bulletSpawner.SetSpawnedBulletsParent(spawnedAmmoBox.transform);
            bulletSpawner.SpawnLevelBullets();
            LogShellInventory(
                $"Spawned shells: Live={bulletSpawner.SpawnedLiveShellCount}, Blank={bulletSpawner.SpawnedBlankShellCount}");
            GameEventBus.Publish(new BulletsSpawnedEvent());
        }
        else
        {
            Debug.LogWarning("Game flow has no BulletSpawner assigned.", this);
        }

        state = GameFlowState.OpeningAmmoBox;
        yield return OpenAmmoBoxCover();
    }

    private void SpawnAmmoBox()
    {
        if (ammoBoxPrefab == null)
        {
            Debug.LogError("Game flow cannot spawn an ammo box because no prefab is assigned.", this);
            return;
        }

        spawnedAmmoBox = Instantiate(
            ammoBoxPrefab,
            ammoBoxSpawnPosition,
            Quaternion.Euler(ammoBoxSpawnRotation),
            ammoBoxParent);
        GameEventBus.Publish(new AmmoBoxSpawnedEvent(spawnedAmmoBox));

        spawnedAmmoBoxAnimator = spawnedAmmoBox.GetComponent<AmmoBoxAnimator>();
        if (spawnedAmmoBoxAnimator == null)
        {
            spawnedAmmoBoxAnimator = spawnedAmmoBox.AddComponent<AmmoBoxAnimator>();
        }

        spawnedAmmoBoxAnimator.ConfigureDrop(
            ammoBoxLandingPosition,
            ammoBoxLandingRotation,
            ammoBoxDropDuration,
            ammoBoxDropCurve);
        spawnedAmmoBoxAnimator.ConfigureCover(
            coverChildName,
            closedCoverEulerAngles,
            openCoverEulerAngles,
            openDuration,
            openCurve);
        spawnedAmmoBoxAnimator.DisablePhysics();
    }

    private static void DisableAmmoBoxPhysics(Transform target)
    {
        AmmoBoxAnimator ammoBoxAnimator = target != null ? target.GetComponent<AmmoBoxAnimator>() : null;
        if (ammoBoxAnimator != null)
        {
            ammoBoxAnimator.DisablePhysics();
            return;
        }

        Rigidbody rigidbody = target != null ? target.GetComponent<Rigidbody>() : null;
        if (rigidbody == null)
        {
            return;
        }

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    private IEnumerator MoveAmmoBoxToLandingPosition()
    {
        if (spawnedAmmoBoxAnimator == null)
        {
            yield break;
        }

        yield return spawnedAmmoBoxAnimator.PlayDrop();
    }

    private IEnumerator OpenAmmoBoxCover()
    {
        if (spawnedAmmoBoxAnimator == null)
        {
            yield break;
        }

        yield return spawnedAmmoBoxAnimator.PlayOpenCover();
    }

    private void HandleReloadButtonClicked(ReloadButtonClickedEvent evt)
    {
        StartReloadFlow();
    }

    private void HandleShootPlayerButtonClicked(ShootPlayerButtonClickedEvent evt)
    {
        StartShootPlayerFlow();
    }

    private void HandleShootEnemyButtonClicked(ShootEnemyButtonClickedEvent evt)
    {
        StartShootEnemyFlow();
    }

    private void HandleRevealCurrentShellConsumed(RevealCurrentShotGunShellConsumedEvent evt)
    {
        StartRevealCurrentShellFlow();
    }

    private void HandleFocusedTargetRemoved(FocusedTargetRemovedEvent evt)
    {
        UsableItem usableItem = FindUsableItem(evt.FocusTarget);
        if (usableItem == null || !HasRevealCurrentShellEffect(usableItem))
        {
            return;
        }

        if (revealCurrentShellConsumeWaitCoroutine != null)
        {
            StopCoroutine(revealCurrentShellConsumeWaitCoroutine);
        }

        revealCurrentShellConsumeWaitCoroutine = StartCoroutine(WaitForRevealCurrentShellItemConsumed(usableItem));
    }

    [ContextMenu("Start Reload Flow")]
    public void StartReloadFlow()
    {
        if (reloadCoroutine != null
            || shootPlayerCoroutine != null
            || shootEnemyCoroutine != null
            || revealCurrentShellCoroutine != null)
        {
            return;
        }

        reloadCoroutine = StartCoroutine(RunReloadFlow());
    }

    [ContextMenu("Start Shoot Player Flow")]
    public void StartShootPlayerFlow()
    {
        if (shootPlayerCoroutine != null
            || shootEnemyCoroutine != null
            || reloadCoroutine != null
            || revealCurrentShellCoroutine != null)
        {
            return;
        }

        shootPlayerCoroutine = StartCoroutine(RunShootPlayerFlow());
    }

    [ContextMenu("Start Shoot Enemy Flow")]
    public void StartShootEnemyFlow()
    {
        if (shootEnemyCoroutine != null || shootPlayerCoroutine != null || reloadCoroutine != null || revealCurrentShellCoroutine != null)
        {
            return;
        }

        shootEnemyCoroutine = StartCoroutine(RunShootEnemyFlow());
    }

    [ContextMenu("Start Reveal Current Shell Flow")]
    public void StartRevealCurrentShellFlow()
    {
        if (revealCurrentShellCoroutine != null
            || reloadCoroutine != null
            || shootPlayerCoroutine != null
            || shootEnemyCoroutine != null)
        {
            return;
        }

        revealCurrentShellCoroutine = StartCoroutine(RunRevealCurrentShellFlow());
    }

    private IEnumerator WaitForRevealCurrentShellItemConsumed(UsableItem usableItem)
    {
        yield return null;

        DissolveAnimator itemDissolveAnimator = usableItem != null
            ? usableItem.GetComponent<DissolveAnimator>() ?? usableItem.GetComponentInChildren<DissolveAnimator>(true)
            : null;

        while (itemDissolveAnimator != null && itemDissolveAnimator.IsPlaying)
        {
            yield return null;
        }

        revealCurrentShellConsumeWaitCoroutine = null;
        StartRevealCurrentShellFlow();
    }

    private IEnumerator RunReloadFlow()
    {
        bool keepShotGunFocusedDuringCameraMove = shotGunFocus != null && shotGunFocus.IsFocused;
        GameEventBus.Publish(new ReloadStartedEvent());
        SetPlayerInputLocked(true);

        if (interactionUI != null)
        {
            interactionUI.SetFocusActionsVisible(false);
        }

        if (cameraLook != null)
        {
            cameraLook.SetExternalControl(true);
        }

        if (shotGunFocus != null)
        {
            shotGunFocus.BeginExternalControl();
        }

        Transform cameraTransform = GetCameraTransform();
        if (cameraTransform != null)
        {
            if (keepShotGunFocusedDuringCameraMove)
            {
                shotGunFocus.ApplyExternalFocusedTransform();
            }

            yield return TweenTransform(
                cameraTransform,
                initialCameraPosition,
                initialCameraRotation,
                cameraTransform.localScale,
                reloadCameraMoveDuration,
                false,
                null,
                keepShotGunFocusedDuringCameraMove ? shotGunFocus.ApplyExternalFocusedTransform : null);

            if (keepShotGunFocusedDuringCameraMove)
            {
                shotGunFocus.ApplyExternalFocusedTransform();
            }
        }

        if (shotGun != null)
        {
            yield return TweenShotGunTransform(
                ShotGunMovePurpose.ReloadMove,
                shotGun,
                shotGunReloadPosition,
                Quaternion.Euler(shotGunReloadEulerAngles),
                shotGunReloadScale,
                reloadShotGunMoveDuration,
                true);
        }

        if (spawnedAmmoBox != null)
        {
            DisableAmmoBoxPhysics(spawnedAmmoBox.transform);
            yield return TweenTransform(
                spawnedAmmoBox.transform,
                ammoBoxReloadPosition,
                Quaternion.Euler(ammoBoxReloadEulerAngles),
                ammoBoxReloadScale,
                reloadAmmoBoxMoveDuration,
                true);
        }

        DestroySpawnedBulletEntities();

        if (skippedLoadAnimationDelay > 0f)
        {
            yield return new WaitForSeconds(skippedLoadAnimationDelay);
        }

        yield return PlayReloadAnimations();

        if (shotGunState != null && bulletSpawner != null)
        {
            shotGunState.LoadShells(GetRandomizedSpawnedShellKinds());
            LogShellInventory($"Loaded shells: {shotGunState.GetLoadedShellSummary()}");
        }

        if (dissolveAmmoBoxAfterReload)
        {
            yield return DissolveSpawnedAmmoBox();
        }

        if (postBoltReturnDelay > 0f)
        {
            yield return new WaitForSeconds(postBoltReturnDelay);
        }

        if (shotGun != null)
        {
            yield return TweenShotGunTransform(
                ShotGunMovePurpose.ReloadReturn,
                shotGun,
                initialShotGunPosition,
                initialShotGunRotation,
                initialShotGunScale,
                reloadShotGunReturnDuration,
                true);
        }

        if (cameraLook != null)
        {
            cameraLook.ResetLookToStartingRotation();
            cameraLook.SetExternalControl(false);
        }

        if (shotGunFocus != null)
        {
            shotGunFocus.EndExternalControl();
        }

        SetPlayerInputLocked(false);
        GameEventBus.Publish(new ReloadCompletedEvent());
        reloadCoroutine = null;
    }

    private IEnumerator RunShootPlayerFlow()
    {
        yield return RunShotGunShootFlow(
            shotGunShootPlayerCameraPosition,
            shotGunShootPlayerCameraEulerAngles,
            shotGunShootPlayerScale,
            shootPlayerShotGunMoveDuration,
            shootPlayerTransitionCurve,
            ShotGunFireEffectContext.PlayerShootsPlayer,
            PlayShootPlayerAnimation,
            () => GameEventBus.Publish(new ShootPlayerStartedEvent()),
            () => GameEventBus.Publish(new ShootPlayerCompletedEvent()));
        shootPlayerCoroutine = null;
    }

    private IEnumerator RunShootEnemyFlow()
    {
        yield return RunShotGunShootFlow(
            shotGunShootEnemyCameraPosition,
            shotGunShootEnemyCameraEulerAngles,
            shotGunShootEnemyScale,
            shootEnemyShotGunMoveDuration,
            shootEnemyTransitionCurve,
            ShotGunFireEffectContext.PlayerShootsEnemy,
            PlayShootEnemyAnimation,
            () => GameEventBus.Publish(new ShootEnemyStartedEvent()),
            () => GameEventBus.Publish(new ShootEnemyCompletedEvent()));

        bool skipEnemyTurn = ConsumeSkipNextEnemyTurnAfterPlayerShootsEnemy();
        if (enemyActsAfterPlayerShootsEnemy && !shotGunDepletedAfterLastShot && !skipEnemyTurn)
        {
            yield return RunNotificationFlow(
                NotificationMessageKind.TurnChanged,
                turnChangedNotificationMessage);
            yield return RunEnemyTurnShotFlow();
        }

        shootEnemyCoroutine = null;
    }

    private static bool ConsumeSkipNextEnemyTurnAfterPlayerShootsEnemy()
    {
        GameGlobalManager gameGlobalManager = FindFirstObjectByType<GameGlobalManager>();
        return gameGlobalManager != null && gameGlobalManager.ConsumeSkipNextEnemyTurnAfterPlayerShootsEnemy();
    }

    private IEnumerator RunEnemyTurnShotFlow()
    {
        if (UnityEngine.Random.value < enemyShootPlayerChance)
        {
            yield return RunEnemyShootPlayerFlow();
            yield break;
        }

        yield return RunEnemyShootEnemyFlow();
    }

    private IEnumerator RunEnemyShootPlayerFlow()
    {
        yield return RunShotGunShootFlow(
            enemyShootPlayerShotGunWorldPosition,
            enemyShootPlayerShotGunWorldEulerAngles,
            enemyShootPlayerShotGunScale,
            enemyShootPlayerShotGunMoveDuration,
            enemyShootPlayerTransitionCurve,
            ShotGunFireEffectContext.EnemyShootsPlayer,
            PlayShootEnemyAnimation,
            () => GameEventBus.Publish(new ShootPlayerStartedEvent()),
            () => GameEventBus.Publish(new ShootPlayerCompletedEvent()),
            moveToEjectPositionBeforeBolt: true,
            useCameraReferenceFrame: false,
            useEnemyWorldEjectPosition: true);
    }

    private IEnumerator RunEnemyShootEnemyFlow()
    {
        yield return RunShotGunShootFlow(
            enemyShootEnemyShotGunWorldPosition,
            enemyShootEnemyShotGunWorldEulerAngles,
            enemyShootEnemyShotGunScale,
            enemyShootEnemyShotGunMoveDuration,
            enemyShootEnemyTransitionCurve,
            ShotGunFireEffectContext.EnemyShootsEnemy,
            PlayShootEnemyAnimation,
            () => GameEventBus.Publish(new ShootEnemyStartedEvent()),
            () => GameEventBus.Publish(new ShootEnemyCompletedEvent()),
            moveToEjectPositionBeforeBolt: true,
            useCameraReferenceFrame: false,
            useEnemyWorldEjectPosition: true);
    }

    private IEnumerator RunShotGunShootFlow(
        Vector3 shotGunCameraPosition,
        Vector3 shotGunCameraEulerAngles,
        Vector3 shotGunScale,
        float shotGunMoveDuration,
        AnimationCurve transitionCurve,
        ShotGunFireEffectContext fireEffectContext,
        Func<ShotGunShellKind, ShotGunFireEffectContext, IEnumerator> playShootAnimation,
        Action publishStarted,
        Action publishCompleted,
        bool moveToEjectPositionBeforeBolt = true,
        bool useCameraReferenceFrame = true,
        bool useEnemyWorldEjectPosition = false)
    {
        publishStarted?.Invoke();
        shotGunDepletedAfterLastShot = false;
        SetPlayerInputLocked(true);

        if (interactionUI != null)
        {
            interactionUI.SetFocusActionsVisible(false);
        }

        if (cameraLook != null)
        {
            cameraLook.SetExternalControl(true);
        }

        if (shotGunFocus != null)
        {
            shotGunFocus.BeginExternalControl();
        }

        Transform cameraTransform = GetCameraTransform();
        if (shotGun != null && (!useCameraReferenceFrame || cameraTransform != null))
        {
            Vector3 shotGunWorldPosition = useCameraReferenceFrame
                ? cameraTransform.TransformPoint(shotGunCameraPosition)
                : shotGunCameraPosition;
            Quaternion shotGunWorldRotation = useCameraReferenceFrame
                ? cameraTransform.rotation * Quaternion.Euler(shotGunCameraEulerAngles)
                : Quaternion.Euler(shotGunCameraEulerAngles);
            yield return TweenShotGunTransform(
                ShotGunMovePurpose.ShootAimMove,
                shotGun,
                shotGunWorldPosition,
                shotGunWorldRotation,
                shotGunScale,
                shotGunMoveDuration,
                true,
                transitionCurve);
        }

        ShotGunShellKind firedShellKind = shotGunState != null
            ? shotGunState.ConsumeNextShell()
            : ShotGunShellKind.Blank;
        shotGunDepletedAfterLastShot = shotGunState != null && shotGunState.RemainingShellCount <= 0;
        LogShellInventory(
            shotGunState != null
                ? $"ShotGun fired shell kind: {firedShellKind}. After shot: {shotGunState.GetLoadedShellSummary()}"
                : $"ShotGun fired shell kind: {firedShellKind}. No ShotGunState assigned.");

        bool waitForPlayerHitScreenEffect = IsLiveShotHittingPlayer(firedShellKind, fireEffectContext);
        bool playerHitScreenEffectStarted = false;
        bool playerHitScreenEffectCompleted = false;
        IDisposable playerHitScreenEffectStartedSubscription = null;
        IDisposable playerHitScreenEffectCompletedSubscription = null;
        if (waitForPlayerHitScreenEffect)
        {
            playerHitScreenEffectStartedSubscription =
                GameEventBus.Subscribe<PlayerHitScreenEffectStartedEvent>(_ => playerHitScreenEffectStarted = true);
            playerHitScreenEffectCompletedSubscription =
                GameEventBus.Subscribe<PlayerHitScreenEffectCompletedEvent>(_ => playerHitScreenEffectCompleted = true);
        }

        if (playShootAnimation != null)
        {
            yield return playShootAnimation(firedShellKind, fireEffectContext);
        }

        if (waitForPlayerHitScreenEffect)
        {
            yield return WaitForPlayerHitScreenEffect(
                () => playerHitScreenEffectStarted,
                () => playerHitScreenEffectCompleted);
            playerHitScreenEffectStartedSubscription?.Dispose();
            playerHitScreenEffectCompletedSubscription?.Dispose();
        }

        PublishShotGunHitResolved(firedShellKind, fireEffectContext);

        yield return RunShootPlayerEjectPhase(
            cameraTransform,
            firedShellKind,
            moveToEjectPositionBeforeBolt,
            useEnemyWorldEjectPosition);

        if (cameraLook != null)
        {
            cameraLook.ResetLookToStartingRotation();
            cameraLook.SetExternalControl(false);
        }

        if (shotGunFocus != null)
        {
            shotGunFocus.EndExternalControl();
        }

        if (shotGunDepletedAfterLastShot)
        {
            yield return RunNotificationFlow(
                NotificationMessageKind.AmmoDepleted,
                ammoDepletedNotificationMessage);
            yield return RunAmmoBoxSupplyFlow();
            state = GameFlowState.Gameplay;
        }

        SetPlayerInputLocked(false);
        publishCompleted?.Invoke();
    }

    private IEnumerator PlayShootPlayerAnimation(ShotGunShellKind shellKind, ShotGunFireEffectContext fireEffectContext)
    {
        if (shotGunShootPlayerAnimator == null)
        {
            yield break;
        }

        yield return shotGunShootPlayerAnimator.PlayShootPlayer(shellKind, fireEffectContext);
    }

    private IEnumerator PlayShootEnemyAnimation(ShotGunShellKind shellKind, ShotGunFireEffectContext fireEffectContext)
    {
        if (shotGunShootEnemyAnimator == null)
        {
            yield break;
        }

        yield return shotGunShootEnemyAnimator.PlayShootEnemy(shellKind, fireEffectContext);
    }

    private static void PublishShotGunHitResolved(ShotGunShellKind shellKind, ShotGunFireEffectContext fireEffectContext)
    {
        GameCharacter target = GetShotTarget(fireEffectContext);
        int damage = shellKind == ShotGunShellKind.Live ? 1 : 0;
        GameEventBus.Publish(new ShotGunHitResolvedEvent(shellKind, fireEffectContext, target, damage));
    }

    private IEnumerator WaitForPlayerHitScreenEffect(Func<bool> hasStarted, Func<bool> hasCompleted)
    {
        float startWaitElapsed = 0f;
        while (!hasStarted() && startWaitElapsed < playerHitScreenEffectStartGraceDuration)
        {
            startWaitElapsed += Time.deltaTime;
            yield return null;
        }

        if (!hasStarted())
        {
            yield break;
        }

        while (!hasCompleted())
        {
            yield return null;
        }
    }

    private static bool IsLiveShotHittingPlayer(ShotGunShellKind shellKind, ShotGunFireEffectContext fireEffectContext)
    {
        return shellKind == ShotGunShellKind.Live && GetShotTarget(fireEffectContext) == GameCharacter.Player;
    }

    private static GameCharacter GetShotTarget(ShotGunFireEffectContext fireEffectContext)
    {
        switch (fireEffectContext)
        {
            case ShotGunFireEffectContext.PlayerShootsPlayer:
            case ShotGunFireEffectContext.EnemyShootsPlayer:
                return GameCharacter.Player;
            case ShotGunFireEffectContext.PlayerShootsEnemy:
            case ShotGunFireEffectContext.EnemyShootsEnemy:
                return GameCharacter.Enemy;
            default:
                return GameCharacter.Enemy;
        }
    }

    private IEnumerator RunShootPlayerEjectPhase(
        Transform cameraTransform,
        ShotGunShellKind firedShellKind,
        bool moveToEjectPositionBeforeBolt,
        bool useEnemyWorldEjectPosition)
    {
        if (moveToEjectPositionBeforeBolt && shotGun != null && useEnemyWorldEjectPosition)
        {
            yield return TweenShotGunTransform(
                ShotGunMovePurpose.EjectMove,
                shotGun,
                enemyShotGunEjectWorldPosition,
                Quaternion.Euler(enemyShotGunEjectWorldEulerAngles),
                enemyShotGunEjectScale,
                enemyShotGunEjectMoveDuration,
                true,
                enemyEjectTransitionCurve);
        }
        else if (moveToEjectPositionBeforeBolt && shotGun != null && cameraTransform != null)
        {
            Vector3 ejectWorldPosition = cameraTransform.TransformPoint(shotGunEjectCameraPosition);
            Quaternion ejectWorldRotation = cameraTransform.rotation * Quaternion.Euler(shotGunEjectCameraEulerAngles);
            yield return TweenShotGunTransform(
                ShotGunMovePurpose.EjectMove,
                shotGun,
                ejectWorldPosition,
                ejectWorldRotation,
                shotGunEjectScale,
                ejectShotGunMoveDuration,
                true,
                ejectTransitionCurve);
        }

        if (shotGunReloadAnimator != null)
        {
            yield return shotGunReloadAnimator.PlayBolt(
                () => GameEventBus.Publish(new ShotGunShellEjectRequestedEvent(firedShellKind, shotGun)));
        }

        if (shotGun != null)
        {
            yield return TweenShotGunTransform(
                ShotGunMovePurpose.EjectReturn,
                shotGun,
                initialShotGunPosition,
                initialShotGunRotation,
                initialShotGunScale,
                ejectShotGunReturnDuration,
                true,
                ejectTransitionCurve);
        }
    }

    private IEnumerator RunNotificationFlow(NotificationMessageKind messageKind, string message)
    {
        GameEventBus.Publish(new NotificationFlowStartedEvent(messageKind, message));

        Transform cameraTransform = GetCameraTransform();
        if (cameraTransform == null)
        {
            DisplayNotification(message);
            if (notificationDisplayDuration > 0f)
            {
                yield return new WaitForSeconds(notificationDisplayDuration);
            }

            RestoreRegularNotification();
            GameEventBus.Publish(new NotificationFlowCompletedEvent(messageKind, message));
            yield break;
        }

        Vector3 previousCameraPosition = cameraTransform.position;
        Quaternion previousCameraRotation = cameraTransform.rotation;
        Vector3 previousCameraScale = cameraTransform.localScale;

        SetPlayerInputLocked(true);
        if (interactionUI != null)
        {
            interactionUI.SetFocusActionsVisible(false);
        }

        if (cameraLook != null)
        {
            cameraLook.SetExternalControl(true);
        }

        yield return TweenTransform(
            cameraTransform,
            notificationCameraPosition,
            Quaternion.Euler(notificationCameraEulerAngles),
            cameraTransform.localScale,
            notificationCameraMoveDuration,
            false,
            notificationTransitionCurve);

        GameEventBus.Publish(new NotificationCameraArrivedEvent(messageKind, message));
        DisplayNotification(message);

        if (notificationDisplayDuration > 0f)
        {
            yield return new WaitForSeconds(notificationDisplayDuration);
        }

        if (returnCameraAfterNotification)
        {
            yield return TweenTransform(
                cameraTransform,
                previousCameraPosition,
                previousCameraRotation,
                previousCameraScale,
                notificationCameraReturnDuration,
                false,
                notificationTransitionCurve);
        }

        if (cameraLook != null)
        {
            cameraLook.SetExternalControl(false);
        }

        RestoreRegularNotification();
        GameEventBus.Publish(new NotificationFlowCompletedEvent(messageKind, message));
    }

    private IEnumerator RunRevealCurrentShellFlow()
    {
        if (shotGunState == null)
        {
            shotGunState = FindFirstObjectByType<ShotGunState>();
        }

        ShotGunShellKind shellKind = ShotGunShellKind.Blank;
        bool hasShellKind = shotGunState != null && shotGunState.TryPeekNextShell(out shellKind);
        string message = hasShellKind ? GetRevealCurrentShellMessage(shellKind) : string.Empty;
        Transform cameraTransform = GetCameraTransform();
        if (cameraTransform == null)
        {
            Debug.LogWarning(
                "Cannot move camera for reveal current shell flow because the camera transform is missing.",
                this);
            GameEventBus.Publish(new RevealCurrentShotGunShellRequestedEvent());
            if (!string.IsNullOrWhiteSpace(message))
            {
                DisplayNotification(message);
            }

            if (revealCurrentShellDisplayDuration > 0f)
            {
                yield return new WaitForSeconds(revealCurrentShellDisplayDuration);
            }

            RestoreRegularNotification();
            revealCurrentShellCoroutine = null;
            yield break;
        }

        SetPlayerInputLocked(true);
        if (interactionUI != null)
        {
            interactionUI.SetFocusActionsVisible(false);
        }

        if (cameraLook != null)
        {
            cameraLook.SetExternalControl(true);
        }

        yield return TweenTransform(
            cameraTransform,
            revealCurrentShellCameraPosition,
            Quaternion.Euler(revealCurrentShellCameraEulerAngles),
            cameraTransform.localScale,
            revealCurrentShellCameraMoveDuration,
            false,
            revealCurrentShellTransitionCurve);

        GameEventBus.Publish(new RevealCurrentShotGunShellRequestedEvent());
        if (!string.IsNullOrWhiteSpace(message))
        {
            DisplayNotification(message);
        }

        if (revealCurrentShellDisplayDuration > 0f)
        {
            yield return new WaitForSeconds(revealCurrentShellDisplayDuration);
        }

        RestoreRegularNotification();

        yield return TweenTransform(
            cameraTransform,
            initialCameraPosition,
            initialCameraRotation,
            cameraTransform.localScale,
            revealCurrentShellCameraReturnDuration,
            false,
            revealCurrentShellTransitionCurve);

        if (cameraLook != null)
        {
            cameraLook.ResetLookToStartingRotation();
            cameraLook.SetExternalControl(false);
        }

        SetPlayerInputLocked(false);
        revealCurrentShellCoroutine = null;
    }

    private string GetRevealCurrentShellMessage(ShotGunShellKind shellKind)
    {
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                return liveShellRevealMessage;
            case ShotGunShellKind.Blank:
                return blankShellRevealMessage;
            default:
                return string.Empty;
        }
    }

    private static UsableItem FindUsableItem(UnityEngine.Object focusTarget)
    {
        if (focusTarget is UsableItem usableItem)
        {
            return usableItem;
        }

        if (focusTarget is Component component)
        {
            return component.GetComponent<UsableItem>() ?? component.GetComponentInParent<UsableItem>();
        }

        if (focusTarget is GameObject gameObject)
        {
            return gameObject.GetComponent<UsableItem>() ?? gameObject.GetComponentInParent<UsableItem>();
        }

        return null;
    }

    private static bool HasRevealCurrentShellEffect(UsableItem usableItem)
    {
        if (usableItem == null || UsableItemEffectsField == null)
        {
            return false;
        }

        UsableItem.ItemEffect effects = (UsableItem.ItemEffect)UsableItemEffectsField.GetValue(usableItem);
        return (effects & UsableItem.ItemEffect.RevealCurrentShotGunShell) != 0;
    }

    private void DisplayNotification(string message)
    {
        if (notificationScreen != null)
        {
            notificationScreen.Show(message);
        }
    }

    private void RestoreRegularNotification()
    {
        if (notificationScreen != null)
        {
            notificationScreen.ShowRegular();
        }
    }

    private IEnumerator TweenShotGunTransform(
        ShotGunMovePurpose purpose,
        Transform target,
        Vector3 destinationPosition,
        Quaternion destinationRotation,
        Vector3 destinationScale,
        float duration,
        bool includeScale,
        AnimationCurve easeCurve = null,
        Action onUpdate = null)
    {
        GameEventBus.Publish(new ShotGunMoveStartedEvent(
            target,
            purpose,
            target.position,
            destinationPosition,
            duration));

        yield return TweenTransform(
            target,
            destinationPosition,
            destinationRotation,
            destinationScale,
            duration,
            includeScale,
            easeCurve,
            onUpdate);

        GameEventBus.Publish(new ShotGunMoveCompletedEvent(target, purpose));
    }

    private IEnumerator TweenTransform(
        Transform target,
        Vector3 destinationPosition,
        Quaternion destinationRotation,
        Vector3 destinationScale,
        float duration,
        bool includeScale,
        AnimationCurve easeCurve = null,
        Action onUpdate = null)
    {
        target.DOKill();
        bool completed = false;
        AnimationCurve targetEaseCurve = easeCurve ?? reloadTransitionCurve;
        Sequence sequence = DOTween.Sequence();
        sequence.Join(ApplyEase(target.DOMove(destinationPosition, duration), targetEaseCurve));
        sequence.Join(ApplyEase(target.DORotateQuaternion(destinationRotation, duration), targetEaseCurve));
        if (includeScale)
        {
            sequence.Join(ApplyEase(target.DOScale(destinationScale, duration), targetEaseCurve));
        }

        if (onUpdate != null)
        {
            sequence.OnUpdate(() => onUpdate());
        }

        sequence.OnComplete(() => completed = true);
        activeTransformTween = sequence;
        yield return sequence.WaitForCompletion();
        if (activeTransformTween == sequence)
        {
            activeTransformTween = null;
        }

        if (!completed)
        {
            yield break;
        }

        target.SetPositionAndRotation(destinationPosition, destinationRotation);
        if (includeScale)
        {
            target.localScale = destinationScale;
        }
    }

    private IEnumerator PlayReloadAnimations()
    {
        if (shotGun == null)
        {
            yield break;
        }

        int loadCount = bulletSpawner != null ? bulletSpawner.SpawnedShellCount : 0;
        if (shotGunReloadAnimator != null)
        {
            yield return shotGunReloadAnimator.PlayReload(loadCount);
        }
    }

    private IReadOnlyList<ShotGunShellKind> GetRandomizedSpawnedShellKinds()
    {
        List<ShotGunShellKind> randomizedShellKinds = new List<ShotGunShellKind>();
        if (bulletSpawner == null)
        {
            return randomizedShellKinds;
        }

        IReadOnlyList<ShotGunShellKind> shellKinds = bulletSpawner.SpawnedShellKinds;
        for (int i = 0; i < shellKinds.Count; i++)
        {
            randomizedShellKinds.Add(shellKinds[i]);
        }

        for (int i = randomizedShellKinds.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            ShotGunShellKind shellKind = randomizedShellKinds[i];
            randomizedShellKinds[i] = randomizedShellKinds[swapIndex];
            randomizedShellKinds[swapIndex] = shellKind;
        }

        return randomizedShellKinds;
    }

    private void DestroySpawnedBulletEntities()
    {
        if (bulletSpawner == null)
        {
            return;
        }

        bulletSpawner.DestroySpawnedBulletEntities();
    }

    private IEnumerator DissolveSpawnedAmmoBox()
    {
        if (spawnedAmmoBox == null)
        {
            yield break;
        }

        DissolveAnimator dissolveAnimator = spawnedAmmoBox.GetComponent<DissolveAnimator>();
        if (dissolveAnimator == null)
        {
            dissolveAnimator = spawnedAmmoBox.GetComponentInChildren<DissolveAnimator>(true);
        }

        if (dissolveAnimator == null)
        {
            Debug.LogWarning("Ammo box has no DissolveAnimator assigned. Destroying it without dissolve animation.", this);
            Destroy(spawnedAmmoBox);
            spawnedAmmoBox = null;
            spawnedAmmoBoxAnimator = null;
            yield break;
        }

        yield return dissolveAnimator.PlayAndWait();
        spawnedAmmoBox = null;
        spawnedAmmoBoxAnimator = null;
    }

    private static T ApplyEase<T>(T tween, AnimationCurve curve) where T : Tween
    {
        return curve != null ? tween.SetEase(curve) : tween.SetEase(Ease.Linear);
    }

    private void LogShellInventory(string message)
    {
        if (!logShellInventory)
        {
            return;
        }

        Debug.Log(message, this);
    }

    private void KillOwnedTweens()
    {
        activeTransformTween?.Kill();
        activeTransformTween = null;
    }

    private void CaptureInitialReloadTransforms()
    {
        Transform cameraTransform = GetCameraTransform();
        if (cameraTransform != null)
        {
            initialCameraPosition = cameraTransform.position;
            initialCameraRotation = cameraTransform.rotation;
        }

        if (shotGun != null)
        {
            initialShotGunPosition = shotGun.position;
            initialShotGunRotation = shotGun.rotation;
            initialShotGunScale = shotGun.localScale;
        }
    }

    private Transform GetCameraTransform()
    {
        if (cameraMovement != null)
        {
            return cameraMovement.transform;
        }

        return cameraLook != null ? cameraLook.transform : null;
    }

    private void SetPlayerInputLocked(bool locked)
    {
        GameEventBus.Publish(new PlayerInputLockChangedEvent(locked));

        if (cameraMovement != null)
        {
            cameraMovement.SetMovementLocked(locked);
        }

        if (cameraLook != null)
        {
            cameraLook.SetLookLocked(locked);
        }
    }

}
