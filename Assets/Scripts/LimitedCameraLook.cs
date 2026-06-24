using System;
using UnityEngine;

public sealed class LimitedCameraLook : MonoBehaviour
{
    [Header("Look")]
    [SerializeField, Min(0f)] private float sensitivity = 120f;
    [SerializeField, Min(0f)] private float smoothTime = 0.04f;

    [Header("Angle Limits (relative to the starting rotation)")]
    [SerializeField] private Vector2 horizontalLimits = new Vector2(-80f, 80f);
    [SerializeField] private Vector2 verticalLimits = new Vector2(-60f, 60f);

    private Quaternion startingRotation;
    private Vector2 targetAngles;
    private Vector2 currentAngles;
    private Vector2 smoothVelocity;
    private IDisposable focusStateChangedSubscription;
    private bool focusLocked;
    private bool lookLocked;
    private bool externalControl;

    private void Awake()
    {
        startingRotation = transform.localRotation;
        ConfineCursor();
    }

    private void OnEnable()
    {
        focusStateChangedSubscription = GameEventBus.Subscribe<FocusStateChangedEvent>(HandleFocusStateChanged);
    }

    private void Update()
    {
        ConfineCursor();
        if (!Application.isFocused)
        {
            return;
        }

        if (lookLocked || focusLocked)
        {
            smoothVelocity = Vector2.zero;
            return;
        }

        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        targetAngles.x += mouseDelta.x * sensitivity * Time.unscaledDeltaTime;
        targetAngles.y -= mouseDelta.y * sensitivity * Time.unscaledDeltaTime;

        targetAngles.x = Mathf.Clamp(targetAngles.x, horizontalLimits.x, horizontalLimits.y);
        targetAngles.y = Mathf.Clamp(targetAngles.y, verticalLimits.x, verticalLimits.y);

        currentAngles = smoothTime > 0f
            ? Vector2.SmoothDamp(currentAngles, targetAngles, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime)
            : targetAngles;
    }

    private void LateUpdate()
    {
        if (externalControl)
        {
            return;
        }

        transform.localRotation = startingRotation * Quaternion.Euler(currentAngles.y, currentAngles.x, 0f);
    }

    private void OnDisable()
    {
        focusStateChangedSubscription?.Dispose();
        focusStateChangedSubscription = null;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnValidate()
    {
        horizontalLimits = OrderLimits(horizontalLimits, -180f, 180f);
        verticalLimits = OrderLimits(verticalLimits, -89f, 89f);
    }

    public void SetLookLocked(bool locked)
    {
        lookLocked = locked;
        if (lookLocked)
        {
            smoothVelocity = Vector2.zero;
        }
    }

    public void SetExternalControl(bool controlled)
    {
        externalControl = controlled;
        if (externalControl)
        {
            smoothVelocity = Vector2.zero;
        }
    }

    public void ResetLookToStartingRotation()
    {
        targetAngles = Vector2.zero;
        currentAngles = Vector2.zero;
        smoothVelocity = Vector2.zero;
        transform.localRotation = startingRotation;
    }

    private void HandleFocusStateChanged(FocusStateChangedEvent evt)
    {
        focusLocked = evt.Focused;
    }

    private static Vector2 OrderLimits(Vector2 limits, float minimum, float maximum)
    {
        float lower = Mathf.Clamp(Mathf.Min(limits.x, limits.y), minimum, maximum);
        float upper = Mathf.Clamp(Mathf.Max(limits.x, limits.y), minimum, maximum);
        return new Vector2(lower, upper);
    }

    private static void ConfineCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }
}
