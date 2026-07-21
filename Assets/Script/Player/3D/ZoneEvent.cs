using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ZoneEvent : MonoBehaviour
{
    [Header("=== Interaction Settings ===")]

    [Tooltip("Otomatis aktif saat player masuk zona, nonaktif saat keluar.\n" +
             "Bisa dilihat di Inspector untuk debug.")]
    public bool isInRange = false;

    [Tooltip("Tombol yang ditekan player untuk trigger event.\n" +
             "Default: E")]
    public KeyCode interactButton = KeyCode.E;

    [Tooltip("Jika dicentang, zona hanya bisa di-trigger SATU KALI.\n" +
             "Setelah triggered, tidak akan aktif lagi sampai ResetZone() dipanggil.")]
    public bool isOneShot = false;

    [Tooltip("Jeda (detik) setelah event dijalankan sebelum zona bisa aktif lagi.\n" +
             "Berguna untuk mencegah double-trigger dan bug teleport.")]
    [Range(0f, 3f)]
    public float cooldown = 0.5f;

    [Header("=== Interaction Sound ===")]

    [Tooltip("Suara yang diputar saat player menekan tombol interaksi.\n" +
             "Contoh: suara pintu, tombol, langkah, dsb.\n" +
             "Kosongkan jika tidak perlu suara.")]
    public AudioClip interactSound;

    [Tooltip("Volume suara interaksi.")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Tooltip("Spatial Blend suara:\n" +
             "0 = 2D (volume sama di mana pun player berada)\n" +
             "1 = 3D (makin jauh dari objek, makin kecil suaranya)")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;

    [Header("=== Interaction Prompt (Opsional) ===")]

    [Tooltip("GameObject UI yang muncul saat player dalam zona.\n" +
             "Contoh: Text 'Tekan E', ikon interaksi, dsb.\n" +
             "Kosongkan jika tidak diperlukan.")]
    public GameObject interactionPrompt;

    [Header("=== Zone Event ===")]

    [Tooltip("Event yang dijalankan saat player menekan tombol di dalam zona.\n" +
             "Sambungkan ke FadeBehaviour.BeginFadingIn, TeleBehaviour.BeginTeleport, dll.")]
    public UnityEvent zoneEvent = new UnityEvent();

    [Header("=== Debug ===")]

    [Tooltip("Aktifkan untuk melihat log di Console.\n" +
             "Matikan saat build final untuk performa lebih baik.")]
    public bool enableDebugLog = true;

    private bool        _isOnCooldown = false;
    private bool        _hasTriggered = false;
    private AudioSource _audioSource;


    void Start()
    {
        SetPromptVisible(false);
        SetupAudio();
    }

    void Update()
    {
        if (!isInRange)                    return;
        if (_isOnCooldown)                 return;
        if (isOneShot && _hasTriggered)    return;

        if (Input.GetKeyDown(interactButton))
            StartCoroutine(HandleInteraction());
    }

    void SetupAudio()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake   = false;
        _audioSource.spatialBlend  = spatialBlend;
        _audioSource.volume        = soundVolume;
        if (spatialBlend > 0f)
        {
            _audioSource.minDistance = 1f;
            _audioSource.maxDistance = 15f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    void PlayInteractSound()
    {
        if (_audioSource == null || interactSound == null) return;

        _audioSource.volume       = soundVolume;
        _audioSource.spatialBlend = spatialBlend;

        _audioSource.PlayOneShot(interactSound);
    }

    IEnumerator HandleInteraction()
    {
        isInRange     = false;
        _isOnCooldown = true;

        SetPromptVisible(false);

        if (isOneShot) _hasTriggered = true;

        Log($"Interaction triggered → [{gameObject.name}]");

        PlayInteractSound();
        zoneEvent?.Invoke();

        yield return new WaitForSeconds(cooldown);

        _isOnCooldown = false;
        Log("Cooldown selesai, zona siap digunakan lagi.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))        return;
        if (isOneShot && _hasTriggered)         return;
        if (_isOnCooldown)                      return;

        isInRange = true;
        SetPromptVisible(true);

        Log($"Player masuk zona → [{gameObject.name}]");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isInRange = false;
        SetPromptVisible(false);

        Log($"Player keluar zona → [{gameObject.name}]");
    }

    public void ResetZone()
    {
        isInRange     = false;
        _isOnCooldown = false;
        _hasTriggered = false;
        SetPromptVisible(false);
        Log("Zona di-reset.");
    }

    public void ForceDeactivate()
    {
        StopAllCoroutines();
        isInRange     = false;
        _isOnCooldown = false;
        SetPromptVisible(false);
        Log("Zona di-nonaktifkan paksa.");
    }

    void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(visible);
    }

    void Log(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[ZoneEvent] {message}");
    }
}