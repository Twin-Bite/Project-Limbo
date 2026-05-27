using UnityEngine;
using UnityEngine.Events;

public class ZoneEvent : MonoBehaviour
{
    public bool isInRange;
    public KeyCode interactButton;
    
    public UnityEvent zoneEvent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
        if (isInRange)
        {
            if (Input.GetKeyDown(interactButton))
            {
                Debug.Log(("Interaction Success"));
                zoneEvent.Invoke();
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = true;
            Debug.Log("Inside Interaction Area");
        }
        
      
    }
    
    
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            isInRange = false;
        }
    }
}