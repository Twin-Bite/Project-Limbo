using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(RoomCombinationLock))]
public class RoomLockInspector : MonoBehaviour
{
    private enum ViewState { Closed, Opening, Ready, Closing }

    [Header("References")]
    [SerializeField] private RoomCombinationLock puzzle;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Camera inspectionCamera;
    [SerializeField] private Transform inspectionAnchor;
    [SerializeField] private Light inspectionLight;
    [SerializeField] private Behaviour[] disableDuringInspection = new Behaviour[0];

    [Header("Fixed Pose")]
    [SerializeField] private string inspectionLayerName = "RoomInspection";
    [SerializeField] private Vector3 inspectionEulerAngles = new Vector3(15f, 160f, -20f);
    [Min(0.01f)] [SerializeField] private float displayScale = 1f;
    [Range(15f, 65f)] [SerializeField] private float initialFieldOfView = 35f;

    [Header("Slide - posisi relatif terhadap Anchor")]
    [SerializeField] private Vector3 slideOffset = new Vector3(1f, 0f, 0f);
    [Min(0f)] [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private bool closeWhenSolved = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onInspectionStarted = new UnityEvent();
    [SerializeField] private UnityEvent onSlideInFinished = new UnityEvent();
    [SerializeField] private UnityEvent onInspectionEnded = new UnityEvent();

    private static RoomLockInspector activeInspector;
    private ViewState state;
    private RoomPlayerInteractor playerInteractor;
    private Behaviour[] suspendedControls;
    private bool[] controlStates;
    private Transform previousParent;
    private Vector3 previousPosition, previousScale, shownPosition;
    private Quaternion previousRotation;
    private Transform[] modelTransforms;
    private int[] previousLayers;
    private int inspectionLayer;
    private bool previousInteractorEnabled, previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private UniversalAdditionalCameraData overlayData;
    private List<Camera> cameraStack;
    private bool addedToStack, previousCameraEnabled;
    private int previousGameMask, previousInspectionMask;
    private float previousFieldOfView;
    private Rect previousCameraRect;
    private CameraRenderType previousRenderType;
    private bool previousPostProcessing, previousShadows;
    private bool previousLightEnabled;
    private int previousLightMask;
//some fuckass fix lmao
    public bool IsInspecting => state != ViewState.Closed;
    public bool CanInspect => !IsInspecting && isActiveAndEnabled &&
        puzzle != null && puzzle.isActiveAndEnabled && puzzle.IsReady;

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
        StopAllCoroutines();
        RestoreView();
    }

    private bool ValidateView(RoomPlayerInteractor source)
    {
        if (gameCamera == null) gameCamera = Camera.main;
        inspectionLayer = LayerMask.NameToLayer(inspectionLayerName);
        if (inspectionLayer < 0)
        {
            Debug.LogError("Buat layer bernama " + inspectionLayerName + " terlebih dahulu.", this);
            return false;
        }
        if (source == null || gameCamera == null || inspectionCamera == null ||
            inspectionAnchor == null || inspectionCamera == gameCamera ||
            inspectionCamera.orthographic || !gameCamera.isActiveAndEnabled ||
            !inspectionCamera.gameObject.activeInHierarchy ||
            !inspectionAnchor.gameObject.activeInHierarchy ||
            !inspectionCamera.transform.IsChildOf(gameCamera.transform) ||
            !inspectionAnchor.IsChildOf(inspectionCamera.transform) ||
            inspectionAnchor.IsChildOf(transform) || inspectionCamera.targetTexture != null)
        {
            Debug.LogError("Cek Main Camera > RoomInspectCamera > RoomInspectAnchor. Kamera inspect harus Perspective dan Target Texture kosong.", this);
            return false;
        }
        UniversalAdditionalCameraData baseData = gameCamera.GetUniversalAdditionalCameraData();
        if (baseData.renderType != CameraRenderType.Base)
        {
            Debug.LogError("Game Camera harus URP Base Camera.", this);
            return false;
        }
        cameraStack = baseData.cameraStack;
        if (cameraStack == null)
        {
            Debug.LogError("Renderer Main Camera belum mendukung camera stacking URP.", this);
            return false;
        }
        overlayData = inspectionCamera.GetUniversalAdditionalCameraData();
        if (!overlayData.clearDepth)
        {
            Debug.LogError("Centang Clear Depth pada kamera Overlay untuk menampilkan gembok di depan level.", this);
            return false;
        }
        return true;
    }

