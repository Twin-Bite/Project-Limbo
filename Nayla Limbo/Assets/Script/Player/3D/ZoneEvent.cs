using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ZoneEvent : MonoBehaviour
{
    [Header("=== Interaction Settings ===")]
    [Tooltip("Otomatis Aktif pas player masuk zona")]
    public bool isInRange = false;

    [Tooltip("Tombol buat di tekan untuk trigger event.")]
    public KeyCode interactButton = KeyCode.E;
    
    [Tooltip("Kalo dicentang, zona cuman bisa di trigger satu kali.")]
    public bool isOneShot = false;

    [Tooltip("Jeda, setelah trigger nyala berapa detik sebelum zona aktif lagi.")]
    [Range(0f, 3f)]
    public float cooldown = 0.5f;

    [Header("=== Intearction UI ===")]
    [Tooltip("GameObject UI, bakal muncul pas dalam Zona.")]
    public GameObject interactionPrompt;

    [Header("=== Zone Evenet ===")]
    [Tooltip("Buat nyambungin Event, yang pas di jalanin.")]
    public UnityEvent zoneEvent = new UnityEvent();

    // Buat debugging. Gw buatin biar gampang
    [Header("=== Debug ===")]
    [Tooltip("Diaktifin aja, buat liat di konsol. Kalo dah ga butuh, matiin yak.")]
    public bool enableDebugLog = true;

    // Inisiasi
    private bool _isOnCooldown = false;
    private bool _hasTriggered = false;

    // Lifecycle

    void Start()
    {
        SetPromptVisible(false);
    }

    void Update()
    {
        if (!isInRange) return;
        if (_isOnCooldown) return;
        if (isOneShot && _hasTriggered) return;

        if (Input.GetKeyDown(interactButton))
        {
            StartCoroutine(HandleInteraction());
        }
    }

    IEnumerator HandleInteraction()
    {
        isInRange = false;
        _isOnCooldown = true;

        SetPromptVisible(false);

        if (isOneShot) _hasTriggered = true;
        Log($"Interaction Triggered -> [{gameObject.name}]");

        zoneEvent?.Invoke();
        yield return new WaitForSeconds(cooldown);

        _isOnCooldown = false;
        Log("Cooldown selesai, zona bisa di pakai lagi.");
    }

    // Trigger Detection
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isOneShot && _hasTriggered)
        {
            Log("One-shot sudah trigger, ga aktif lagi");
            return;
        }

        if (_isOnCooldown) return;
        isInRange = true;
        SetPromptVisible(true);

        Log($"Player masuk Zona -> [{gameObject.name}]");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isInRange = false;
        SetPromptVisible(false);

        Log($"Player keluar Zona -> [{gameObject.name}]");
    }

    public void ResetZone()
    {
        isInRange = false;
        _isOnCooldown = false;
        _hasTriggered = false;
        SetPromptVisible(false);
        Log("Zona di-reset.");
    }

    public void ForceDeactivate()
    {
        StopAllCoroutines();
        isInRange = false;
        _isOnCooldown = false;
        SetPromptVisible(false);
        Log("Zona di-nonaktifkan paksa");
    }

    // Private Helper
    void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(visible);
    }

    void Log(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[Zone Event] {message}");
    }
}