using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TeleBehaviour : MonoBehaviour
{
    [Header("=== Teleport Settings ===")]
    [Tooltip("Object yang akan di-teleport (contoh: Player)")]
    public Transform targetTransform;

    [Tooltip("Lokasi tujuan teleport (contoh: Empty Object B)")]
    public Transform targetLocation;

    [Header("=== Delay Settings ===")]
    [Tooltip("Aktifkan jika ingin ada jeda sebelum teleport")]
    public bool useDelayedStart = false;

    [Tooltip("Berapa detik jeda sebelum teleport (aktif jika Use Delayed Start dicentang)")]
    public float delayedTime = 1f;

    [Header("=== On Complete Move ===")]
    [Tooltip("Event yang dijalankan setelah teleport selesai")]
    public UnityEvent onCompleteMove;

    private Vector3    _initialPosition;
    private Quaternion _initialRotation;

    void Start()
    {
        if (targetTransform != null)
        {
            _initialPosition = targetTransform.position;
            _initialRotation = targetTransform.rotation;
        }
    }

    public void BeginTeleport()
    {
        if (useDelayedStart)
            StartCoroutine(RunAfterDelay(DoTeleport));
        else
            DoTeleport();
    }

    public void BeginTeleportInitialPosition()
    {
        if (useDelayedStart)
            StartCoroutine(RunAfterDelay(DoTeleportToInitial));
        else
            DoTeleportToInitial();
    }

    void DoTeleport()
    {
        if (targetTransform == null || targetLocation == null)
        {
            Debug.LogWarning("[TeleBehaviour] Target Transform atau Target Location belum di-assign!");
            return;
        }

        MoveTarget(targetLocation.position, targetLocation.rotation);
        onCompleteMove?.Invoke();
    }

    void DoTeleportToInitial()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("[TeleBehaviour] Target Transform belum di-assign!");
            return;
        }

        MoveTarget(_initialPosition, _initialRotation);
        onCompleteMove?.Invoke();
    }

    void MoveTarget(Vector3 position, Quaternion rotation)
    {
        CharacterController cc = targetTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        targetTransform.position = position;
        targetTransform.rotation = rotation;

        if (cc != null) cc.enabled = true;
    }

    IEnumerator RunAfterDelay(System.Action action)
    {
        yield return new WaitForSeconds(delayedTime);
        action?.Invoke();
    }
}