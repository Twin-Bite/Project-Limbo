using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls showing, hiding, and toggling a UI panel/canvas.
/// Works with:
/// 1. A physical keyboard/gamepad button press (e.g., Escape, Tab, I, M).
/// 2. A UI Button click (via direct inspector slot or Button onClick event).
/// </summary>
public class UIToggleController : MonoBehaviour
{
    [Header("Target UI")]
    [Tooltip("The UI GameObject (panel, window, or Canvas) to show and hide.")]
    [SerializeField] private GameObject uiTarget;

    [Tooltip("Optional CanvasGroup on the UI target for smooth fading and raycast blocking.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("UI Buttons (Optional)")]
    [Tooltip("Optional UI Button that toggles or opens this UI when clicked.")]
    [SerializeField] private Button openOrToggleButton;

    [Tooltip("Optional UI Button (e.g. an 'X' or 'Back' button) that closes this UI when clicked.")]
    [SerializeField] private Button closeButton;

    [Header("Keyboard / Controller Trigger")]
    [Tooltip("Enable or disable keyboard key toggle.")]
    [SerializeField] private bool enableKeyToggle = true;

    [Tooltip("The keyboard key used to toggle the UI (e.g. Escape, Tab, I, M).")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Tooltip("If true, pressing Escape will close the UI even if toggleKey is set to another key.")]
    [SerializeField] private bool allowEscapeToClose = true;

    [Header("Behavior Settings")]
    [Tooltip("Whether the UI should start visible or hidden when the scene loads.")]
    [SerializeField] private bool startVisible = false;

    [Tooltip("Pause the game (Time.timeScale = 0) while the UI is open.")]
    [SerializeField] private bool pauseGameWhenOpen = false;

    [Tooltip("Unlock and show the mouse cursor while the UI is open (helpful for first-person/horror games).")]
    [SerializeField] private bool controlCursor = true;

    [Header("Animation Settings")]
    [Tooltip("Fade in/out smoothly using the CanvasGroup (if assigned).")]
    [SerializeField] private bool smoothFade = false;

    [Tooltip("Fade duration in seconds (unscaled time).")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Events")]
    public UnityEvent onShow;
    public UnityEvent onHide;

    private bool _isVisible;
    private Coroutine _fadeCoroutine;
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    public bool IsVisible => _isVisible;

    private void Awake()
    {
        // Auto-detect CanvasGroup if target is assigned but CanvasGroup slot is empty
        if (uiTarget != null && canvasGroup == null)
        {
            canvasGroup = uiTarget.GetComponent<CanvasGroup>();
        }

        // Auto-wire UI Buttons if assigned in inspector
        if (openOrToggleButton != null)
        {
            openOrToggleButton.onClick.AddListener(ToggleUI);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideUI);
        }
    }

    private void Start()
    {
        // Apply initial state
        SetUIVisible(startVisible, instant: true);
    }

    private void Update()
    {
        if (!enableKeyToggle) return;

        if (IsToggleKeyPressed())
        {
            ToggleUI();
        }
        else if (allowEscapeToClose && _isVisible && toggleKey != KeyCode.Escape && IsEscapePressed())
        {
            HideUI();
        }
    }

    private void OnDestroy()
    {
        if (openOrToggleButton != null)
        {
            openOrToggleButton.onClick.RemoveListener(ToggleUI);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideUI);
        }
    }

    /// <summary>
    /// Shows the target UI. Can be hooked directly into UI Button onClick events.
    /// </summary>
    public void ShowUI()
    {
        SetUIVisible(true);
    }

    /// <summary>
    /// Hides the target UI. Can be hooked directly into UI Button onClick events.
    /// </summary>
    public void HideUI()
    {
        SetUIVisible(false);
    }

    /// <summary>
    /// Toggles the target UI between visible and hidden.
    /// </summary>
    public void ToggleUI()
    {
        SetUIVisible(!_isVisible);
    }

    /// <summary>
    /// Sets the UI visibility state.
    /// </summary>
    public void SetUIVisible(bool visible, bool instant = false)
    {
        _isVisible = visible;

        if (uiTarget == null)
        {
            Debug.LogWarning("[UIToggleController] UI Target is not assigned in the inspector!", this);
            return;
        }

        // Handle game pause
        if (pauseGameWhenOpen)
        {
            Time.timeScale = _isVisible ? 0f : 1f;
            Debug.Log("Paused");
        }

        // Handle cursor lock/unlock
        if (controlCursor)
        {
            if (_isVisible)
            {
                _previousLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = _previousLockMode;
                Cursor.visible = _previousCursorVisible;
            }
        }

        // Apply visual display
        if (smoothFade && canvasGroup != null && Application.isPlaying && !instant)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeRoutine(_isVisible));
        }
        else
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = _isVisible ? 1f : 0f;
                canvasGroup.interactable = _isVisible;
                canvasGroup.blocksRaycasts = _isVisible;
            }

            uiTarget.SetActive(_isVisible);
        }

        // Trigger events
        if (_isVisible)
        {
            onShow?.Invoke();
        }
        else
        {
            onHide?.Invoke();
        }
    }

    private IEnumerator FadeRoutine(bool targetVisible)
    {
        if (targetVisible)
        {
            uiTarget.SetActive(true);
        }

        float startAlpha = canvasGroup.alpha;
        float endAlpha = targetVisible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        canvasGroup.interactable = targetVisible;
        canvasGroup.blocksRaycasts = targetVisible;

        if (!targetVisible)
        {
            uiTarget.SetActive(false);
        }

        _fadeCoroutine = null;
    }

    /// <summary>
    /// Checks if the configured toggle key was pressed across both old and new input systems.
    /// </summary>
    private bool IsToggleKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Key key = MapKeyCodeToKey(toggleKey);
            if (key != Key.None && Keyboard.current[key].wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(toggleKey))
            {
                return true;
            }
        }
        catch (System.InvalidOperationException)
        {
            // Thrown if project is strictly set to New Input System without legacy support
        }
#endif

        return false;
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                return true;
        }
        catch (System.InvalidOperationException) { }
#endif

        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private Key MapKeyCodeToKey(KeyCode keyCode)
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