    public void Interact(RoomPlayerInteractor source)
    {
        if (!CanInspect || activeInspector != null || !ValidateView(source)) return;

        previousParent = transform.parent;
        previousPosition = transform.localPosition;
        previousRotation = transform.localRotation;
        previousScale = transform.localScale;
        Vector3 worldScale = transform.lossyScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        playerInteractor = source;
        previousInteractorEnabled = source.enabled;
        suspendedControls = disableDuringInspection == null
            ? new Behaviour[0] : (Behaviour[])disableDuringInspection.Clone();
        controlStates = new bool[suspendedControls.Length];
        for (int i = 0; i < suspendedControls.Length; i++)
            controlStates[i] = suspendedControls[i] != null && suspendedControls[i].enabled;

        modelTransforms = GetComponentsInChildren<Transform>(true);
        previousLayers = new int[modelTransforms.Length];
        for (int i = 0; i < modelTransforms.Length; i++)
            previousLayers[i] = modelTransforms[i].gameObject.layer;
        previousGameMask = gameCamera.cullingMask;
        previousInspectionMask = inspectionCamera.cullingMask;
        previousCameraEnabled = inspectionCamera.enabled;
        previousFieldOfView = inspectionCamera.fieldOfView;
        previousCameraRect = inspectionCamera.rect;
        previousRenderType = overlayData.renderType;
        previousPostProcessing = overlayData.renderPostProcessing;
        previousShadows = overlayData.renderShadows;
        addedToStack = !cameraStack.Contains(inspectionCamera);
        if (inspectionLight != null)
        {
            previousLightEnabled = inspectionLight.enabled;
            previousLightMask = inspectionLight.cullingMask;
        }

        state = ViewState.Opening;
        activeInspector = this;
        source.enabled = false;
        for (int i = 0; i < suspendedControls.Length; i++)
            if (CanSuspend(suspendedControls[i])) suspendedControls[i].enabled = false;
        for (int i = 0; i < modelTransforms.Length; i++)
            modelTransforms[i].gameObject.layer = inspectionLayer;

        gameCamera.cullingMask &= ~(1 << inspectionLayer);
        inspectionCamera.cullingMask = 1 << inspectionLayer;
        inspectionCamera.fieldOfView = initialFieldOfView;
        inspectionCamera.rect = gameCamera.rect;
        overlayData.renderType = CameraRenderType.Overlay;
        overlayData.renderPostProcessing = false;
        overlayData.renderShadows = false;
        if (addedToStack) cameraStack.Add(inspectionCamera);

        transform.SetParent(inspectionAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(inspectionEulerAngles);
        transform.localScale = worldScale * displayScale;
        CenterModel();
        shownPosition = transform.localPosition;
        transform.localPosition = shownPosition + slideOffset;
        if (inspectionLight != null)
        {
            inspectionLight.cullingMask = 1 << inspectionLayer;
            inspectionLight.enabled = true;
        }
        inspectionCamera.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Physics.SyncTransforms();
        onInspectionStarted.Invoke();
        if (state == ViewState.Opening)
            StartCoroutine(Slide(transform.localPosition, shownPosition, true));
    }

    private bool CanSuspend(Behaviour control)
    {
        return control != null && control != this && control != puzzle &&
            control != playerInteractor && control != gameCamera &&
            control != inspectionCamera && control != inspectionLight;
    }

    private void CenterModel()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        transform.position += inspectionAnchor.position - bounds.center;
    }

    private IEnumerator Slide(Vector3 from, Vector3 to, bool opening)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            transform.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.localPosition = to;
        if (opening)
        {
            state = ViewState.Ready;
            onSlideInFinished.Invoke();
        }
        else RestoreView();
    }

    private void Update()
    {
        if (!IsInspecting) return;
        if (inspectionCamera == null || inspectionAnchor == null || gameCamera == null)
        {
            StopAllCoroutines();
            RestoreView();
            return;
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            CloseInspect();
            return;
        }
        if (state != ViewState.Ready) return;
        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            puzzle.CheckCode();
        if (state != ViewState.Ready) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        int direction = 0;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            bool reverse = keyboard != null &&
                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            direction = reverse ? -1 : 1;
        }
        else if (mouse.rightButton.wasPressedThisFrame) direction = -1;
        else
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) direction = scroll > 0f ? 1 : -1;
        }
        if (direction != 0) TurnWheelAt(mouse.position.ReadValue(), direction);
    }

    private void TurnWheelAt(Vector2 screenPosition, int direction)
    {
        Physics.SyncTransforms();
        Ray ray = inspectionCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, inspectionCamera.farClipPlane,
            1 << inspectionLayer, QueryTriggerInteraction.Collide);
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
        if (direction > 0) puzzle.RotateNext(nearestIndex);
        else puzzle.RotatePrevious(nearestIndex);
    }

    private void HandleUnlocked()
    {
        if (closeWhenSolved) CloseInspect();
    }

    public void CloseInspect()
    {
        if (state == ViewState.Closed || state == ViewState.Closing) return;
        StopAllCoroutines();
        state = ViewState.Closing;
        StartCoroutine(Slide(transform.localPosition, shownPosition + slideOffset, false));
    }

    private void RestoreView()
    {
        if (state == ViewState.Closed) return;
        state = ViewState.Closed;
        if (activeInspector == this) activeInspector = null;
        transform.SetParent(previousParent != null ? previousParent : null, false);
        transform.localPosition = previousPosition;
        transform.localRotation = previousRotation;
        transform.localScale = previousScale;
        if (modelTransforms != null)
            for (int i = 0; i < modelTransforms.Length; i++)
                if (modelTransforms[i] != null) modelTransforms[i].gameObject.layer = previousLayers[i];

        if (addedToStack && cameraStack != null) cameraStack.Remove(inspectionCamera);
        if (inspectionCamera != null)
        {
            inspectionCamera.enabled = previousCameraEnabled;
            inspectionCamera.cullingMask = previousInspectionMask;
            inspectionCamera.fieldOfView = previousFieldOfView;
            inspectionCamera.rect = previousCameraRect;
        }
        if (overlayData != null)
        {
            overlayData.renderType = previousRenderType;
            overlayData.renderPostProcessing = previousPostProcessing;
            overlayData.renderShadows = previousShadows;
        }
        if (gameCamera != null) gameCamera.cullingMask = previousGameMask;
        if (inspectionLight != null)
        {
            inspectionLight.enabled = previousLightEnabled;
            inspectionLight.cullingMask = previousLightMask;
        }
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
