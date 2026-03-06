using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class CamSwitcher : MonoBehaviour
{
    public GameObject Player;

    public CinemachineCamera activeCam;
    
    public UnityEvent onEnterEvent;
    public UnityEvent onExitEvent;



    private void Start()
    {
        Player = GameObject.Find("Player");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activeCam.Priority = 1;
            onEnterEvent.Invoke();
            
        }
        
      
    }
    
    
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            activeCam.Priority = 0;
            onExitEvent.Invoke();
           
        }
    }
}
