using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundEffectManager))]
public sealed class SoundEffectManagerEditor : Editor
{
    private SerializedProperty sourcePrefab;
    private SerializedProperty initialPoolSize;
    private SerializedProperty masterVolume;
    private SerializedProperty persistAcrossScenes;
    private SerializedProperty sounds;
    private SerializedProperty gameEventBindings;

    private readonly Dictionary<string, string> clipNamesBySoundId = new Dictionary<string, string>();
    private readonly List<string> soundIds = new List<string>();
    private readonly List<string> soundPopupOptions = new List<string>();

    private void OnEnable()
    {
        sourcePrefab = serializedObject.FindProperty("sourcePrefab");
        initialPoolSize = serializedObject.FindProperty("initialPoolSize");
        masterVolume = serializedObject.FindProperty("masterVolume");
        persistAcrossScenes = serializedObject.FindProperty("persistAcrossScenes");
        sounds = serializedObject.FindProperty("sounds");
        gameEventBindings = serializedObject.FindProperty("gameEventBindings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        RebuildClipLookup();

        EditorGUILayout.PropertyField(sourcePrefab);
        EditorGUILayout.PropertyField(initialPoolSize);
        EditorGUILayout.PropertyField(masterVolume);
        EditorGUILayout.PropertyField(persistAcrossScenes);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(sounds, true);

        EditorGUILayout.Space();
        DrawGameEventBindings();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGameEventBindings()
    {
        gameEventBindings.isExpanded = EditorGUILayout.Foldout(
            gameEventBindings.isExpanded,
            $"Game Event Bindings ({gameEventBindings.arraySize})",
            true);
        if (!gameEventBindings.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        gameEventBindings.arraySize = Mathf.Max(
            0,
            EditorGUILayout.IntField("Size", gameEventBindings.arraySize));

        for (int i = 0; i < gameEventBindings.arraySize; i++)
        {
            SerializedProperty binding = gameEventBindings.GetArrayElementAtIndex(i);
            SerializedProperty gameEvent = binding.FindPropertyRelative("gameEvent");
            SerializedProperty soundId = binding.FindPropertyRelative("soundId");
            SerializedProperty volumeScale = binding.FindPropertyRelative("volumeScale");

            string clipSummary = GetClipSummary(soundId.stringValue);
            string gameEventName = gameEvent.enumValueIndex >= 0 && gameEvent.enumValueIndex < gameEvent.enumDisplayNames.Length
                ? gameEvent.enumDisplayNames[gameEvent.enumValueIndex]
                : $"Unknown Event ({gameEvent.intValue})";
            string title = $"{gameEventName} -> {GetSoundTitle(soundId.stringValue, clipSummary)}";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            binding.isExpanded = EditorGUILayout.Foldout(binding.isExpanded, title, true);
            if (GUILayout.Button("-", GUILayout.Width(24f)))
            {
                gameEventBindings.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (binding.isExpanded)
            {
                EditorGUILayout.PropertyField(gameEvent);
                DrawSoundIdPopup(soundId);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Audio Clip", clipSummary);
                }

                EditorGUILayout.PropertyField(volumeScale);
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Binding"))
        {
            gameEventBindings.InsertArrayElementAtIndex(gameEventBindings.arraySize);
            SerializedProperty addedBinding = gameEventBindings.GetArrayElementAtIndex(gameEventBindings.arraySize - 1);
            addedBinding.isExpanded = true;
        }

        EditorGUI.indentLevel--;
    }

    private void RebuildClipLookup()
    {
        clipNamesBySoundId.Clear();
        soundIds.Clear();
        soundPopupOptions.Clear();
        for (int i = 0; i < sounds.arraySize; i++)
        {
            SerializedProperty sound = sounds.GetArrayElementAtIndex(i);
            SerializedProperty id = sound.FindPropertyRelative("id");
            SerializedProperty clips = sound.FindPropertyRelative("clips");
            if (string.IsNullOrWhiteSpace(id.stringValue))
            {
                continue;
            }

            string soundId = id.stringValue;
            string clipSummary = BuildClipSummary(clips);
            clipNamesBySoundId[soundId] = clipSummary;
            soundIds.Add(soundId);
            soundPopupOptions.Add($"{soundId} - {clipSummary}");
        }
    }

    private void DrawSoundIdPopup(SerializedProperty soundId)
    {
        if (soundIds.Count == 0)
        {
            EditorGUILayout.PropertyField(soundId);
            EditorGUILayout.HelpBox("Add sounds to the Sound Library to select sound ids from a named list.", MessageType.Info);
            return;
        }

        int selectedIndex = soundIds.IndexOf(soundId.stringValue);
        string[] options;
        if (selectedIndex >= 0)
        {
            options = soundPopupOptions.ToArray();
        }
        else
        {
            selectedIndex = 0;
            options = new string[soundPopupOptions.Count + 1];
            string currentId = string.IsNullOrWhiteSpace(soundId.stringValue) ? "(No sound id)" : soundId.stringValue;
            options[0] = $"{currentId} - (Unknown sound id)";
            for (int i = 0; i < soundPopupOptions.Count; i++)
            {
                options[i + 1] = soundPopupOptions[i];
            }
        }

        int newIndex = EditorGUILayout.Popup("Sound Id", selectedIndex, options);
        if (soundIds.IndexOf(soundId.stringValue) >= 0)
        {
            soundId.stringValue = soundIds[newIndex];
            return;
        }

        if (newIndex > 0)
        {
            soundId.stringValue = soundIds[newIndex - 1];
        }
    }

    private static string BuildClipSummary(SerializedProperty clips)
    {
        if (clips == null || clips.arraySize == 0)
        {
            return "(No clips)";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < clips.arraySize; i++)
        {
            SerializedProperty clipProperty = clips.GetArrayElementAtIndex(i);
            Object clip = clipProperty.objectReferenceValue;
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(clip != null ? clip.name : "(Missing)");
        }

        return builder.ToString();
    }

    private string GetClipSummary(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            return "(No sound id)";
        }

        return clipNamesBySoundId.TryGetValue(soundId, out string clipSummary)
            ? clipSummary
            : "(Unknown sound id)";
    }

    private static string GetSoundTitle(string soundId, string clipSummary)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            return "(No sound id)";
        }

        return $"{soundId} ({clipSummary})";
    }
}
