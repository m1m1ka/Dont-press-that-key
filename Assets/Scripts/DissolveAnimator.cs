using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DissolveAnimator : MonoBehaviour
{
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

    [Header("Targets")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private GameObject destroyTarget;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable;
    [SerializeField] private bool resetMaterialValuesOnPlay = true;
    [SerializeField] private bool destroyOnComplete = true;

    [Header("Emission")]
    [SerializeField, Min(0f)] private float targetEmissionStrength = 4f;
    [SerializeField, Min(0f)] private float emissionRampDuration = 0.25f;
    [SerializeField] private AnimationCurve emissionCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Dissolve")]
    [SerializeField, Min(0.01f)] private float dissolveDuration = 1f;
    [SerializeField] private AnimationCurve dissolveCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    private readonly List<Material> materialInstances = new List<Material>();
    private readonly HashSet<Material> materialInstanceSet = new HashSet<Material>();
    private Sequence activeSequence;
    private bool materialInstancesPrepared;
    private bool completed;

    public bool IsPlaying => activeSequence != null && activeSequence.IsActive() && activeSequence.IsPlaying();

    private void Awake()
    {
        if (destroyTarget == null)
        {
            destroyTarget = gameObject;
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    [ContextMenu("Play Dissolve")]
    public void Play()
    {
        completed = false;
        PrepareMaterialInstances();

        if (materialInstances.Count == 0)
        {
            Debug.LogWarning("Dissolve animator found no renderer materials to animate.", this);
            if (destroyOnComplete && destroyTarget != null)
            {
                Destroy(destroyTarget);
            }

            return;
        }

        activeSequence?.Kill();

        if (resetMaterialValuesOnPlay)
        {
            SetEmissionStrength(0f);
            SetDissolveAmount(0f);
        }

        activeSequence = DOTween.Sequence();
        activeSequence.Append(ApplyEase(
            DOVirtual.Float(0f, targetEmissionStrength, emissionRampDuration, SetEmissionStrength),
            emissionCurve));
        activeSequence.Append(ApplyEase(
            DOVirtual.Float(0f, 1f, dissolveDuration, SetDissolveAmount),
            dissolveCurve));
        activeSequence.OnComplete(HandleDissolveCompleted);
    }

    public IEnumerator PlayAndWait()
    {
        Play();
        if (activeSequence == null)
        {
            yield break;
        }

        yield return activeSequence.WaitForCompletion();
    }

    public void Stop()
    {
        activeSequence?.Kill();
        activeSequence = null;
    }

    [ContextMenu("Reset Dissolve Values")]
    public void ResetDissolveValues()
    {
        PrepareMaterialInstances();
        SetEmissionStrength(0f);
        SetDissolveAmount(0f);
    }

    private void PrepareMaterialInstances()
    {
        if (materialInstancesPrepared)
        {
            return;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        }

        materialInstances.Clear();
        materialInstanceSet.Clear();

        for (int rendererIndex = 0; rendererIndex < targetRenderers.Length; rendererIndex++)
        {
            Renderer targetRenderer = targetRenderers[rendererIndex];
            if (targetRenderer == null)
            {
                continue;
            }

            Material[] rendererMaterials = targetRenderer.materials;
            for (int materialIndex = 0; materialIndex < rendererMaterials.Length; materialIndex++)
            {
                Material material = rendererMaterials[materialIndex];
                if (material == null || materialInstanceSet.Contains(material))
                {
                    continue;
                }

                if (!material.HasProperty(DissolveAmountId) && !material.HasProperty(EmissionStrengthId))
                {
                    continue;
                }

                materialInstances.Add(material);
                materialInstanceSet.Add(material);
            }
        }

        materialInstancesPrepared = true;
    }

    private void SetEmissionStrength(float value)
    {
        for (int i = 0; i < materialInstances.Count; i++)
        {
            Material material = materialInstances[i];
            if (material != null && material.HasProperty(EmissionStrengthId))
            {
                material.SetFloat(EmissionStrengthId, value);
            }
        }
    }

    private void SetDissolveAmount(float value)
    {
        for (int i = 0; i < materialInstances.Count; i++)
        {
            Material material = materialInstances[i];
            if (material != null && material.HasProperty(DissolveAmountId))
            {
                material.SetFloat(DissolveAmountId, value);
            }
        }
    }

    private void HandleDissolveCompleted()
    {
        completed = true;
        activeSequence = null;
        SetDissolveAmount(1f);

        if (destroyOnComplete && destroyTarget != null)
        {
            Destroy(destroyTarget);
        }
    }

    private static T ApplyEase<T>(T tween, AnimationCurve curve) where T : Tween
    {
        return curve != null ? tween.SetEase(curve) : tween.SetEase(Ease.Linear);
    }

    private void OnDisable()
    {
        if (completed)
        {
            return;
        }

        activeSequence?.Kill();
        activeSequence = null;
    }

    private void OnDestroy()
    {
        activeSequence?.Kill();
        activeSequence = null;

        for (int i = 0; i < materialInstances.Count; i++)
        {
            Material material = materialInstances[i];
            if (material != null)
            {
                Destroy(material);
            }
        }

        materialInstances.Clear();
        materialInstanceSet.Clear();
    }
}
