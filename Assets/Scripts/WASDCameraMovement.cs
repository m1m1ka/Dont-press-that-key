using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WASDCameraMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(1f)] private float sprintMultiplier = 2f;
    [SerializeField] private bool stayLevel = true;
    [SerializeField] private bool disableWhileFocused = true;

    private IDisposable focusStateChangedSubscription;
    private bool focusLocked;
    private bool movementLocked;

    private void OnEnable()
    {
        focusStateChangedSubscription = GameEventBus.Subscribe<FocusStateChangedEvent>(HandleFocusStateChanged);
    }

    private void OnDisable()
    {
        focusStateChangedSubscription?.Dispose();
        focusStateChangedSubscription = null;
    }

    private void Update()
    {
        if (movementLocked)
        {
            return;
        }

        if (disableWhileFocused && focusLocked)
        {
            return;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude <= 0f)
        {
            return;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        if (stayLevel)
        {
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
        }

        float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        Vector3 movement = (right * input.x + forward * input.y) * speed * Time.deltaTime;
        transform.position += movement;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    private void HandleFocusStateChanged(FocusStateChangedEvent evt)
    {
        focusLocked = evt.Focused;
    }
}
