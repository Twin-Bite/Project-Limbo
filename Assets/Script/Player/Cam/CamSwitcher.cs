using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class CamSwitcher : MonoBehaviour
{
    [Header("Auto Detect")]
    public Transform Player;
    public CinemachineCamera activeCam;

    [Header("Events")]
    public UnityEvent onEnterEvent;
    public UnityEvent onExitEvent;

    private static CinemachineCamera currentActiveCam;

    private void Reset()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (Player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) Player = playerObj.transform;
        }

        if (activeCam == null)
        {
            activeCam = GetComponentInChildren<CinemachineCamera>();
        }

        if (activeCam != null)
            activeCam.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (currentActiveCam != null && currentActiveCam != activeCam)
            currentActiveCam.gameObject.SetActive(false);

        if (activeCam != null)
        {
            activeCam.gameObject.SetActive(true);
            activeCam.Priority = 10;
            currentActiveCam = activeCam;
        }

        onEnterEvent.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (activeCam != null)
        {
            activeCam.Priority = 0;
            activeCam.gameObject.SetActive(false);
        }

        if (currentActiveCam == activeCam)
            currentActiveCam = null;

        onExitEvent.Invoke();
    }
}
