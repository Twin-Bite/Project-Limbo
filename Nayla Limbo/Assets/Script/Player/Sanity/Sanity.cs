using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SanitySystem : MonoBehaviour
{
   public float maxSanity = 100f;
   public float currentSanity;
   public float drainRate = 5f;
   public float rechargeRate = 10f;
   public float delayBeforeDrain = 15f;
   public Slider sanitySlider;

   private Coroutine sanityRoutine;
   private bool isDraining = false;
   private bool isInZone = false;

   void Start()
    {
        currentSanity = maxSanity;
        UpdateUI();
    }

    void Update()
    {
        if (!isInZone)
        {
            currentSanity = Mathf.MoveTowards(currentSanity, maxSanity, rechargeRate * Time.deltaTime);
        } else if (isDraining)
        {
            currentSanity = Mathf.MoveTowards(currentSanity, 0f, drainRate * Time.deltaTime);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (sanitySlider)
        {
            sanitySlider.maxValue = maxSanity;
            sanitySlider.value = currentSanity;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SanityZone"))
            StartDrainSequence();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SanityZone") && !isInZone)
            StartDrainSequence();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SanityZone"))
        {
            isInZone = false;
            if (sanityRoutine != null) StopCoroutine(sanityRoutine);
            isDraining = false;
        }
    }

    private void StartDrainSequence()
    {
        isInZone = true;
        if (sanityRoutine != null) StopCoroutine(sanityRoutine);
        sanityRoutine = StartCoroutine(WaitAndDrain());
    }

    private IEnumerator WaitAndDrain()
    {
        yield return new WaitForSeconds(delayBeforeDrain);
        if (isInZone) isDraining = true;
    }
}