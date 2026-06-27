using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string defaultStateName = "Idle";
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private int hitLayerIndex;
    [SerializeField, Min(0.01f)] private float hitPlaybackDuration = 0.5f;
    [SerializeField, Min(0f)] private float returnTransitionDuration = 0.15f;

    private IDisposable characterHealthChangedSubscription;
    private Coroutine hitCoroutine;
    private int defaultStateHash;
    private int hitStateHash;
    private float defaultAnimatorSpeed = 1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        defaultStateHash = Animator.StringToHash(defaultStateName);
        hitStateHash = Animator.StringToHash(hitStateName);
        if (animator != null)
        {
            defaultAnimatorSpeed = animator.speed;
        }
    }

    private void OnEnable()
    {
        characterHealthChangedSubscription = GameEventBus.Subscribe<CharacterHealthChangedEvent>(HandleCharacterHealthChanged);
    }

    private void OnDisable()
    {
        characterHealthChangedSubscription?.Dispose();
        characterHealthChangedSubscription = null;

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
            hitCoroutine = null;
        }

        if (animator != null)
        {
            animator.speed = defaultAnimatorSpeed;
        }
    }

    private void HandleCharacterHealthChanged(CharacterHealthChangedEvent evt)
    {
        if (evt.Character != GameCharacter.Enemy || evt.Delta >= 0)
        {
            return;
        }

        PlayHit();
    }

    private void PlayHit()
    {
        if (animator == null || string.IsNullOrWhiteSpace(hitStateName))
        {
            return;
        }

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }

        hitCoroutine = StartCoroutine(PlayHitCoroutine());
    }

    private IEnumerator PlayHitCoroutine()
    {
        defaultAnimatorSpeed = animator.speed;
        float clipLength = GetClipLength(hitStateName);
        if (clipLength > 0f)
        {
            animator.speed = clipLength / hitPlaybackDuration;
        }

        animator.Play(hitStateHash, hitLayerIndex, 0f);
        animator.Update(0f);

        yield return new WaitForSeconds(hitPlaybackDuration);

        animator.speed = defaultAnimatorSpeed;
        PlayDefaultState();
        hitCoroutine = null;
    }

    private void PlayDefaultState()
    {
        if (animator == null || string.IsNullOrWhiteSpace(defaultStateName))
        {
            return;
        }

        if (returnTransitionDuration > 0f)
        {
            animator.CrossFade(defaultStateHash, returnTransitionDuration, hitLayerIndex, 0f);
        }
        else
        {
            animator.Play(defaultStateHash, hitLayerIndex, 0f);
        }

        animator.Update(0f);
    }

    private float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(clipName))
        {
            return 0f;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == clipName)
            {
                return clip.length;
            }
        }

        return 0f;
    }
}
