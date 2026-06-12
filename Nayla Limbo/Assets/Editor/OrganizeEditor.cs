using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Organize))]
public class OrganizeEditor : Editor
{
    SerializedProperty entriesProp;

    static readonly Color[] CategoryHeaderColors = new Color[]
    {
        new Color(0.30f, 0.65f, 1.00f), // Player
        new Color(1.00f, 0.60f, 0.20f), // Building
        new Color(0.30f, 0.90f, 0.50f), // Decoration
        new Color(0.70f, 0.70f, 0.70f), // Misc
        new Color(1.00f, 0.20f, 0.20f), // HorrorEvent
    };

    static readonly string[] CategoryLabels = { "Player", "Building", "Decoration", "Misc", "Horror Event" };

    void OnEnable()
    {
        entriesProp = serializedObject.FindProperty("entries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Title ─────────────────────────────────────────────
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Hierarchy Organizer", titleStyle);
        EditorGUILayout.Space(2);

        // ── Legend ────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        for (int i = 0; i < CategoryLabels.Length; i++)
        {
            DrawCategoryBadge(CategoryLabels[i], CategoryHeaderColors[i]);
            GUILayout.Space(4);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        DrawDivider();
        EditorGUILayout.Space(4);

        // ── Entry List ────────────────────────────────────────
        int removeIndex = -1;

        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            SerializedProperty entry       = entriesProp.GetArrayElementAtIndex(i);
            SerializedProperty targetProp  = entry.FindPropertyRelative("target");
            SerializedProperty catProp     = entry.FindPropertyRelative("category");
            SerializedProperty colorProp   = entry.FindPropertyRelative("color");

            Color entryColor   = colorProp.colorValue;
            Color bgColor      = entryColor;
            bgColor.a          = 0.15f;

            // ── Kotak per entry ───────────────────────────────
            Rect boxRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(boxRect, bgColor);

            EditorGUILayout.BeginHorizontal();

            Rect swatchRect = GUILayoutUtility.GetRect(12, 40, GUILayout.Width(12));
            Color swatchColor = entryColor;
            swatchColor.a = 1f;
            EditorGUI.DrawRect(swatchRect, swatchColor);

            GUILayout.Space(4);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(targetProp, GUIContent.none, GUILayout.Height(18));

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(catProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                colorProp.colorValue = Organize.GetCategoryColor(
                    (OrganizeCategory)catProp.enumValueIndex
                );
                EditorApplication.RepaintHierarchyWindow();
            }

            // Color picker
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorProp, GUIContent.none, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Tombol remove
            GUIStyle removeBtnStyle = new GUIStyle(GUI.skin.button)
            {
                normal    = { textColor = new Color(1f, 0.4f, 0.4f) },
                fontStyle = FontStyle.Bold,
                fontSize  = 14
            };
            if (GUILayout.Button("✕", removeBtnStyle, GUILayout.Width(26), GUILayout.Height(36)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(3);
        }

        if (removeIndex >= 0)
        {
            entriesProp.DeleteArrayElementAtIndex(removeIndex);
            EditorApplication.RepaintHierarchyWindow();
        }

        EditorGUILayout.Space(4);

        // ── Add Entry Button ──────────────────────────────────
        GUIStyle addBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 12,
            normal    = { textColor = new Color(0.4f, 1f, 0.6f) }
        };
        if (GUILayout.Button("+ Add Entry", addBtnStyle, GUILayout.Height(28)))
        {
            entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
            SerializedProperty newEntry = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
            newEntry.FindPropertyRelative("target").objectReferenceValue  = null;
            newEntry.FindPropertyRelative("category").enumValueIndex      = (int)OrganizeCategory.Misc;
            newEntry.FindPropertyRelative("color").colorValue             = Organize.GetCategoryColor(OrganizeCategory.Misc);
        }

        DrawDivider();

        GUIStyle infoStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField($"Total entries : {entriesProp.arraySize}", infoStyle);

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Copyright to Renzien  ↗", GUILayout.Height(26)))
            Application.OpenURL("https://github.com/renzien");

        serializedObject.ApplyModifiedProperties();
    }


    void DrawCategoryBadge(string label, Color color)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUIStyle badge = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize  = 9,
            fontStyle = FontStyle.Bold
        };
        GUILayout.Label(label, badge);
        GUI.backgroundColor = prev;
    }

    void DrawDivider()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        EditorGUILayout.Space(2);
    }
}