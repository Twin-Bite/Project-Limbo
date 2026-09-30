using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RoomData
{
    public string roomName;
    public Transform entryPoint;

    [Header("Room Event")]
    public UnityEvent onEntered = new UnityEvent();
    public UnityEvent onExited = new UnityEvent();
}

public class RoomManager : MonoBehaviour
{
    private const int RoomCount = 6;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Rooms")]
    [SerializeField] private RoomData[] rooms = new RoomData[RoomCount];

    [Range(0, RoomCount - 1)]
    [SerializeField] private int startingRoomIndex;
    [SerializeField] private bool startAutomatically = true;

    [Header("Transition")]
    [Min(0f)]
    [SerializeField] private float teleportDelay = 0.2f;

    [Min(0.05f)]
    [SerializeField] private float doorCooldown = 0.3f;

    [Header("Global Events")]
    [SerializeField] private UnityEvent onRouteStarted = new UnityEvent();
    [SerializeField] private UnityEvent onBeforeTeleport = new UnityEvent();
    [SerializeField] private UnityEvent onAfterTeleport = new UnityEvent();
    [SerializeField] private UnityEvent onProgressChanged = new UnityEvent();
    [SerializeField] private UnityEvent onAllRoomsVisited = new UnityEvent();
    [SerializeField] private UnityEvent onRouteFinished = new UnityEvent();

    // Menyimpan status ruangan sekarang
    private readonly bool[] visited = new bool[RoomCount];

    // Index Ruangan
    private readonly int[] candidates = new int[RoomCount];
    private CharacterController controller;

    // Inisiasi
    private bool routeReady;
    private bool isTransitioning;
    private bool routeFinished;

    private float nextAllowedTime;

    // Variable Public
    public int CurrentRoomIndex { get; private set; } = -1;
    public int VisitedCount { get; private set; }
    public bool IsTransitioning => isTransitioning;

    private void Start()
    {
        if (startAutomatically && !routeReady)
        {
            StartNewRoute();
        }
    }

    // Reset Route
    // Player balik ke Entry Point
    public void StartNewRoute()
    {
        if (!isActiveAndEnabled || isTransitioning)
        {
            return;
        }

        routeReady = false;

        if (!ValidateConfiguration())
        {
            return;
        }

        controller = player.GetComponent<CharacterController>();
        isTransitioning = true;

        try
        {
            if (!TryTeleport(rooms[startingRoomIndex].entryPoint))
            {
                return;
            }

            Array.Clear(visited, 0, visited.Length);

            CurrentRoomIndex = startingRoomIndex;
            visited[startingRoomIndex] = true;
            VisitedCount = 1;

            routeFinished = false;
            routeReady = true;

            onRouteStarted.Invoke();
            rooms[startingRoomIndex].onEntered.Invoke();
            onProgressChanged.Invoke(VisitedCount);
        }
        finally
        {
            EndTransition();
        }
    }

    // Kita panggil UnityEvent punya pintu.
    public void GoToRandomRoom()
    {
        if (!isActiveEnabled ||
            !routeReady ||
            isTransitioning ||
            Time.unscaledTime < nextAllowedTime)
        {
            return;
        }

        // Kalo udah di kunjungi. Interaksi pintu berikutnya memicu ending.
        if (VisitedCount == RoomCount)
        {
            if (routeFinished)
            {
                return;
            }

            routeFinished = true;
            isTransitioning = true;

            try
            {
                onRouteFinished.Invoke();
            }
            finally
            {
                EndTransition();
            }

            return;
        }

        int candidateCount = 0;

        for (int i = 0; i < RoomCount; i++)
        {
            if (!visited[i])
            {
                candidates[candidateCount] = i;
                candidateCount++;
            }
        }

        int randomIndex = UnityEngine.Random.Range(0, candidateCount);
        int destinationIndex = candidates[randomIndex];

        // Cegah permintaan lain selama proses perpindahan.
        isTransitioning = true;
        StartCoroutine(TeleportRoutine(destinationIndex));
    }

    // Gunakan jika pintu memiliki index ruangan asal. Pintu dari ruangan lain, gabisa memicu perpindahan.
    public void UseExitFrom(int roomIndex)
    {
        if (roomIndex == CurrentRoomIndex)
        {
            GoToRandomRoom();
        }
    }

    // Bisa digunakan script lain untuk memeriksa kunjungan.
    public bool HasVisited(int roomIndex)
    {
        return roomIndex >= 0 && 
               roomIndex < RoomCount &&
               visited[roomIndex];
    }

    private IEnumerator TeleportRoutine(int destinationIndex)
    {
        try
        {
            onBeforeTeleport.Invoke();

            if (teleportDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    teleportDelay
                );
            }

            if (!isActiveEnabled || !routeReady)
            {
                yield break;
            }

            Transform destination =
                rooms[destinationIndex].entryPoint;
            
            if (!TryTeleport(destination))
            {
                // Kalo gagal walaupun belum dikunjungi.
                yield break;
            }

            int previousRoomIndex = CurrentRoomIndex;

            CurrentRoomIndex = destinationIndex;
            visited[destinationIndex] = true;
            VisitedCount++;

            // Perbarui status sebelum event;
            rooms[previousRoomIndex].onExited.Invoke();
            rooms[destinationIndex].onEntered.Invoke();

            onAfterTeleport.Invoke();
            onProgressChanged.Invoke(VisitedCount);

            if (VisitedCount == RoomCount)
            {
                onAllRoomsVisited.Invoke();
            }
        }
        finally
        {
            EndTransition();
        }
    }

    private bool TryTeleport(Transform destination)
    {
        if (player == null || destination == null)
        {
            Debug.LogError("Player atau entry point tidak tersedia", this);
            return false;
        }

        bool wasEnabled = controller != null && controller.enabled;

        if (wasEnabled)
        {
            controller.enabled = false;
        }

        try
        {
            player.SetPositionAndRotation(
                destination.position,
                destination.rotation
            );
        }
        finally
        {
            if (wasEnabled && controller != null)
            {
                controller.enabled = true;
            }
        }

        return true;
    }

    private void EndTransition()
    {
        nextAllowedTime= Time.unscaledTime + 
            Mathf.Max(0.05f, doorCooldown);
        
        isTransitioning = false;
    }

    private bool ValidateConfiguration()
    {
        if (player == null)
        {
            return ConfigurationError(
                "Player belum diisi"
            );
        }

        if (rooms == null || rooms.Length != RoomCount)
        {
            return ConfigurationError(
                "Array rooms harus diisi tepat 6 ruangan."
            );
        }

        if (startingRoomIndex < 0 ||
            startingRoomIndex >= RoomCount)
        {
            return ConfigurationError(
                "Starting Room harus diisi sampai 5"
            );
        }

        for (int i = 0; i < RoomCount; i++)
        {
            if (rooms[i] == null ||
                rooms[i].entryPoint == null)
            {
                return ConfigurationError(
                    $"Entry point pada Rooms[{i}] belum diisi"
                );
            }

            for (int j = 0; j < i; j++)
            {
                if (rooms[i].entryPoint == rooms[j].entryPoint)
                {
                    return ConfigurationError(
                        $"Rooms[{i}] dan Rooms [{j}] " +
                        "menggunakan entry point yang sama"
                    );
                }
            }
        }

        return true;
    }

    private bool ConfigurationError(string message)
    {
        Debug.LogError(message, this);
        return false;
    }

    private void OnDisable()
    {
        StopAllCoroutine();

        routeReady = false;
        isTransitioning = false;
    }
}
