using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class ShotGunShootPlayerAnimator : MonoBehaviour
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

    [Header("Live Fire Effects")]
    [SerializeField, FormerlySerializedAs("fireEffect")] private Transform playerShootsPlayerFireEffect;
    [SerializeField] private Transform playerShootsEnemyFireEffect;
    [SerializeField] private Transform enemyShootsPlayerFireEffect;
    [SerializeField] private Transform enemyShootsEnemyFireEffect;
    [SerializeField, Min(0f)] private float fireEffectActiveDuration = 0.2f;

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
    private Coroutine fireEffectCoroutine;
    public ShotGunShellKind LastPlayedShellKind { get; private set; }
    public ShotGunFireEffectContext LastPlayedFireEffectContext { get; private set; }

    private void Awake()
    {
        if (shotGun == null)
        {
            shotGun = transform;
        }
    }

    public IEnumerator PlayShootPlayer(ShotGunShellKind shellKind)
    {
        yield return PlayShootPlayer(shellKind, ShotGunFireEffectContext.PlayerShootsPlayer);
    }

    public IEnumerator PlayShootPlayer(ShotGunShellKind shellKind, ShotGunFireEffectContext fireEffectContext)
    {
        if (shotGun == null)
        {
            yield break;
        }

        LastPlayedShellKind = shellKind;
        LastPlayedFireEffectContext = fireEffectContext;
        yield return PlayShake();
        switch (shellKind)
        {
            case ShotGunShellKind.Live:
                yield return PlayLiveFire(fireEffectContext);
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

    private IEnumerator PlayLiveFire(ShotGunFireEffectContext fireEffectContext)
    {
        PlayFireEffect(fireEffectContext);
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

        GameEventBus.Publish(new ShotGunFiredEvent(LastPlayedShellKind));

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

    private void PlayFireEffect(ShotGunFireEffectContext fireEffectContext)
    {
        Transform effect = GetFireEffect(fireEffectContext);
        if (effect == null)
        {
            Debug.LogWarning($"ShotGun live fire has no Fire Effect assigned for {fireEffectContext}.", this);
            return;
        }

        if (fireEffectCoroutine != null)
        {
            StopCoroutine(fireEffectCoroutine);
        }

        fireEffectCoroutine = StartCoroutine(PlayFireEffectCoroutine(effect));
    }

    private IEnumerator PlayFireEffectCoroutine(Transform effect)
    {
        SetFireEffectActive(effect, true);

        if (fireEffectActiveDuration > 0f)
        {
            yield return new WaitForSeconds(fireEffectActiveDuration);
            SetFireEffectActive(effect, false);
        }

        fireEffectCoroutine = null;
    }

    private Transform GetFireEffect(ShotGunFireEffectContext fireEffectContext)
    {
        Transform effect = GetOwnFireEffect(fireEffectContext);
        if (effect == null && TryGetComponent(out ShotGunShootEnemyAnimator enemyAnimator))
        {
            effect = enemyAnimator.GetFireEffectReference(fireEffectContext);
        }

        return effect;
    }

    public Transform GetFireEffectReference(ShotGunFireEffectContext fireEffectContext)
    {
        return GetOwnFireEffect(fireEffectContext);
    }

    private Transform GetOwnFireEffect(ShotGunFireEffectContext fireEffectContext)
    {
        switch (fireEffectContext)
        {
            case ShotGunFireEffectContext.PlayerShootsPlayer:
                return playerShootsPlayerFireEffect;
            case ShotGunFireEffectContext.PlayerShootsEnemy:
                return playerShootsEnemyFireEffect;
            case ShotGunFireEffectContext.EnemyShootsPlayer:
                return enemyShootsPlayerFireEffect;
            case ShotGunFireEffectContext.EnemyShootsEnemy:
                return enemyShootsEnemyFireEffect;
            default:
                return null;
        }
    }

    private static void SetFireEffectActive(Transform effect, bool active)
    {
        effect.gameObject.SetActive(active);

        ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (active)
            {
                particleSystems[i].Clear(true);
                particleSystems[i].Play(true);
            }
            else
            {
                particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private static T ApplyEase<T>(T tween, AnimationCurve curve) where T : Tween
    {
        return curve != null ? tween.SetEase(curve) : tween.SetEase(Ease.Linear);
    }

    private void OnDisable()
    {
        activeSequence?.Kill();
        activeSequence = null;
        if (fireEffectCoroutine != null)
        {
            StopCoroutine(fireEffectCoroutine);
            fireEffectCoroutine = null;
        }

        DisableFireEffect(playerShootsPlayerFireEffect);
        DisableFireEffect(playerShootsEnemyFireEffect);
        DisableFireEffect(enemyShootsPlayerFireEffect);
        DisableFireEffect(enemyShootsEnemyFireEffect);
    }

    private static void DisableFireEffect(Transform effect)
    {
        if (effect != null)
        {
            SetFireEffectActive(effect, false);
        }
    }
}
