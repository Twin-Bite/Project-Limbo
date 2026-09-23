using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector untuk FadeBehaviour.
/// File ini WAJIB ada di dalam folder bernama "Editor".
/// Contoh path: Assets/Editor/FadeBehaviourEditor.cs
/// </summary>
[CustomEditor(typeof(FadeBehaviour))]
public class FadeBehaviourEditor : Editor
{
    // ── SerializedProperties ─────────────────────────────────
    SerializedProperty selectType;
    SerializedProperty fadingPanel;
    SerializedProperty modifyOtherCanvasGroup;
    SerializedProperty inOut;
    SerializedProperty onStart;
    SerializedProperty startFadeIn;
    SerializedProperty startFadeOut;
    SerializedProperty fadingSpeed;
    SerializedProperty delay;
    SerializedProperty endDelay;
    SerializedProperty selectEase;
    SerializedProperty fadingPanelPosition;
    SerializedProperty gradientMode;
    SerializedProperty eyeController;
    SerializedProperty clockPanel;
    SerializedProperty onBeginFadingIn;
    SerializedProperty onCompleteFadingIn;
    SerializedProperty onBeginFadingOut;
    SerializedProperty onCompleteFadingOut;

    // Foldout states
    bool fadeInFoldout  = true;
    bool fadeOutFoldout = true;

    void OnEnable()
    {
        selectType              = serializedObject.FindProperty("selectType");
        fadingPanel             = serializedObject.FindProperty("fadingPanel");
        modifyOtherCanvasGroup  = serializedObject.FindProperty("modifyOtherCanvasGroup");
        inOut                   = serializedObject.FindProperty("inOut");
        onStart                 = serializedObject.FindProperty("onStart");
        startFadeIn             = serializedObject.FindProperty("startFadeIn");
        startFadeOut            = serializedObject.FindProperty("startFadeOut");
        fadingSpeed             = serializedObject.FindProperty("fadingSpeed");
        delay                   = serializedObject.FindProperty("delay");
        endDelay                = serializedObject.FindProperty("endDelay");
        selectEase              = serializedObject.FindProperty("selectEase");
        fadingPanelPosition     = serializedObject.FindProperty("fadingPanelPosition");
        gradientMode            = serializedObject.FindProperty("gradientMode");
        eyeController           = serializedObject.FindProperty("eyeController");
        clockPanel              = serializedObject.FindProperty("clockPanel");
        onBeginFadingIn         = serializedObject.FindProperty("onBeginFadingIn");
        onCompleteFadingIn      = serializedObject.FindProperty("onCompleteFadingIn");
        onBeginFadingOut        = serializedObject.FindProperty("onBeginFadingOut");
        onCompleteFadingOut     = serializedObject.FindProperty("onCompleteFadingOut");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        FadeBehaviour.SelectType currentType = (FadeBehaviour.SelectType)selectType.enumValueIndex;

        // ── Select Type ──────────────────────────────────────
        EditorGUILayout.PropertyField(selectType, new GUIContent("Select Type"));

        EditorGUILayout.Space(4);

        // ── Type Header Label ────────────────────────────────
        GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize  = 12,
        };
        EditorGUILayout.LabelField(GetTypeLabel(currentType), headerStyle, GUILayout.Height(26));

        EditorGUILayout.Space(6);

        // ── Fields Berdasarkan Type ──────────────────────────
        switch (currentType)
        {
            case FadeBehaviour.SelectType.Normal:
                EditorGUILayout.PropertyField(fadingPanel, new GUIContent("Fading Panel"));
                EditorGUILayout.PropertyField(modifyOtherCanvasGroup, new GUIContent("Modify Other Canvas Group"));
                EditorGUILayout.PropertyField(inOut, new GUIContent("In Out?"));
                break;

            case FadeBehaviour.SelectType.Gradient:
                EditorGUILayout.PropertyField(fadingPanel, new GUIContent("Fading Panel"));
                EditorGUILayout.PropertyField(modifyOtherCanvasGroup, new GUIContent("Modify Other Canvas Group"));
                EditorGUILayout.PropertyField(inOut, new GUIContent("In Out?"));
                EditorGUILayout.PropertyField(gradientMode, new GUIContent("Gradient Mode"));
                break;

            case FadeBehaviour.SelectType.Eye:
                EditorGUILayout.PropertyField(eyeController, new GUIContent("Eye Controller"));
                EditorGUILayout.PropertyField(inOut, new GUIContent("In Out?"));
                break;

            case FadeBehaviour.SelectType.Clock:
                EditorGUILayout.PropertyField(clockPanel, new GUIContent("Clock Panel"));
                EditorGUILayout.PropertyField(inOut, new GUIContent("In Out?"));
                break;
        }

