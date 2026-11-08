using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EnemyJumpscare : MonoBehaviour
{
    [Header("UI - script harus berada pada object yang tetap aktif")]
    [SerializeField] private GameObject jumpscarePanel;
    [SerializeField] private GameObject gameOverPanel;
    [Min(0.1f)] [SerializeField] private float displayDuration = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpscareSound;
    [Range(0f, 1f)] [SerializeField] private float soundVolume = 1f;

    [Header("Player Controls")]
    [SerializeField] private Behaviour[] controlsToDisable = new Behaviour[0];
    [SerializeField] private bool pauseGame = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onJumpscareStarted = new UnityEvent();
    [SerializeField] private UnityEvent onJumpscareFinished = new UnityEvent();

    private Behaviour[] lockedControls;
    private bool[] previousControlStates;
    private bool triggered, ownsLock, previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private float previousTimeScale;
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (jumpscarePanel != null && jumpscarePanel != gameObject) jumpscarePanel.SetActive(false);
        if (gameOverPanel != null && gameOverPanel != gameObject) gameOverPanel.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
    }

    public void Play()
    {
        if (!isActiveAndEnabled || triggered) return;
        if (jumpscarePanel == null || jumpscarePanel == gameObject)
        {
            Debug.LogError("EnemyJumpscare: isi panel child, dan pasang script pada Canvas/manager yang tetap aktif.", this);
            return;
        }
        triggered = true;
        ownsLock = true;
        IsPlaying = true;
        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        lockedControls = controlsToDisable == null
            ? new Behaviour[0] : (Behaviour[])controlsToDisable.Clone();
        previousControlStates = new bool[lockedControls.Length];
        for (int i = 0; i < lockedControls.Length; i++)
        {
            Behaviour control = lockedControls[i];
            previousControlStates[i] = control != null && control.enabled;
            if (control != null && control != this) control.enabled = false;
        }
        if (pauseGame) Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        jumpscarePanel.SetActive(true);
        if (jumpscareSound != null) audioSource.PlayOneShot(jumpscareSound, soundVolume);
        onJumpscareStarted.Invoke();
        if (isActiveAndEnabled && ownsLock) StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        IsPlaying = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            jumpscarePanel.SetActive(false);
        }
        // Tanpa panel game-over, gambar tetap tampil sampai scene diulang.
        onJumpscareFinished.Invoke();
    }

    private void LateUpdate()
    {
        // Puzzle yang baru selesai menutup tidak boleh membuka kontrol saat sudah tertangkap.
        if (!ownsLock || lockedControls == null) return;
        foreach (Behaviour control in lockedControls)
            if (control != null && control != this && control.enabled) control.enabled = false;
    }

    public void RestartCurrentScene()
    {
        int buildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        if (buildIndex < 0)
        {
            Debug.LogError("Tambahkan scene aktif ke Scene List di Build Profiles sebelum memakai Restart.", this);
            return;
        }
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(buildIndex);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (!ownsLock) return;
        ownsLock = false;
        IsPlaying = false;
        if (pauseGame && Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = previousTimeScale;
        if (lockedControls != null)
            for (int i = 0; i < lockedControls.Length; i++)
                if (lockedControls[i] != null && lockedControls[i] != this)
                    lockedControls[i].enabled = previousControlStates[i];
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }
}
