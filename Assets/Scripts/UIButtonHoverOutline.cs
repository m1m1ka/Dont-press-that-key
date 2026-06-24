using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIButtonHoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Outline outline;
    [SerializeField] private bool hideOutlineOnAwake = true;

    private void Awake()
    {
        ResolveOutline();

        if (hideOutlineOnAwake)
        {
            SetOutlineEnabled(false);
        }
    }

    private void OnDisable()
    {
        SetOutlineEnabled(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetOutlineEnabled(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetOutlineEnabled(false);
    }

    private void ResolveOutline()
    {
        if (outline != null)
        {
            return;
        }

        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = GetComponentInChildren<Outline>(true);
        }
    }

    private void SetOutlineEnabled(bool enabled)
    {
        ResolveOutline();
        if (outline != null)
        {
            outline.enabled = enabled;
        }
    }
}
