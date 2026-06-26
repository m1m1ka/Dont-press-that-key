using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FocusableObject : MonoBehaviour, IFocusableTarget
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform target;
    [SerializeField] private bool showFocusActions;

    [Header("Focused Transform (camera space)")]
    [SerializeField, Min(0.05f)] private float focusDistance = 0.8f;
    [SerializeField] private Vector3 focusPosition = new Vector3(0.2f, -0.12f, 0f);
    [SerializeField] private Vector3 focusRotation = new Vector3(5f, 100f, 5f);
    [SerializeField] private Vector3 focusScale = new Vector3(1.5f, 1.5f, 1.5f);

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.45f;
    [SerializeField] private AnimationCurve transitionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool isFocused;
    private bool isTransitioning;
    private bool externalControl;
    private Sequence transitionSequence;

    public bool IsFocused => isFocused;
    public bool CanFocus => enabled && !externalControl && target != null && playerCamera != null;
    public bool ShowFocusActions => showFocusActions;
    public UnityEngine.Object Owner => this;
    public Transform Target => target;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        CaptureOriginalTransform();
    }

    private void LateUpdate()
    {
        if (externalControl || !isFocused || isTransitioning)
        {
            return;
        }

        ApplyFocusedTransform();
    }

    public void SetFocused(bool focused)
    {
        if (!CanFocus && focused)
        {
            return;
        }

        isFocused = focused;
        isTransitioning = true;
        transitionSequence?.Kill();
        target.DOKill();

        GetDestination(focused, out Vector3 position, out Quaternion rotation, out Vector3 scale);
        transitionSequence = DOTween.Sequence().SetUpdate(true);
        transitionSequence.Join(ApplyEase(target.DOMove(position, transitionDuration), transitionCurve));
        transitionSequence.Join(ApplyEase(target.DORotateQuaternion(rotation, transitionDuration), transitionCurve));
        transitionSequence.Join(ApplyEase(target.DOScale(scale, transitionDuration), transitionCurve));
        transitionSequence.OnComplete(() =>
        {
            isTransitioning = false;
            transitionSequence = null;

            if (isFocused)
            {
                ApplyFocusedTransform();
            }
            else
            {
                RestoreOriginalTransform();
            }
        });
    }

    public void BeginExternalControl()
    {
        externalControl = true;
        isFocused = false;
        isTransitioning = false;
        transitionSequence?.Kill();
        transitionSequence = null;
        if (target != null)
        {
            target.DOKill();
        }
    }

    public void EndExternalControl()
    {
        externalControl = false;
    }

    public void ApplyExternalFocusedTransform()
    {
        if (target == null || playerCamera == null)
        {
            return;
        }

        GetFocusedDestination(out Vector3 position, out Quaternion rotation, out Vector3 scale);
        target.SetPositionAndRotation(position, rotation);
        target.localScale = scale;
    }

    public void RestoreOriginalPose()
    {
        transitionSequence?.Kill();
        transitionSequence = null;
        if (target != null)
        {
            target.DOKill();
            RestoreOriginalTransform();
        }
    }

    public void CaptureOriginalTransform()
    {
        if (target == null)
        {
            return;
        }

        originalPosition = target.position;
        originalRotation = target.rotation;
        originalScale = target.localScale;
    }

    public void Configure(
        Camera camera,
        Transform focusTarget,
        bool actionsVisibleWhenFocused,
        float distance,
        Vector3 position,
        Vector3 rotation,
        Vector3 scale,
        float duration,
        AnimationCurve curve)
    {
        playerCamera = camera != null ? camera : playerCamera;
        target = focusTarget != null ? focusTarget : target;
        showFocusActions = actionsVisibleWhenFocused;
        focusDistance = distance;
        focusPosition = position;
        focusRotation = rotation;
        focusScale = scale;
        transitionDuration = duration;
        transitionCurve = curve;
        CaptureOriginalTransform();
    }

    private void GetDestination(bool focused, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        if (focused)
        {
            GetFocusedDestination(out position, out rotation, out scale);
            return;
        }

        position = originalPosition;
        rotation = originalRotation;
        scale = originalScale;
    }

    private void GetFocusedDestination(out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        Vector3 cameraLocalPosition = focusPosition + Vector3.forward * focusDistance;
        position = playerCamera.transform.TransformPoint(cameraLocalPosition);
        rotation = playerCamera.transform.rotation * Quaternion.Euler(focusRotation);
        scale = focusScale;
    }

    private void ApplyFocusedTransform()
    {
        GetFocusedDestination(out Vector3 position, out Quaternion rotation, out Vector3 scale);
        target.SetPositionAndRotation(position, rotation);
        target.localScale = scale;
    }

    private void RestoreOriginalTransform()
    {
        target.SetPositionAndRotation(originalPosition, originalRotation);
        target.localScale = originalScale;
    }

    private static T ApplyEase<T>(T tween, AnimationCurve curve) where T : Tween
    {
        return curve != null ? tween.SetEase(curve) : tween.SetEase(Ease.Linear);
    }

    private void OnDisable()
    {
        transitionSequence?.Kill();
        transitionSequence = null;
        if (target != null)
        {
            target.DOKill();
            RestoreOriginalTransform();
        }
    }
}
