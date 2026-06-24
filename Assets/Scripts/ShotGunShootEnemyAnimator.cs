using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class ShotGunShootEnemyAnimator : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("revolver")] private Transform shotGun;

    [Header("Shake")]
    [SerializeField, Min(0f)] private float shakeDuration = 1.2f;
    [SerializeField] private Vector3 shakeLocalPositionOffset = new Vector3(0.015f, 0.01f, 0f);
    [SerializeField] private Vector3 shakeLocalEulerOffset = new Vector3(0f, 0f, 1.5f);
    [SerializeField, Min(0.01f)] private float shakeLoopHalfDuration = 0.08f;
    [SerializeField] private AnimationCurve shakeCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Live Fire")]
    [SerializeField] private Vector3 liveRecoilLocalPositionOffset = new Vector3(0f, 0f, -0.12f);
    [SerializeField] private Vector3 liveRecoilLocalEulerOffset = new Vector3(-6f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float liveRecoilOutDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float liveRecoilReturnDuration = 0.16f;
    [SerializeField] private AnimationCurve liveRecoilCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Blank Fire")]
    [SerializeField] private Vector3 blankRecoilLocalPositionOffset = new Vector3(0f, 0f, -0.035f);
    [SerializeField] private Vector3 blankRecoilLocalEulerOffset = new Vector3(-1.5f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float blankRecoilOutDuration = 0.05f;
    [SerializeField, Min(0.01f)] private float blankRecoilReturnDuration = 0.1f;
    [SerializeField] private AnimationCurve blankRecoilCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Optional Child Kick")]
    [SerializeField] private string childKickName;
    [SerializeField] private Vector3 liveChildKickLocalEulerOffset;
    [SerializeField] private Vector3 blankChildKickLocalEulerOffset;
    [SerializeField, Min(0.01f)] private float childKickOutDuration = 0.05f;
    [SerializeField, Min(0.01f)] private float childKickReturnDuration = 0.12f;
    [SerializeField] private AnimationCurve childKickCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    private Sequence activeSequence;
    public ShotGunShellKind LastPlayedShellKind { get; private set; }

    private void Awake()
    {
        if (shotGun == null)
        {
            shotGun = transform;
        }
    }

    public IEnumerator PlayShootEnemy(ShotGunShellKind shellKind)
    {
        if (shotGun == null)
        {
            yield break;
        }

        LastPlayedShellKind = shellKind;
        yield return PlayShake();
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                yield return PlayLiveFire();
                break;
            case ShotGunShellKind.Blank:
                yield return PlayBlankFire();
                break;
            default:
                yield return PlayBlankFire();
                break;
        }
    }

    private IEnumerator PlayShake()
    {
        if (shakeDuration <= 0f)
        {
            yield break;
        }

        Vector3 startLocalPosition = shotGun.localPosition;
        Quaternion startLocalRotation = shotGun.localRotation;
        Vector3 shakeLocalPosition = startLocalPosition + shakeLocalPositionOffset;
        Quaternion shakeLocalRotation = startLocalRotation * Quaternion.Euler(shakeLocalEulerOffset);

        shotGun.DOKill();
        activeSequence = DOTween.Sequence();
        activeSequence.Append(ApplyEase(shotGun.DOLocalMove(shakeLocalPosition, shakeLoopHalfDuration), shakeCurve));
        activeSequence.Join(ApplyEase(shotGun.DOLocalRotateQuaternion(shakeLocalRotation, shakeLoopHalfDuration), shakeCurve));
        activeSequence.Append(ApplyEase(shotGun.DOLocalMove(startLocalPosition, shakeLoopHalfDuration), shakeCurve));
        activeSequence.Join(ApplyEase(shotGun.DOLocalRotateQuaternion(startLocalRotation, shakeLoopHalfDuration), shakeCurve));
        activeSequence.SetLoops(Mathf.Max(1, Mathf.CeilToInt(shakeDuration / (shakeLoopHalfDuration * 2f))));

        yield return activeSequence.WaitForCompletion();
        activeSequence = null;
        shotGun.localPosition = startLocalPosition;
        shotGun.localRotation = startLocalRotation;
    }

    private IEnumerator PlayLiveFire()
    {
        yield return PlayFire(
            liveRecoilLocalPositionOffset,
            liveRecoilLocalEulerOffset,
            liveRecoilOutDuration,
            liveRecoilReturnDuration,
            liveRecoilCurve,
            liveChildKickLocalEulerOffset);
    }

    private IEnumerator PlayBlankFire()
    {
        yield return PlayFire(
            blankRecoilLocalPositionOffset,
            blankRecoilLocalEulerOffset,
            blankRecoilOutDuration,
            blankRecoilReturnDuration,
            blankRecoilCurve,
            blankChildKickLocalEulerOffset);
    }

    private IEnumerator PlayFire(
        Vector3 recoilPositionOffset,
        Vector3 recoilEulerOffset,
        float recoilOutDuration,
        float recoilReturnDuration,
        AnimationCurve recoilCurve,
        Vector3 childKickEulerOffset)
    {
        Vector3 startLocalPosition = shotGun.localPosition;
        Quaternion startLocalRotation = shotGun.localRotation;
        Vector3 recoilLocalPosition = startLocalPosition + recoilPositionOffset;
        Quaternion recoilLocalRotation = startLocalRotation * Quaternion.Euler(recoilEulerOffset);

        shotGun.DOKill();
        activeSequence = DOTween.Sequence();
        activeSequence.Append(ApplyEase(shotGun.DOLocalMove(recoilLocalPosition, recoilOutDuration), recoilCurve));
        activeSequence.Join(ApplyEase(shotGun.DOLocalRotateQuaternion(recoilLocalRotation, recoilOutDuration), recoilCurve));
        activeSequence.Append(ApplyEase(shotGun.DOLocalMove(startLocalPosition, recoilReturnDuration), recoilCurve));
        activeSequence.Join(ApplyEase(shotGun.DOLocalRotateQuaternion(startLocalRotation, recoilReturnDuration), recoilCurve));

        Transform child = FindChild(childKickName);
        Quaternion childStartLocalRotation = Quaternion.identity;
        if (child != null)
        {
            childStartLocalRotation = child.localRotation;
            Quaternion kickedLocalRotation = childStartLocalRotation * Quaternion.Euler(childKickEulerOffset);
            child.DOKill();
            Sequence childSequence = DOTween.Sequence();
            childSequence.Append(ApplyEase(child.DOLocalRotateQuaternion(kickedLocalRotation, childKickOutDuration), childKickCurve));
            childSequence.Append(ApplyEase(child.DOLocalRotateQuaternion(childStartLocalRotation, childKickReturnDuration), childKickCurve));
            activeSequence.Join(childSequence);
        }

        yield return activeSequence.WaitForCompletion();
        activeSequence = null;
        shotGun.localPosition = startLocalPosition;
        shotGun.localRotation = startLocalRotation;
        if (child != null)
        {
            child.localRotation = childStartLocalRotation;
        }
    }

    private Transform FindChild(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName) || shotGun == null)
        {
            return null;
        }

        Transform[] children = shotGun.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
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
        activeSequence?.Kill();
        activeSequence = null;
    }
}
