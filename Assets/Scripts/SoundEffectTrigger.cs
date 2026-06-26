using UnityEngine;

[DisallowMultipleComponent]
public sealed class SoundEffectTrigger : MonoBehaviour
{
    [SerializeField] private string soundId;
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
    [SerializeField] private bool playAtTransformPosition;
    [SerializeField] private bool spatialClip;

    public void Play()
    {
        SoundEffectManager manager = SoundEffectManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("Cannot play sound effect because no SoundEffectManager exists in the scene.", this);
            return;
        }

        if (clip != null)
        {
            if (playAtTransformPosition)
            {
                manager.PlayClipAtPosition(clip, transform.position, volumeScale);
            }
            else
            {
                if (spatialClip)
                {
                    manager.PlayClipAtPosition(clip, transform.position, volumeScale);
                }
                else
                {
                    manager.PlayClip(clip, volumeScale);
                }
            }

            return;
        }

        if (playAtTransformPosition)
        {
            manager.PlayAtPosition(soundId, transform.position, volumeScale);
        }
        else
        {
            manager.Play(soundId, volumeScale);
        }
    }

    public void Play(string overrideSoundId)
    {
        SoundEffectManager manager = SoundEffectManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("Cannot play sound effect because no SoundEffectManager exists in the scene.", this);
            return;
        }

        if (playAtTransformPosition)
        {
            manager.PlayAtPosition(overrideSoundId, transform.position, volumeScale);
        }
        else
        {
            manager.Play(overrideSoundId, volumeScale);
        }
    }
}
