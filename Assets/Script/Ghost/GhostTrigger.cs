using System.Collections;
using UnityEngine;

/// <summary>
/// Pasang script ini di GameObject TRIGGER (bukan di Ghost langsung).
/// Assign Ghost GameObject ke field ghostObject di Inspector.
/// Pilih salah satu mode: isShow, isMoving, atau isChase.
/// AudioSource akan otomatis dibuat di Ghost object jika belum ada.
/// </summary>
public class GhostTrigger : MonoBehaviour
{
    [Header("Ghost Reference")]
    [Tooltip("Drag GameObject Ghost (Capsule) ke sini")]
    public GameObject ghostObject;

    [Header("Mode Ghost")]
    [Tooltip("Ghost muncul sebentar lalu menghilang")]
    public bool isShow = false;

    [Tooltip("Ghost bergerak mengikuti waypoints")]
    public bool isMoving = false;

    [Tooltip("Ghost mengejar player selama beberapa detik")]
    public bool isChase = false;

    // -------------------------------------------------------

    [Header("isShow Settings")]
    [Tooltip("Berapa detik ghost terlihat sebelum menghilang")]
    public float showDuration = 3f;

    [Tooltip("Durasi fade out ghost sebelum menghilang (detik)")]
    public float showFadeOutDuration = 1f;

    // -------------------------------------------------------

    [Header("isMoving Settings")]
    [Tooltip("Tambah Transform kosong sebagai titik tujuan ghost")]
    public Transform[] waypoints;
    public float moveSpeed = 3f;

    // -------------------------------------------------------

    [Header("isChase Settings")]
    public float chaseSpeed = 5f;
    [Tooltip("Berapa detik ghost mengejar player sebelum menghilang")]
    public float chaseDuration = 6f;

    // -------------------------------------------------------

    [Header("Audio Settings")]
    [Tooltip("Suara sekali saat ghost pertama muncul (appear & isShow)")]
    public AudioClip ghostAppearSound;

    [Tooltip("Suara ambient loop saat ghost aktif")]
    public AudioClip ghostAmbientSound;

    [Tooltip("Suara loop saat mode isChase aktif (opsional, gantikan ambient)")]
    public AudioClip ghostChaseSound;

    [Tooltip("Volume suara ambient")]
    [Range(0f, 1f)]
    public float ambientVolume = 0.8f;

    [Tooltip("Volume suara chase")]
    [Range(0f, 1f)]
    public float chaseVolume = 1f;

    private Transform playerTransform;
    private bool hasTriggered = false;
    private int waypointIndex = 0;
    private bool isCurrentlyChasing = false;
    private CharacterController ghostCC;
    private AudioSource ghostAudioSource;

    void Start()
    {
        ValidateMode();

        if (ghostObject != null)
        {
            ghostObject.SetActive(false);
            ghostCC = ghostObject.GetComponent<CharacterController>();

            ghostAudioSource = ghostObject.GetComponent<AudioSource>();
            if (ghostAudioSource == null)
            {
                ghostAudioSource = ghostObject.AddComponent<AudioSource>();
                Debug.Log("[GhostTrigger] AudioSource otomatis dibuat di Ghost.");
            }

            SetupGhostAudio();
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogWarning("[GhostTrigger] Player dengan tag 'Player' tidak ditemukan!");
    }

    void SetupGhostAudio()
    {
        ghostAudioSource.spatialBlend  = 1f;                          
        ghostAudioSource.rolloffMode   = AudioRolloffMode.Linear;     
        ghostAudioSource.minDistance   = 2f;                          
        ghostAudioSource.maxDistance   = 20f;                         
        ghostAudioSource.playOnAwake   = false;
        ghostAudioSource.loop          = true;
        ghostAudioSource.volume        = ambientVolume;
    }

    void Update()
    {
        if (!hasTriggered || ghostObject == null || !ghostObject.activeSelf) return;

        if (isMoving && !isCurrentlyChasing)
            HandleMoving();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            ActivateGhost();
        }
    }

    void ActivateGhost()
    {
        if (ghostObject == null)
        {
            Debug.LogError("[GhostTrigger] ghostObject belum di-assign di Inspector!");
            return;
        }

        ghostObject.SetActive(true);
        Debug.Log($"[GhostTrigger] Ghost aktif! Mode: {GetActiveMode()}");

        if (ghostAppearSound != null)
            ghostAudioSource.PlayOneShot(ghostAppearSound);

        if (isShow)
        {
            StartCoroutine(ShowRoutine());
        }
        else if (isMoving)
        {
            PlayAmbientLoop();
        }
        else if (isChase)
        {
            StartCoroutine(ChaseRoutine());
        }
    }

