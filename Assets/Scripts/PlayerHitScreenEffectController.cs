using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerHitScreenEffectController : MonoBehaviour
{
    [SerializeField] private Image blackOverlay;
    [SerializeField, Min(0f)] private float effectDelay = 0.03f;
    [SerializeField, Min(0.01f)] private float recoverDuration = 1.2f;
    [SerializeField] private AnimationCurve recoverCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    [SerializeField, Min(0f)] private float heartbeatStartDelay = 1f;
    [SerializeField] private string slowHeartbeatSoundId = "HeartBeatSlow";
    [SerializeField] private string fastHeartbeatSoundId = "HeartBeatFast";

    private IDisposable shotGunFiredSubscription;
    private Coroutine hitEffectCoroutine;
    private Tween recoverTween;

    private void Awake()
    {
        ResolveOverlay();
    }

    private void OnEnable()
    {
        shotGunFiredSubscription = GameEventBus.Subscribe<ShotGunFiredEvent>(HandleShotGunFired);
    }

    private void OnDisable()
    {
        shotGunFiredSubscription?.Dispose();
        shotGunFiredSubscription = null;

        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = null;
        }

        recoverTween?.Kill();
        recoverTween = null;
    }

    private void HandleShotGunFired(ShotGunFiredEvent evt)
    {
        if (!IsLiveShotHittingPlayer(evt))
        {
            return;
        }

        PlayHitEffect();
    }

    private void ResolveOverlay()
    {
        if (blackOverlay == null)
        {
            Transform foundOverlay = FindChildByName(transform, "PlayerHitBlackOverlay");
            blackOverlay = foundOverlay != null ? foundOverlay.GetComponent<Image>() : null;
        }

        if (blackOverlay == null)
        {
            blackOverlay = CreateOverlay();
        }

        SetOverlayAlpha(0f);
        blackOverlay.gameObject.SetActive(false);
    }

    private Image CreateOverlay()
    {
        GameObject overlayObject = new GameObject("PlayerHitBlackOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = false;
        return overlayImage;
    }

    private void PlayHitEffect()
    {
        ResolveOverlay();
        if (blackOverlay == null)
        {
            return;
        }

        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
        }

        recoverTween?.Kill();
        recoverTween = null;
        hitEffectCoroutine = StartCoroutine(PlayHitEffectCoroutine());
    }

    private IEnumerator PlayHitEffectCoroutine()
    {
        if (effectDelay > 0f)
        {
            yield return new WaitForSeconds(effectDelay);
        }
        else
        {
            yield return null;
        }

        SoundEffectManager.Instance?.StopAllActiveSounds();

        blackOverlay.gameObject.SetActive(true);
        blackOverlay.transform.SetAsLastSibling();
        SetOverlayAlpha(1f);
        GameEventBus.Publish(new PlayerHitScreenEffectStartedEvent());

        recoverTween = blackOverlay
            .DOFade(0f, recoverDuration)
            .SetEase(recoverCurve)
            .OnComplete(() =>
            {
                blackOverlay.gameObject.SetActive(false);
                recoverTween = null;
                GameEventBus.Publish(new PlayerHitScreenEffectCompletedEvent());
            });

        yield return PlayHeartbeatSequence();
        hitEffectCoroutine = null;
    }

    private IEnumerator PlayHeartbeatSequence()
    {
        if (heartbeatStartDelay > 0f)
        {
            yield return new WaitForSeconds(heartbeatStartDelay);
        }

        SoundEffectManager soundEffectManager = SoundEffectManager.Instance;
        if (soundEffectManager == null)
        {
            yield break;
        }

        float slowHeartbeatDuration = soundEffectManager.PlayAndGetDuration(slowHeartbeatSoundId);
        if (slowHeartbeatDuration > 0f)
        {
            yield return new WaitForSeconds(slowHeartbeatDuration);
        }

        soundEffectManager.Play(fastHeartbeatSoundId);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (blackOverlay == null)
        {
            return;
        }

        Color color = blackOverlay.color;
        color.a = alpha;
        blackOverlay.color = color;
    }

    private static bool IsLiveShotHittingPlayer(ShotGunFiredEvent evt)
    {
        if (evt.ShellKind != ShotGunShellKind.Live || !evt.HasFireContext)
        {
            return false;
        }

        return evt.FireContext == ShotGunFireEffectContext.PlayerShootsPlayer
            || evt.FireContext == ShotGunFireEffectContext.EnemyShootsPlayer;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }
}
