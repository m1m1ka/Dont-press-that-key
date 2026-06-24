using System.Collections;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AmmoBoxAnimator : MonoBehaviour
{
    [SerializeField] private string coverChildName = "Cover";

    [Header("Drop")]
    [SerializeField] private Vector3 landingPosition;
    [SerializeField] private Vector3 landingRotation;
    [SerializeField, Min(0.01f)] private float dropDuration = 0.8f;
    [SerializeField] private AnimationCurve dropCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Open")]
    [SerializeField] private Vector3 closedCoverEulerAngles = new Vector3(-90f, 0f, 0f);
    [SerializeField] private Vector3 openCoverEulerAngles = new Vector3(-210f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float openDuration = 0.8f;
    [SerializeField] private AnimationCurve openCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    private Tween activeTween;

    public void ConfigureDrop(Vector3 position, Vector3 rotation, float duration, AnimationCurve curve)
    {
        landingPosition = position;
        landingRotation = rotation;
        dropDuration = duration;
        dropCurve = curve;
    }

    public void ConfigureCover(string childName, Vector3 closedAngles, Vector3 openAngles, float duration, AnimationCurve curve)
    {
        coverChildName = childName;
        closedCoverEulerAngles = closedAngles;
        openCoverEulerAngles = openAngles;
        openDuration = duration;
        openCurve = curve;
    }

    public IEnumerator PlayDrop()
    {
        DisablePhysics();
        transform.DOKill();
        Sequence sequence = DOTween.Sequence();
        sequence.Join(ApplyEase(transform.DOMove(landingPosition, dropDuration), dropCurve));
        sequence.Join(ApplyEase(transform.DORotate(landingRotation, dropDuration), dropCurve));
        activeTween = sequence;
        yield return sequence.WaitForCompletion();
        activeTween = null;
        transform.SetPositionAndRotation(landingPosition, Quaternion.Euler(landingRotation));
    }

    public IEnumerator PlayOpenCover()
    {
        Transform cover = FindCover();
        if (cover == null)
        {
            Debug.LogWarning($"Ammo box animator could not find a child named '{coverChildName}'.", this);
            yield break;
        }

        cover.DOKill();
        cover.localRotation = Quaternion.Euler(closedCoverEulerAngles);
        activeTween = ApplyEase(
            cover.DOLocalRotate(openCoverEulerAngles, openDuration, RotateMode.FastBeyond360),
            openCurve);
        yield return activeTween.WaitForCompletion();
        activeTween = null;
        cover.localRotation = Quaternion.Euler(openCoverEulerAngles);
    }

    public void DisablePhysics()
    {
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            return;
        }

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    private Transform FindCover()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == coverChildName)
            {
                return children[i];
            }
        }

        return null;
    }

    private static T ApplyEase<T>(T tween, AnimationCurve curve) where T : Tween
    {
        return curve != null ? tween.SetEase(curve) : tween.SetEase(Ease.Linear);
    }

    private void OnDisable()
    {
        activeTween?.Kill();
        activeTween = null;
    }
}
