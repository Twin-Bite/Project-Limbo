using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ZoneEvent))]
public class ZoneEventEditor : Editor
{
    SerializedProperty isInRange;
    SerializedProperty interactButton;
    SerializedProperty isOneShot;
    SerializedProperty cooldown;
    SerializedProperty interactSound;
    SerializedProperty soundVolume;
    SerializedProperty spatialBlend;
    SerializedProperty interactionPrompt;
    SerializedProperty zoneEvent;
    SerializedProperty enableDebugLog;

    void OnEnable()
    {
        isInRange         = serializedObject.FindProperty("isInRange");
        interactButton    = serializedObject.FindProperty("interactButton");
        isOneShot         = serializedObject.FindProperty("isOneShot");
        cooldown          = serializedObject.FindProperty("cooldown");
        interactSound     = serializedObject.FindProperty("interactSound");
        soundVolume       = serializedObject.FindProperty("soundVolume");
        spatialBlend      = serializedObject.FindProperty("spatialBlend");
        interactionPrompt = serializedObject.FindProperty("interactionPrompt");
        zoneEvent         = serializedObject.FindProperty("zoneEvent");
        enableDebugLog    = serializedObject.FindProperty("enableDebugLog");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (Application.isPlaying)
        {
            bool inRange = isInRange.boolValue;
            Color statusColor = inRange
                ? new Color(0.2f, 0.85f, 0.2f)
                : new Color(0.85f, 0.3f, 0.3f);

            GUIStyle statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize  = 12,
                normal    = { textColor = statusColor }
            };

            string label = inRange ? "● Player IN Range" : "○ Player OUT of Range";
            EditorGUILayout.LabelField(label, statusStyle, GUILayout.Height(26));
            EditorGUILayout.Space(8);
        }

        DrawSectionHeader("Interaction Settings");

        GUI.enabled = false;
        EditorGUILayout.PropertyField(isInRange, new GUIContent("Is In Range"));
        GUI.enabled = true;

        EditorGUILayout.PropertyField(interactButton, new GUIContent("Interact Button"));
        EditorGUILayout.PropertyField(isOneShot,      new GUIContent("Is One Shot?"));
        EditorGUILayout.Slider(cooldown, 0f, 3f,      new GUIContent("Cooldown (detik)"));

        EditorGUILayout.Space(6);

        DrawSectionHeader("Interaction Sound");

        EditorGUILayout.PropertyField(interactSound, new GUIContent("Interact Sound"));

        if (interactSound.objectReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Slider(soundVolume,  0f, 1f, new GUIContent("Volume"));
            EditorGUILayout.Slider(spatialBlend, 0f, 1f, new GUIContent("Spatial Blend",
                "0 = 2D (sama di mana pun)\n1 = 3D (mengecil kalau jauh dari objek)"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6);

        DrawSectionHeader("Interaction Prompt (Opsional)");
        EditorGUILayout.PropertyField(interactionPrompt, new GUIContent("Prompt Object"));

        EditorGUILayout.Space(6);

        DrawSectionHeader("Zone Event");
        EditorGUILayout.PropertyField(zoneEvent, new GUIContent("Zone Event ()"));

        EditorGUILayout.Space(6);

        DrawSectionHeader("Debug");
        EditorGUILayout.PropertyField(enableDebugLog, new GUIContent("Enable Debug Log"));

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(8);
            DrawSectionHeader("Quick Actions");

            ZoneEvent t = (ZoneEvent)target;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Zone",       GUILayout.Height(26))) t.ResetZone();
            if (GUILayout.Button("Force Deactivate", GUILayout.Height(26))) t.ForceDeactivate();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(16);

        GUIStyle noteStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 11 };

        EditorGUILayout.LabelField("Note To Users :", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Fungsi yang bisa dipanggil dari luar via UnityEvent :", noteStyle);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("  A.  ResetZone", noteStyle);
        EditorGUILayout.LabelField("        → Reset zona ke kondisi awal.", noteStyle);
        EditorGUILayout.Space(1);
        EditorGUILayout.LabelField("  B.  ForceDeactivate", noteStyle);
        EditorGUILayout.LabelField("        → Paksa nonaktifkan zona dari luar.", noteStyle);

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Copyright to Renzien  ↗", GUILayout.Height(28)))
            Application.OpenURL("https://github.com/renzien");

        serializedObject.ApplyModifiedProperties();

        if (Application.isPlaying) Repaint();
    }

    void DrawSectionHeader(string title)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
        EditorGUILayout.LabelField(title, style);
    }
}