        EditorGUILayout.Space(4);

        // ── On Start? ────────────────────────────────────────
        EditorGUILayout.PropertyField(onStart, new GUIContent("On Start?"));
        if (onStart.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(startFadeIn,  new GUIContent("Fading In"));
            EditorGUILayout.PropertyField(startFadeOut, new GUIContent("Fading Out"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ── Timing & Ease ────────────────────────────────────
        EditorGUILayout.Slider(fadingSpeed, 0f, 5f, new GUIContent("Fading Speed"));
        EditorGUILayout.Slider(delay,       0f, 5f, new GUIContent("Delay"));
        EditorGUILayout.Slider(endDelay,    0f, 5f, new GUIContent("End Delay"));
        EditorGUILayout.PropertyField(selectEase, new GUIContent("Select Ease"));

        if (currentType == FadeBehaviour.SelectType.Normal ||
            currentType == FadeBehaviour.SelectType.Gradient)
        {
            EditorGUILayout.PropertyField(fadingPanelPosition, new GUIContent("Fading Panel Position"));
        }

        EditorGUILayout.Space(8);

        // ── Fading In Event (Foldout) ────────────────────────
        fadeInFoldout = EditorGUILayout.Foldout(fadeInFoldout, "Fading In Event", true, EditorStyles.foldoutHeader);
        if (fadeInFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(onBeginFadingIn,    new GUIContent("On Begin Fading In ()"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(onCompleteFadingIn, new GUIContent("On Complete Fading In ()"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ── Fading Out Event (Foldout) ───────────────────────
        fadeOutFoldout = EditorGUILayout.Foldout(fadeOutFoldout, "Fading Out Event", true, EditorStyles.foldoutHeader);
        if (fadeOutFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(onBeginFadingOut,    new GUIContent("On Begin Fading Out ()"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(onCompleteFadingOut, new GUIContent("On Complete Fading Out ()"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(16);

        // ── Note To Users ────────────────────────────────────
        GUIStyle noteLabel = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 11 };

        EditorGUILayout.LabelField("Note To Users :", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Fungsi yang bisa dipanggil via UnityEvent :", noteLabel);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("  A.  BeginFadingIn",  noteLabel);
        EditorGUILayout.LabelField("        → Menjalankan efek Fade In.", noteLabel);
        EditorGUILayout.Space(1);
        EditorGUILayout.LabelField("  B.  BeginFadingOut", noteLabel);
        EditorGUILayout.LabelField("        → Menjalankan efek Fade Out.", noteLabel);

        EditorGUILayout.Space(12);

        // ── Copyright ────────────────────────────────────────
        GUIStyle copyrightStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Italic
        };
        if (GUILayout.Button("Copyright to Renzien  ↗", copyrightStyle, GUILayout.Height(28)))
            Application.OpenURL("https://github.com/renzien");

        serializedObject.ApplyModifiedProperties();
    }

    string GetTypeLabel(FadeBehaviour.SelectType type)
    {
        return type switch
        {
            FadeBehaviour.SelectType.Normal   => "Normal Fading",
            FadeBehaviour.SelectType.Gradient => "Gradient Fading",
            FadeBehaviour.SelectType.Eye      => "Eye Fading",
            FadeBehaviour.SelectType.Clock    => "Clock Fading",
            _                                 => "—"
        };
    }
}