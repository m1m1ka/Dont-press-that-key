using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class ShotGunReloadAnimator : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("revolver")] private Transform shotGun;
    [SerializeField] private string barrelGuardChildName = "ShotGun_BarrelGuard";

    [Header("Shell Load")]
    [SerializeField] private Vector3 shellLoadLocalEulerOffset = new Vector3(8f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float shellLoadHalfDuration = 0.08f;
    [SerializeField] private AnimationCurve shellLoadCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Barrel Guard")]
    [SerializeField] private float barrelGuardBoltLocalZOffset = -0.12f;
    [SerializeField, Min(0.01f)] private float barrelGuardBoltHalfDuration = 0.12f;
    [SerializeField] private AnimationCurve barrelGuardBoltCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    private Sequence activeSequence;

    private void Awake()
    {
        if (shotGun == null)
        {
            shotGun = transform;
        }
    }

    public IEnumerator PlayReload(int shellCount)
    {
        if (shotGun == null)
        {
            yield break;
        }

        for (int i = 0; i < shellCount; i++)
        {
            yield return PlaySingleShellLoad(i, shellCount);
        }

        yield return PlayBarrelGuardBolt();
    }

    public IEnumerator PlayBolt(Action onBoltPulled = null)
    {
        yield return PlayBarrelGuardBolt(onBoltPulled, onBoltPulled != null);
    }

    private IEnumerator PlaySingleShellLoad(int shellIndex, int shellCount)
    {
        Quaternion startRotation = shotGun.rotation;
        Quaternion loadedRotation = startRotation * Quaternion.Euler(shellLoadLocalEulerOffset);

        GameEventBus.Publish(new ShotGunShellLoadStartedEvent(shotGun, shellIndex, shellCount));

        shotGun.DOKill();
        activeSequence = DOTween.Sequence();
        activeSequence.Append(ApplyEase(shotGun.DORotateQuaternion(loadedRotation, shellLoadHalfDuration), shellLoadCurve));
        activeSequence.Append(ApplyEase(shotGun.DORotateQuaternion(startRotation, shellLoadHalfDuration), shellLoadCurve));
        yield return activeSequence.WaitForCompletion();
        activeSequence = null;
        shotGun.rotation = startRotation;

        GameEventBus.Publish(new ShotGunShellLoadCompletedEvent(shotGun, shellIndex, shellCount));
    }

    private IEnumerator PlayBarrelGuardBolt(Action onBoltPulled = null, bool ejectsShell = false)
    {
        Transform barrelGuard = FindChild(barrelGuardChildName);
        if (barrelGuard == null)
        {
            Debug.LogWarning($"Reload animator could not find a ShotGun child named '{barrelGuardChildName}'.", this);
            yield break;
        }

        Vector3 startLocalPosition = barrelGuard.localPosition;
        Vector3 pulledLocalPosition = startLocalPosition + Vector3.forward * barrelGuardBoltLocalZOffset;

        barrelGuard.DOKill();
        activeSequence = DOTween.Sequence();
        activeSequence.Append(ApplyEase(barrelGuard.DOLocalMove(pulledLocalPosition, barrelGuardBoltHalfDuration), barrelGuardBoltCurve));
        activeSequence.AppendCallback(() => GameEventBus.Publish(new ShotGunBoltPulledEvent(shotGun, ejectsShell)));
        if (onBoltPulled != null)
        {
            activeSequence.AppendCallback(() => onBoltPulled());
        }

        activeSequence.Append(ApplyEase(barrelGuard.DOLocalMove(startLocalPosition, barrelGuardBoltHalfDuration), barrelGuardBoltCurve));
        yield return activeSequence.WaitForCompletion();
        activeSequence = null;
        barrelGuard.localPosition = startLocalPosition;
    }

    private Transform FindChild(string childName)
    {
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
