using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(RoomCombinationLock))]
public class RoomLockInspector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoomCombinationLock puzzle;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Camera inspectionCamera;
    [SerializeField] private Transform inspectionAnchor;

    [Tooltip("Isi dengan Movement dan script kontrol lain milik Player.")]
    [SerializeField] private Behaviour[] disableDuringInspection = new Behaviour[0];

    [Header("View")]
    [SerializeField] private Vector3 inspectionEulerAngles = new Vector3(0f, 180f, 0f);
    [SerializeField] private float rotateSensitivity = 0.2f;
    [SerializeField] private float zoomSensitivity = 0.03f;
    [Range(15f, 65f)] [SerializeField] private float initialFieldOfView = 35f;
    [SerializeField] private bool closeWhenSolved = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onInspectionStarted = new UnityEvent();
    [SerializeField] private UnityEvent onInspectionEnded = new UnityEvent();

    private static RoomLockInspector activeInspector;
    private RoomPlayerInteractor playerInteractor;
    private Behaviour[] suspendedControls;
    private bool[] controlStates;
    private Transform previousParent;
    private Vector3 previousPosition, previousScale;
    private Quaternion previousRotation;
    private bool previousGameCameraEnabled, previousInspectionCameraEnabled;
    private bool previousInteractorEnabled, previousCursorVisible;
    private float previousFieldOfView;
    private CursorLockMode previousCursorLock;
    public bool IsInspecting { get; private set; }
    public bool CanInspect => isActiveAndEnabled && puzzle != null &&
        puzzle.isActiveAndEnabled && puzzle.IsReady;

    private void Awake()
    {
        if (puzzle == null) puzzle = GetComponent<RoomCombinationLock>();
        if (inspectionCamera != null && activeInspector == null &&
            inspectionCamera != gameCamera && inspectionCamera != Camera.main)
            inspectionCamera.enabled = false;
    }

    private void OnEnable()
    {
        if (puzzle != null) puzzle.OnUnlocked.AddListener(HandleUnlocked);
    }

    private void OnDisable()
    {
        if (puzzle != null) puzzle.OnUnlocked.RemoveListener(HandleUnlocked);
        CloseInspect();
    }

    public void Interact(RoomPlayerInteractor source)
    {
        if (!CanInspect || IsInspecting || activeInspector != null) return;
        if (gameCamera == null) gameCamera = Camera.main;
        if (source == null || gameCamera == null || inspectionCamera == null ||
            inspectionAnchor == null || inspectionCamera == gameCamera ||
            inspectionAnchor.IsChildOf(transform) || inspectionCamera.orthographic ||
            !inspectionCamera.gameObject.activeInHierarchy ||
            !inspectionAnchor.gameObject.activeInHierarchy)
        {
            Debug.LogError("[RoomLockInspector] Cek kamera Perspective, Anchor, dan Player.", this);
            return;
        }

        previousParent = transform.parent;
        previousPosition = transform.localPosition;
        previousRotation = transform.localRotation;
        previousScale = transform.localScale;
        Vector3 worldScale = transform.lossyScale;
        previousGameCameraEnabled = gameCamera.enabled;
        previousInspectionCameraEnabled = inspectionCamera.enabled;
        previousFieldOfView = inspectionCamera.fieldOfView;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        playerInteractor = source;
        previousInteractorEnabled = source.enabled;
        suspendedControls = disableDuringInspection == null
            ? new Behaviour[0] : (Behaviour[])disableDuringInspection.Clone();
        controlStates = new bool[suspendedControls.Length];
        for (int i = 0; i < suspendedControls.Length; i++)
            controlStates[i] = suspendedControls[i] != null && suspendedControls[i].enabled;

        IsInspecting = true;
        activeInspector = this;
        source.enabled = false;
        for (int i = 0; i < suspendedControls.Length; i++)
            if (CanSuspend(suspendedControls[i])) suspendedControls[i].enabled = false;

        transform.SetParent(inspectionAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(inspectionEulerAngles);
        transform.localScale = worldScale;
        CenterModel();
        inspectionCamera.fieldOfView = initialFieldOfView;
        gameCamera.enabled = false;
        inspectionCamera.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Physics.SyncTransforms();
        onInspectionStarted.Invoke();
    }

    private bool CanSuspend(Behaviour control)
    {
        return control != null && control != this && control != puzzle &&
            control != playerInteractor && control != gameCamera && control != inspectionCamera;
    }

    private void CenterModel()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        transform.position += inspectionAnchor.position - bounds.center;
    }

    private void Update()
    {
        if (!IsInspecting) return;
        if (inspectionCamera == null || inspectionAnchor == null)
        {
            CloseInspect();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseInspect();
            return;
        }
        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            puzzle.CheckCode();
        if (!IsInspecting) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue() * rotateSensitivity;
            transform.RotateAround(inspectionAnchor.position, inspectionCamera.transform.up, -delta.x);
            transform.RotateAround(inspectionAnchor.position, inspectionCamera.transform.right, delta.y);
        }

        inspectionCamera.fieldOfView = Mathf.Clamp(
            inspectionCamera.fieldOfView - mouse.scroll.ReadValue().y * zoomSensitivity, 15f, 65f);

        if (mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.isPressed)
        {
            bool reverse = keyboard != null &&
                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            ClickWheel(mouse.position.ReadValue(), reverse);
        }
    }

    private void ClickWheel(Vector2 screenPosition, bool reverse)
    {
        Physics.SyncTransforms();
        Ray ray = inspectionCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray, inspectionCamera.farClipPlane, ~0, QueryTriggerInteraction.Collide);
        int nearestIndex = -1;
        float nearestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
        {
            int index = puzzle.FindWheelIndex(hit.collider.transform);
            if (index < 0 || hit.distance >= nearestDistance) continue;
            nearestIndex = index;
            nearestDistance = hit.distance;
        }
        if (nearestIndex < 0) return;
        if (reverse) puzzle.RotatePrevious(nearestIndex);
        else puzzle.RotateNext(nearestIndex);
    }

    private void HandleUnlocked()
    {
        if (closeWhenSolved) CloseInspect();
    }

    public void CloseInspect()
    {
        if (!IsInspecting) return;
        IsInspecting = false;
        if (activeInspector == this) activeInspector = null;
        transform.SetParent(previousParent, false);
        transform.localPosition = previousPosition;
        transform.localRotation = previousRotation;
        transform.localScale = previousScale;

        if (inspectionCamera != null)
        {
            inspectionCamera.fieldOfView = previousFieldOfView;
            inspectionCamera.enabled = previousInspectionCameraEnabled;
        }
        if (gameCamera != null) gameCamera.enabled = previousGameCameraEnabled;
        if (suspendedControls != null)
            for (int i = 0; i < suspendedControls.Length; i++)
                if (CanSuspend(suspendedControls[i])) suspendedControls[i].enabled = controlStates[i];
        if (playerInteractor != null) playerInteractor.enabled = previousInteractorEnabled;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        Physics.SyncTransforms();
        onInspectionEnded.Invoke();
    }
}
