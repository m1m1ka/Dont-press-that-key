using UnityEngine;

public interface IHoverableTarget
{
    string DisplayName { get; }
    UnityEngine.Object Owner { get; }
    bool CanHover { get; }
    void SetHovered(bool hovered);
}

public interface IFocusableTarget
{
    bool IsFocused { get; }
    bool CanFocus { get; }
    bool ShowFocusActions { get; }
    void SetFocused(bool focused);
}

public interface IInteractableTarget
{
    bool CanInteract { get; }
    void Interact();
}
