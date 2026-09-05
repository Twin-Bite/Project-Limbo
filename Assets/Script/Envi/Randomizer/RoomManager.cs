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
}
