using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InspectableItem))]
[RequireComponent(typeof(FocusableObject))]
public sealed class UsableItem : MonoBehaviour
{
    [Flags]
    public enum ItemEffect
    {
        None = 0,
        HealPlayer = 1 << 0,
        AddNextPlayerShotDamage = 1 << 1,
        AddNextShotDamage = 1 << 2,
        RevealCurrentShotGunShell = 1 << 3,
        SwapCurrentAndNextShotGunShell = 1 << 4,
        SkipNextEnemyTurnAfterPlayerShootsEnemy = 1 << 5,
        InvertCurrentShotGunShell = 1 << 6
    }

    [SerializeField] private ItemEffect effects = ItemEffect.HealPlayer;
    [SerializeField, Min(0)] private int playerHealAmount = 1;
    [SerializeField, Min(0)] private int nextPlayerShotDamageBonus = 1;
    [SerializeField, Min(0)] private int nextShotDamageBonus = 1;
    [SerializeField] private bool consumeOnUse = true;
    [SerializeField] private GameObject rootToDisableOnConsume;
    [SerializeField] private DissolveAnimator dissolveAnimator;
    [SerializeField, Min(0.01f)] private float focusedYRotationDuration = 1.2f;

    private FocusableObject focusableObject;
    private IDisposable useButtonClickedSubscription;
    private float focusedYRotationAngle;
    private bool rotatingForFocus;
    private bool consumed;
    private bool dissolving;

    public bool CanUse => enabled && !consumed;

    private void Awake()
    {
        focusableObject = GetComponent<FocusableObject>();
        focusableObject.SetFocusActionsVisibleWhenFocused(true);

        if (rootToDisableOnConsume == null)
        {
            rootToDisableOnConsume = gameObject;
        }

        if (dissolveAnimator == null)
        {
            dissolveAnimator = GetComponent<DissolveAnimator>();
            if (dissolveAnimator == null)
            {
                dissolveAnimator = GetComponentInChildren<DissolveAnimator>(true);
            }
        }
    }

    private void OnEnable()
    {
        useButtonClickedSubscription = GameEventBus.Subscribe<UseFocusedItemButtonClickedEvent>(HandleUseButtonClicked);
    }

    private void LateUpdate()
    {
        bool shouldRotate = (CanUse || dissolving)
            && focusableObject != null
            && focusableObject.IsFocused
            && !focusableObject.IsTransitioning;

        if (!shouldRotate)
        {
            if (rotatingForFocus)
            {
                StopFocusedRotation();
            }

            return;
        }

        if (!rotatingForFocus)
        {
            StartFocusedRotation();
        }

        focusedYRotationAngle += 360f * Time.deltaTime / focusedYRotationDuration;
        focusableObject.SetFocusedRotationOffset(Quaternion.Euler(0f, focusedYRotationAngle, 0f));
    }

    private void OnDisable()
    {
        useButtonClickedSubscription?.Dispose();
        useButtonClickedSubscription = null;
        StopFocusedRotation();
    }

    public void Use()
    {
        if (!CanUse || focusableObject == null || !focusableObject.IsFocused)
        {
            return;
        }

        GameGlobalManager gameGlobalManager = FindFirstObjectByType<GameGlobalManager>();
        if (gameGlobalManager == null)
        {
            Debug.LogWarning("Cannot use item because no GameGlobalManager exists in the scene.", this);
            return;
        }

        if ((effects & ItemEffect.HealPlayer) != 0)
        {
            gameGlobalManager.HealCharacter(GameCharacter.Player, playerHealAmount);
        }

        if ((effects & ItemEffect.AddNextPlayerShotDamage) != 0)
        {
            gameGlobalManager.AddPlayerNextShotDamageBonus(nextPlayerShotDamageBonus);
        }

        if ((effects & ItemEffect.AddNextShotDamage) != 0)
        {
            gameGlobalManager.AddNextShotDamageBonus(nextShotDamageBonus);
        }

        if ((effects & ItemEffect.SkipNextEnemyTurnAfterPlayerShootsEnemy) != 0)
        {
            gameGlobalManager.SkipNextEnemyTurnAfterPlayerShootsEnemy();
        }

        if ((effects & ItemEffect.SwapCurrentAndNextShotGunShell) != 0)
        {
            GameEventBus.Publish(new SwapCurrentAndNextShotGunShellRequestedEvent());
        }

        if ((effects & ItemEffect.InvertCurrentShotGunShell) != 0)
        {
            GameEventBus.Publish(new InvertCurrentShotGunShellRequestedEvent());
        }

        if (consumeOnUse)
        {
            consumed = true;
            dissolving = true;
            GameEventBus.Publish(new FocusedTargetRemovedEvent(focusableObject));
            StartCoroutine(ConsumeWithDissolve());
        }
    }

    private IEnumerator ConsumeWithDissolve()
    {
        if (dissolveAnimator != null)
        {
            yield return dissolveAnimator.PlayAndWait();
        }

        dissolving = false;
        StopFocusedRotation();

        if ((effects & ItemEffect.RevealCurrentShotGunShell) != 0)
        {
            GameEventBus.Publish(new RevealCurrentShotGunShellConsumedEvent(this));
        }

        if (rootToDisableOnConsume != null)
        {
            rootToDisableOnConsume.SetActive(false);
        }
    }

    private void HandleUseButtonClicked(UseFocusedItemButtonClickedEvent evt)
    {
        Use();
    }

    private void StartFocusedRotation()
    {
        rotatingForFocus = true;
        focusedYRotationAngle = 0f;
    }

    private void StopFocusedRotation()
    {
        rotatingForFocus = false;
        focusedYRotationAngle = 0f;
        focusableObject?.SetFocusedRotationOffset(Quaternion.identity);
    }
}
