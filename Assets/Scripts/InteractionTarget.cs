using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class InteractionTarget : MonoBehaviour, IHoverableTarget, IInteractableTarget
{
    [SerializeField] private string displayName = "Interaction Target";
    [SerializeField] private bool hoverable = true;
    [SerializeField] private bool interactable = true;
    [SerializeField] private InspectableItem highlightItem;
    [SerializeField] private UnityEvent onInteract;

    public string DisplayName => displayName;
    public UnityEngine.Object Owner => this;
    public bool CanHover => enabled && hoverable;
    public bool CanInteract => enabled && interactable;

    private void Awake()
    {
        if (highlightItem == null)
        {
            highlightItem = GetComponent<InspectableItem>();
        }
    }

    public void SetHovered(bool hovered)
    {
        if (highlightItem != null)
        {
            highlightItem.SetHighlighted(hovered);
        }
    }

    public void Interact()
    {
        onInteract?.Invoke();
    }
}
