using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class UITextJitterAnimator : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Jitter")]
    [SerializeField, Min(0f)] private float positionJitter = 2.5f;
    [SerializeField, Min(0f)] private float rotationJitter = 2f;
    [SerializeField, Min(0f)] private float scaleJitter = 0.03f;
    [SerializeField, Min(0.01f)] private float refreshRate = 18f;
    [SerializeField, Range(0f, 1f)] private float characterChance = 0.65f;

    [Header("Flicker")]
    [SerializeField] private bool flickerAlpha = true;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.65f;
    [SerializeField, Range(0f, 1f)] private float flickerChance = 0.18f;

    private TMP_Text text;
    private TMP_MeshInfo[] cachedMeshInfo;
    private float nextRefreshTime;
    private bool isPlaying;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        CacheTextMesh();

        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        RestoreTextMesh();
    }

    private void LateUpdate()
    {
        if (!isPlaying || text == null)
        {
            return;
        }

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = time + 1f / refreshRate;
        ApplyJitter();
    }

    public void Play()
    {
        CacheTextMesh();
        isPlaying = true;
        nextRefreshTime = 0f;
    }

    public void Stop()
    {
        isPlaying = false;
        RestoreTextMesh();
    }

    private void CacheTextMesh()
    {
        if (text == null)
        {
            return;
        }

        text.ForceMeshUpdate();
        cachedMeshInfo = text.textInfo.CopyMeshInfoVertexData();
    }

    private void ApplyJitter()
    {
        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;

        if (cachedMeshInfo == null || cachedMeshInfo.Length != textInfo.meshInfo.Length)
        {
            cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;
            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 center = (sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) * 0.5f;
            Matrix4x4 transformMatrix = GetCharacterMatrix(center);
            bool shouldJitter = Random.value <= characterChance;

            for (int j = 0; j < 4; j++)
            {
                Vector3 vertex = sourceVertices[vertexIndex + j];
                destinationVertices[vertexIndex + j] = shouldJitter
                    ? transformMatrix.MultiplyPoint3x4(vertex - center) + center
                    : vertex;
            }

            if (flickerAlpha)
            {
                ApplyCharacterAlpha(textInfo, materialIndex, vertexIndex);
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    private Matrix4x4 GetCharacterMatrix(Vector3 center)
    {
        Vector3 offset = new Vector3(
            Random.Range(-positionJitter, positionJitter),
            Random.Range(-positionJitter, positionJitter),
            0f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, Random.Range(-rotationJitter, rotationJitter));
        float scale = 1f + Random.Range(-scaleJitter, scaleJitter);

        return Matrix4x4.TRS(offset, rotation, Vector3.one * scale);
    }

    private void ApplyCharacterAlpha(TMP_TextInfo textInfo, int materialIndex, int vertexIndex)
    {
        Color32[] sourceColors = cachedMeshInfo[materialIndex].colors32;
        Color32[] destinationColors = textInfo.meshInfo[materialIndex].colors32;
        byte alpha = Random.value <= flickerChance
            ? (byte)Mathf.RoundToInt(255f * minAlpha)
            : sourceColors[vertexIndex].a;

        for (int i = 0; i < 4; i++)
        {
            Color32 color = sourceColors[vertexIndex + i];
            color.a = alpha;
            destinationColors[vertexIndex + i] = color;
        }
    }

    private void RestoreTextMesh()
    {
        if (text == null || cachedMeshInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo = text.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length && i < cachedMeshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            textInfo.meshInfo[i].mesh.colors32 = cachedMeshInfo[i].colors32;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
