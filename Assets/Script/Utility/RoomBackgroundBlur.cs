using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class RoomBackgroundBlur : MonoBehaviour
{
    [Header("Background Blur")]
    [SerializeField] private string volumeLayerName = "RoomInspection";
    [SerializeField] private DepthOfFieldMode blurMode = DepthOfFieldMode.Bokeh;
    [Range(1f, 8f)]
    [SerializeField] private float bokehAperture = 1f;
    [Range(0.5f, 1.5f)]
    [SerializeField] private float blurRadius = 1.5f;
    [SerializeField] private bool highQuality = true;

    private Camera targetCamera;
    private UniversalAdditionalCameraData cameraData;
    private GameObject runtimeObject;
    private Volume runtimeVolume;
    private VolumeProfile runtimeProfile;
    private bool active;
    private bool previousPostProcessing;
    private LayerMask previousVolumeMask;
    private CameraOverrideOption previousDepthOption;
    private VolumeFrameworkUpdateMode previousVolumeUpdateMode;

    public void EnableBlur()
    {
        if (active || !isActiveAndEnabled) return;
        targetCamera = GetComponent<Camera>();
        cameraData = targetCamera.GetUniversalAdditionalCameraData();
        if (cameraData.renderType != CameraRenderType.Base)
        {
            Debug.LogError("Pasang dlu RoomBackgroundBlur ke MainCamera", this);
            return;
        }
        int layer = LayerMask.NameToLayer(volumeLayerName);
        if (layer < 0)
        {
            Debug.LogError("Layer " + volumeLayerName + " belum dibuat.", this);
            return;
        }

        previousPostProcessing = cameraData.renderPostProcessing;
        previousVolumeMask = cameraData.volumeLayerMask;
        previousDepthOption = cameraData.requiresDepthOption;
        previousVolumeUpdateMode = targetCamera.GetVolumeFrameworkUpdateMode();

        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        runtimeProfile.name = "RoomBackgroundBlurProfile";
        runtimeProfile.hideFlags = HideFlags.DontSave;
        DepthOfField depth = runtimeProfile.Add<DepthOfField>(true);
        depth.mode.Override(blurMode);
        depth.gaussianStart.Override(0f);
        depth.gaussianEnd.Override(0.1f);
        depth.gaussianMaxRadius.Override(blurRadius);
        depth.highQualitySampling.Override(highQuality);

        // Gembok digambar belakangan oleh kamera Overlay tanpa post-processing.
        depth.focusDistance.Override(0.2f);
        depth.focalLength.Override(50f);
        depth.aperture.Override(bokehAperture);

        runtimeObject = new GameObject("RoomBackgroundBlurVolume");
        runtimeObject.SetActive(false);
        runtimeObject.hideFlags = HideFlags.DontSave;
        runtimeObject.layer = layer;
        runtimeObject.transform.SetParent(transform, false);
        runtimeVolume = runtimeObject.AddComponent<Volume>();
        runtimeVolume.isGlobal = true;
        runtimeVolume.priority = 10000f;
        runtimeVolume.weight = 1f;
        runtimeVolume.sharedProfile = runtimeProfile;

        active = true;
        cameraData.volumeLayerMask = previousVolumeMask.value | (1 << layer);
        cameraData.renderPostProcessing = true;
        cameraData.requiresDepthOption = CameraOverrideOption.On;
        targetCamera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);
        runtimeObject.SetActive(true);
    }

    public void DisableBlur()
    {
        if (!active) return;
        active = false;
        if (runtimeVolume != null) runtimeVolume.enabled = false;
        if (runtimeObject != null) runtimeObject.SetActive(false);

        if (cameraData != null)
        {
            cameraData.volumeLayerMask = previousVolumeMask;
            cameraData.renderPostProcessing = previousPostProcessing;
            cameraData.requiresDepthOption = previousDepthOption;
        }
        if (targetCamera != null)
            targetCamera.SetVolumeFrameworkUpdateMode(previousVolumeUpdateMode);

        if (runtimeVolume != null) runtimeVolume.sharedProfile = null;
        if (runtimeObject != null) Destroy(runtimeObject);
        if (runtimeProfile != null)
        {
            // VolumeComponent yang dibuat runtime juga perlu dibersihkan.
            foreach (VolumeComponent component in runtimeProfile.components)
                if (component != null) Destroy(component);
            runtimeProfile.components.Clear();
            Destroy(runtimeProfile);
        }
        runtimeObject = null;
        runtimeVolume = null;
        runtimeProfile = null;
    }

    private void OnDisable() { DisableBlur(); }
    private void OnDestroy() { DisableBlur(); }
}
