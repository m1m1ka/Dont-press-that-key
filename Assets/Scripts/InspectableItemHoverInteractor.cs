using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class InspectableItemHoverInteractor : MonoBehaviour
{
    private const string NonInteractiveLayerName = "Non-Interactive";
    private const string NonInteractiveLayerNameWithTrailingSpace = "Non-Interactive ";

    [SerializeField] private Camera playerCamera;
    [SerializeField] private InteractionUIController interactionUI;
    [SerializeField] private Shader defaultOutlineShader;
    [SerializeField] private Shader defaultOutlineMaskShader;
    [SerializeField, Min(0f)] private float maximumHoverDistance = 100f;
    [SerializeField] private LayerMask hoverLayers = ~0;

    private IHoverableTarget currentHover;
    private IFocusableTarget currentFocus;
    private IDisposable reloadStartedSubscription;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponent<Camera>() ?? Camera.main;
        }
    }

    private void OnEnable()
    {
        reloadStartedSubscription = GameEventBus.Subscribe<ReloadStartedEvent>(HandleReloadStarted);
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            return;
        }

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        RaycastHit? hit = pointerOverUi ? null : FindHit();
        UpdateHover(hit);

        if (!Input.GetMouseButtonDown(0) || pointerOverUi)
        {
            return;
        }

        HandleClick(hit);
    }

    private void UpdateHover(RaycastHit? hit)
    {
        IHoverableTarget hoveredTarget = hit.HasValue ? FindCapability<IHoverableTarget>(hit.Value.transform) : null;
        if (hoveredTarget != null && !hoveredTarget.CanHover)
        {
            hoveredTarget = null;
        }

        if (hoveredTarget == currentHover)
        {
            return;
        }

        ClearCurrentHover();
        currentHover = hoveredTarget;
        if (currentHover == null)
        {
            return;
        }

        if (currentHover is InspectableItem inspectableItem)
        {
            inspectableItem.ApplyOutlineDefaults(defaultOutlineShader, defaultOutlineMaskShader);
            GameEventBus.Publish(new InspectableHoveredEvent(inspectableItem));
        }

        currentHover.SetHovered(true);
        if (interactionUI != null)
        {
            interactionUI.ShowHoveredItem(currentHover.DisplayName, currentHover.Owner);
        }
    }

    private void HandleClick(RaycastHit? hit)
    {
        IFocusableTarget focusableTarget = hit.HasValue ? FindCapability<IFocusableTarget>(hit.Value.transform) : null;
        if (focusableTarget != null && focusableTarget.CanFocus && !focusableTarget.IsFocused)
        {
            SetFocusedTarget(focusableTarget);
            return;
        }

        IInteractableTarget interactableTarget = hit.HasValue ? FindCapability<IInteractableTarget>(hit.Value.transform) : null;
        if (interactableTarget != null && interactableTarget.CanInteract)
        {
            interactableTarget.Interact();
            return;
        }

        if (focusableTarget != null && focusableTarget.IsFocused)
        {
            return;
        }

        SetFocusedTarget(null);
    }

    private void SetFocusedTarget(IFocusableTarget focusableTarget)
    {
        if (currentFocus == focusableTarget)
        {
            if (currentFocus != null && !currentFocus.IsFocused)
            {
                ClearCurrentHover();
                currentFocus.SetFocused(true);
                GameEventBus.Publish(new FocusStateChangedEvent(true));
                if (interactionUI != null)
                {
                    interactionUI.SetFocusActionsVisible(currentFocus.ShowFocusActions);
                }
            }

            return;
        }

        if (currentFocus != null)
        {
            currentFocus.SetFocused(false);
        }

        currentFocus = focusableTarget;
        if (currentFocus != null)
        {
            ClearCurrentHover();
            currentFocus.SetFocused(true);
        }

        GameEventBus.Publish(new FocusStateChangedEvent(currentFocus != null));
        if (interactionUI != null)
        {
            interactionUI.SetFocusActionsVisible(currentFocus != null && currentFocus.ShowFocusActions);
        }
    }

    private RaycastHit? FindHit()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(
            ray,
            out RaycastHit hit,
            maximumHoverDistance,
            GetInteractionLayerMask(hoverLayers),
            QueryTriggerInteraction.Collide)
            ? hit
            : null;
    }

    private T FindCapability<T>(Transform hitTransform) where T : class
    {
        Transform cursor = hitTransform;
        while (cursor != null)
        {
            Component[] components = cursor.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is T capability)
                {
                    return capability;
                }
            }

            cursor = cursor.parent;
        }

        return null;
    }

    private static int GetInteractionLayerMask(LayerMask configuredLayers)
    {
        int layerMask = configuredLayers.value;
        int nonInteractiveLayer = LayerMask.NameToLayer(NonInteractiveLayerName);
        if (nonInteractiveLayer < 0)
        {
            nonInteractiveLayer = LayerMask.NameToLayer(NonInteractiveLayerNameWithTrailingSpace);
        }

        return nonInteractiveLayer >= 0
            ? layerMask & ~(1 << nonInteractiveLayer)
            : layerMask;
    }

    private void ClearCurrentHover()
    {
        if (currentHover == null)
        {
            return;
        }

        currentHover.SetHovered(false);
        if (currentHover is InspectableItem)
        {
            GameEventBus.Publish(new InspectableHoverClearedEvent());
        }

        if (interactionUI != null)
        {
            interactionUI.HideHoveredItem(currentHover.Owner);
        }

        currentHover = null;
    }

    private void OnDisable()
    {
        reloadStartedSubscription?.Dispose();
        reloadStartedSubscription = null;
        ClearCurrentHover();
        if (currentFocus != null)
        {
            currentFocus.SetFocused(false);
            currentFocus = null;
        }

        GameEventBus.Publish(new FocusStateChangedEvent(false));
        if (interactionUI != null)
        {
            interactionUI.SetFocusActionsVisible(false);
        }
    }

    private void HandleReloadStarted(ReloadStartedEvent evt)
    {
        ClearCurrentHover();
        if (currentFocus != null)
        {
            currentFocus.SetFocused(false);
            currentFocus = null;
        }

        GameEventBus.Publish(new FocusStateChangedEvent(false));
        if (interactionUI != null)
        {
            interactionUI.SetFocusActionsVisible(false);
        }
    }
}
