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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (activeCam != null)
            {
                activeCam.Priority = 10;
            }
            onEnterEvent.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (activeCam != null)
            {
                activeCam.Priority = 0;
            }
            onExitEvent.Invoke();
        }
    }
}