using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIToggleController : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField] private GameObject uiTarget;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("UI Buttons")]
    [SerializeField] private Button openOrToggleButton;
    [SerializeField] private Button closeButton;

    [Header("Keyboard")]
    [SerializeField] private bool enableKeyToggle = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
    [SerializeField] private bool allowEscapeToClose = true;

    [Header("Behavior")]
    [SerializeField] private bool startVisible;
    [SerializeField] private bool pauseGameWhenOpen;
    [SerializeField] private bool controlCursor = true;

    [SerializeField] private Behaviour blockOpeningWhenDisabled;

    [SerializeField] private Behaviour[] suspendWhileVisible =
        Array.Empty<Behaviour>();

    [Header("Animation")]
    [SerializeField] private bool smoothFade;
    [Min(0f)] [SerializeField] private float fadeDuration = 0.2f;

    [Header("Events")]
    public UnityEvent onShow = new UnityEvent();
    public UnityEvent onHide = new UnityEvent();

    private readonly List<Behaviour> suspendedControls =
        new List<Behaviour>();

    private Coroutine fade;
    private float previousTimeScale;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;
    private bool pausedByMenu;
    private bool cursorControlledByMenu;

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        if (uiTarget != null && canvasGroup == null)
            canvasGroup = uiTarget.GetComponent<CanvasGroup>();

        if (openOrToggleButton != null)
            openOrToggleButton.onClick.AddListener(ToggleUI);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideUI);
    }

    private void Start()
    {
        if (IsVisible)
            return;

        ApplyVisibility(false);

        if (startVisible)
            SetUIVisible(true, instant: true);
    }

    private void Update()
    {
        if (!enableKeyToggle)
            return;

        if (WasPressed(toggleKey))
            ToggleUI();
        else if (IsVisible && allowEscapeToClose && WasPressed(KeyCode.Escape))
            HideUI();
    }

    private void OnDisable()
    {
        bool wasVisible = IsVisible;

        StopFade();
        IsVisible = false;

        if (wasVisible)
            ReleaseGameplayState();

        if (uiTarget != null &&
            uiTarget.activeInHierarchy &&
            !transform.IsChildOf(uiTarget.transform))
        {
            ApplyVisibility(false);
        }

        if (wasVisible)
            onHide?.Invoke();
    }

    private void OnDestroy()
    {
        if (openOrToggleButton != null)
            openOrToggleButton.onClick.RemoveListener(ToggleUI);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HideUI);
    }

    public void ShowUI()
    {
        SetUIVisible(true);
    }

    public void HideUI()
    {
        SetUIVisible(false);
    }

    public void HideUIImmediately()
    {
        SetUIVisible(false, instant: true);
    }

    public void ToggleUI()
    {
        SetUIVisible(!IsVisible);
    }

    public void SetUIVisible(bool visible, bool instant = false)
    {
        if (!isActiveAndEnabled || visible == IsVisible)
            return;

        if (uiTarget == null || transform.IsChildOf(uiTarget.transform))
        {
            Debug.LogWarning(
                "[UIToggleController] Isi UI Target dan taruh controller di objek yang tetap aktif.",
                this
            );

            return;
        }

        if (visible && (
            (pauseGameWhenOpen && Time.timeScale <= 0f) ||
            (blockOpeningWhenDisabled != null &&
                !blockOpeningWhenDisabled.isActiveAndEnabled)
        ))
        {
            return;
        }

        StopFade();
        IsVisible = visible;

        if (visible)
            CaptureGameplayState();
        else
            ReleaseGameplayState();

        if (instant || !smoothFade || canvasGroup == null || fadeDuration <= 0f)
            ApplyVisibility(visible);
        else
            fade = StartCoroutine(FadeVisibility(visible));

        if (visible)
            onShow?.Invoke();
        else
            onHide?.Invoke();
    }

    private void CaptureGameplayState()
    {
        pausedByMenu = pauseGameWhenOpen;

        if (pausedByMenu)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        cursorControlledByMenu = controlCursor;

        if (cursorControlledByMenu)
        {
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (suspendWhileVisible == null)
            return;

        foreach (Behaviour control in suspendWhileVisible)
        {
            if (control == null || control == this || !control.enabled)
                continue;

            suspendedControls.Add(control);
            control.enabled = false;
        }
    }

    private void ReleaseGameplayState()
    {
        if (pausedByMenu && Time.timeScale == 0f)
            Time.timeScale = previousTimeScale;

        pausedByMenu = false;

        foreach (Behaviour control in suspendedControls)
        {
            if (control != null)
                control.enabled = true;
        }

        suspendedControls.Clear();

        if (!cursorControlledByMenu)
            return;

        cursorControlledByMenu = false;
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void StopFade()
    {
        if (fade == null)
            return;

        StopCoroutine(fade);
        fade = null;
    }

    private void ApplyVisibility(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (uiTarget != null && uiTarget.activeSelf != visible)
            uiTarget.SetActive(visible);
    }

    private IEnumerator FadeVisibility(bool visible)
    {
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = true;

        if (visible)
            uiTarget.SetActive(true);

        float initialAlpha = canvasGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                initialAlpha,
                targetAlpha,
                elapsed / fadeDuration
            );

            yield return null;
        }

        ApplyVisibility(visible);
        fade = null;
    }

    private static bool WasPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        Key key = MapKeyCode(keyCode);

        if (Keyboard.current != null &&
            key != Key.None &&
            Keyboard.current[key].wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(keyCode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static Key MapKeyCode(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.Escape => Key.Escape,
            KeyCode.Tab => Key.Tab,
            KeyCode.Space => Key.Space,
            KeyCode.Return => Key.Enter,
            KeyCode.I => Key.I,
            KeyCode.M => Key.M,
            KeyCode.E => Key.E,
            KeyCode.P => Key.P,
            KeyCode.C => Key.C,
            KeyCode.U => Key.U,
            KeyCode.Alpha1 => Key.Digit1,
            KeyCode.Alpha2 => Key.Digit2,
            KeyCode.Alpha3 => Key.Digit3,
            KeyCode.Alpha4 => Key.Digit4,
            KeyCode.F1 => Key.F1,
            KeyCode.F2 => Key.F2,
            KeyCode.F3 => Key.F3,
            _ => Key.None
        };
    }
#endif
}