    IEnumerator ShowRoutine()
    {
        PlayAmbientLoop();

        float waitTime = Mathf.Max(0f, showDuration - showFadeOutDuration);
        Debug.Log($"[GhostTrigger] isShow: ghost terlihat selama {showDuration} detik.");
        yield return new WaitForSeconds(waitTime);

        yield return StartCoroutine(FadeOutAudio(showFadeOutDuration));
        Debug.Log("[GhostTrigger] isShow: ghost menghilang.");
        ghostObject.SetActive(false);
    }

    void HandleMoving()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("[GhostTrigger] isMoving aktif tapi tidak ada waypoints!");
            return;
        }

        Transform target = waypoints[waypointIndex];
        Vector3 direction = (target.position - ghostObject.transform.position).normalized;

        MoveGhost(direction * moveSpeed);

        if (direction != Vector3.zero)
        {
            ghostObject.transform.rotation = Quaternion.Slerp(
                ghostObject.transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 5f
            );
        }

        if (Vector3.Distance(ghostObject.transform.position, target.position) < 0.3f)
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
    }

    IEnumerator ChaseRoutine()
    {
        isCurrentlyChasing = true;
        float timer = 0f;

        if (ghostChaseSound != null)
        {
            ghostAudioSource.clip   = ghostChaseSound;
            ghostAudioSource.volume = chaseVolume;
            ghostAudioSource.loop   = true;
            ghostAudioSource.Play();
        }
        else
        {
            PlayAmbientLoop();
        }

        Debug.Log($"[GhostTrigger] isChase: ghost mengejar selama {chaseDuration} detik.");

        while (timer < chaseDuration)
        {
            if (playerTransform != null && ghostObject.activeSelf)
            {
                Vector3 direction = (playerTransform.position - ghostObject.transform.position).normalized;

                MoveGhost(direction * chaseSpeed);

                if (direction != Vector3.zero)
                {
                    ghostObject.transform.rotation = Quaternion.Slerp(
                        ghostObject.transform.rotation,
                        Quaternion.LookRotation(direction),
                        Time.deltaTime * 10f
                    );
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Fade out lalu hilang
        yield return StartCoroutine(FadeOutAudio(1f));

        Debug.Log("[GhostTrigger] isChase: durasi habis, ghost menghilang.");
        isCurrentlyChasing = false;
        ghostObject.SetActive(false);
    }

    void PlayAmbientLoop()
    {
        if (ghostAmbientSound == null) return;

        ghostAudioSource.clip   = ghostAmbientSound;
        ghostAudioSource.volume = ambientVolume;
        ghostAudioSource.loop   = true;
        ghostAudioSource.Play();
    }

    IEnumerator FadeOutAudio(float fadeDuration)
    {
        if (ghostAudioSource == null || !ghostAudioSource.isPlaying) yield break;

        float startVolume = ghostAudioSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            ghostAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        ghostAudioSource.Stop();
        ghostAudioSource.volume = startVolume;
    }

    void MoveGhost(Vector3 velocity)
    {
        if (ghostCC != null)
        {
            Vector3 move = velocity * Time.deltaTime;
            if (!ghostCC.isGrounded)
                move.y += Physics.gravity.y * Time.deltaTime;
            ghostCC.Move(move);
        }
        else
        {
            ghostObject.transform.position += velocity * Time.deltaTime;
        }
    }

    void ValidateMode()
    {
        int count = (isShow ? 1 : 0) + (isMoving ? 1 : 0) + (isChase ? 1 : 0);
        if (count == 0)
            Debug.LogWarning("[GhostTrigger] Tidak ada mode yang dipilih!");
        if (count > 1)
            Debug.LogWarning("[GhostTrigger] Lebih dari satu mode aktif! Sebaiknya pilih satu saja.");
    }

    string GetActiveMode()
    {
        if (isShow)   return "isShow";
        if (isMoving) return "isMoving";
        if (isChase)  return "isChase";
        return "None";
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            else if (i == waypoints.Length - 1 && waypoints[0] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
        }
    }
}