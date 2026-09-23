using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera doorCam;
    public Transform doorMesh;
    public string nextSceneName;

    [Header("Settings")]
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public int doorCamPriority = 20;
    public int defaultCamPriority = 0;

    private bool isPlayerInZone = false;
    private bool canOpenDoor = false;
    private bool isOpening = false;

    private Quaternion closedRot;
    private Quaternion openRot;

    private static CinemachineCamera currentActiveCam;

    private void Start()
    {
        if (doorMesh == null)
        {
            Debug.LogWarning("Mesh pintu belum di-assign!");
            return;
        }

        closedRot = doorMesh.rotation;
        openRot = Quaternion.Euler(doorMesh.eulerAngles + new Vector3(0, openAngle, 0));

        if (doorCam != null)
        {
            doorCam.Priority = defaultCamPriority;
            doorCam.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInZone = true;

        if (currentActiveCam != null && currentActiveCam != doorCam)
            currentActiveCam.gameObject.SetActive(false);

        if (doorCam != null)
        {
            doorCam.gameObject.SetActive(true);
            doorCam.Priority = doorCamPriority;
            currentActiveCam = doorCam;
        }
        StartCoroutine(AllowInteractionAfterDelay(0.3f));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInZone = false;
        canOpenDoor = false;

        if (doorCam != null)
        {
            doorCam.Priority = defaultCamPriority;
            doorCam.gameObject.SetActive(false);
        }

        if (currentActiveCam == doorCam)
            currentActiveCam = null;
    }

    private IEnumerator AllowInteractionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isPlayerInZone)
            canOpenDoor = true;
    }

    private void Update()
    {
        if (canOpenDoor && !isOpening && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(OpenDoor());
        }
    }

    private IEnumerator OpenDoor()
    {
        isOpening = true;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            doorMesh.rotation = Quaternion.Slerp(closedRot, openRot, t);
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);

        if (doorCam != null)
        {
            doorCam.Priority = defaultCamPriority;
            doorCam.gameObject.SetActive(false);
        }

        if (currentActiveCam == doorCam)
            currentActiveCam = null;
    }
}
