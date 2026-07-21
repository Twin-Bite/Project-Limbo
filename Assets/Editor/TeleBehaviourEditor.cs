using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector untuk TeleBehaviour.
/// File ini HARUS diletakkan di dalam folder bernama "Editor".
/// Contoh: Assets/Editor/TeleBehaviourEditor.cs
/// </summary>
[CustomEditor(typeof(TeleBehaviour))]
public class TeleBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Gambar field-field default TeleBehaviour seperti biasa
        DrawDefaultInspector();

        EditorGUILayout.Space(16);

        // ── Note To Users ──────────────────────────────────
        GUIStyle noteTitle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12
        };

        GUIStyle noteBody = new GUIStyle(EditorStyles.label)
        {
            wordWrap  = true,
            fontSize  = 11,
            richText  = true
        };

        EditorGUILayout.LabelField("Note To Users :", noteTitle);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(
            "Fungsi-fungsi yang bisa dipanggil via UnityEvent (contoh: ZoneEvent) :",
            noteBody
        );

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("  A.  BeginTeleport", noteBody);
        EditorGUILayout.LabelField("        → Teleport target ke Target Location.", noteBody);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("  B.  BeginTeleportInitialPosition", noteBody);
        EditorGUILayout.LabelField("        → Teleport target kembali ke posisi awal.", noteBody);

        EditorGUILayout.Space(16);

        // ── Copyright ──────────────────────────────────────
        GUIStyle copyrightBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Italic
        };

        if (GUILayout.Button("Copyright to Renzien  ↗", copyrightBtn, GUILayout.Height(28)))
        {
            Application.OpenURL("https://github.com/renzien");
        }
    }
}