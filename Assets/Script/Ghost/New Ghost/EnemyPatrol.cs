using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum EnemyPatrolState { Stopped, Waiting, Warning, Patrolling, Chasing, Searching, Caught }

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent), typeof(AudioSource))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyTarget player;
    [Tooltip("Child model hantu. Jangan isi root yang memiliki script ini.")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Transform eyes;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorSpeedParameter = "Speed";

    [Header("Points - harus berada pada NavMesh")]
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField] private Transform[] patrolPoints = new Transform[0];

    [Header("Random Encounter")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private Vector2 spawnDelay = new Vector2(20f, 45f);
    [SerializeField] private Vector2 encounterDuration = new Vector2(15f, 30f);
    [Min(0f)] [SerializeField] private float warningDuration = 2f;
    [Min(0f)] [SerializeField] private float minimumSpawnDistance = 6f;
    [Min(1f)] [SerializeField] private float maximumSpawnDistance = 30f;
    [SerializeField] private bool avoidVisibleSpawn = true;

    [Header("Movement")]
    [Min(0.1f)] [SerializeField] private float patrolSpeed = 1.4f;
    [Min(0.1f)] [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private Vector2 waypointPause = new Vector2(0.2f, 1f);
    [Min(0.1f)] [SerializeField] private float arrivalDistance = 0.4f;
    [Min(0.1f)] [SerializeField] private float navMeshSnapDistance = 1f;
    [Min(0.1f)] [SerializeField] private float repathInterval = 0.35f;
    [Min(1f)] [SerializeField] private float stuckTimeout = 5f;

    [Header("Sight and Capture")]
    [Min(0.05f)] [SerializeField] private float thinkInterval = 0.2f;
    [Min(1f)] [SerializeField] private float sightDistance = 10f;
    [Range(1f, 360f)] [SerializeField] private float fieldOfView = 110f;
    [Min(0f)] [SerializeField] private float closeAwarenessDistance = 1.5f;
    [Min(0f)] [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private LayerMask sightBlockingLayers = ~0;
    [Min(0f)] [SerializeField] private float loseSightAfter = 1.5f;
    [Min(0.1f)] [SerializeField] private float searchDuration = 5f;
    [Min(0.1f)] [SerializeField] private float catchDistance = 1.1f;
    [Min(0.1f)] [SerializeField] private float maximumCatchHeightDifference = 1.5f;

    [Header("3D Audio")]
    [SerializeField] private AudioClip patrolLoop;
    [SerializeField] private AudioClip chaseLoop;
    [SerializeField] private AudioClip appearanceSound;
    [Range(0f, 1f)] [SerializeField] private float patrolVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float chaseVolume = 0.9f;
    [Min(0.1f)] [SerializeField] private float soundMinDistance = 2f;
    [Min(1f)] [SerializeField] private float soundMaxDistance = 28f;

    [Header("Unity Events")]
    [SerializeField] private UnityEvent onSpawned = new UnityEvent();
    [SerializeField] private UnityEvent onChaseStarted = new UnityEvent();
    [SerializeField] private UnityEvent onPlayerLost = new UnityEvent();
    [SerializeField] private UnityEvent onDespawned = new UnityEvent();
    [SerializeField] private UnityEvent onPlayerCaught = new UnityEvent();

    private NavMeshAgent agent;
    private AudioSource sound;
    private NavMeshPath path;
    private readonly RaycastHit[] sightHits = new RaycastHit[16];
    private bool ready, animatorHasSpeed, spawnWarningIssued;
    private int speedHash, lastPatrolIndex = -1;
    private float nextSpawnAt, nextThinkAt, lifeEndsAt, warningEndsAt;
    private float lastSeenAt, searchEndsAt, nextRepathAt, nextWaypointAt, lastProgressAt;
    private Vector3 lastSeenPosition, lastProgressPosition;
    public EnemyPatrolState State { get; private set; } = EnemyPatrolState.Stopped;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sound = GetComponent<AudioSource>();
        path = new NavMeshPath();
        agent.enabled = false;
        sound.playOnAwake = false;
        sound.loop = true;
        sound.spatialBlend = 1f;
        sound.rolloffMode = AudioRolloffMode.Linear;
        sound.minDistance = Mathf.Max(0.1f, soundMinDistance);
        sound.maxDistance = Mathf.Max(sound.minDistance + 0.1f, soundMaxDistance);
        sound.dopplerLevel = 0f;
        if (visualRoot == null || visualRoot == gameObject ||
            !visualRoot.transform.IsChildOf(transform))
        {
            Debug.LogError("EnemyPatrol: Visual Root harus child model hantu, bukan root controller.", this);
            enabled = false;
            return;
        }
        visualRoot.SetActive(false);
        if (animator == null) animator = visualRoot.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.applyRootMotion = false;
            speedHash = Animator.StringToHash(animatorSpeedParameter);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float)
                    animatorHasSpeed = true;
        }
        ready = true;
    }

    private void Start()
    {
        if (startAutomatically) StartEncounters();
    }

    public void StartEncounters()
    {
        if (!isActiveAndEnabled || !ready) return;
        if (State != EnemyPatrolState.Stopped && State != EnemyPatrolState.Waiting) return;
        if (player == null)
        {
            GameObject candidate = GameObject.FindGameObjectWithTag("Player");
            if (candidate != null) player = candidate.GetComponent<EnemyTarget>();
        }
        if (player == null || spawnPoints == null || spawnPoints.Length == 0 ||
            patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("EnemyPatrol: isi Player (EnemyTarget), Spawn Points, dan Patrol Points.", this);
            return;
        }
        if (player.IsCaptured) return;
        spawnWarningIssued = false;
        ScheduleNext();
    }

    public void StopEncounters()
    {
        if (!ready) return;
        bool wasPresent = IsPresent();
        HideEnemy();
        State = EnemyPatrolState.Stopped;
        if (wasPresent) onDespawned.Invoke();
    }

    [ContextMenu("Spawn On Next Tick")]
    public void SpawnNow()
    {
        if (!Application.isPlaying) return;
        if (State == EnemyPatrolState.Stopped) StartEncounters();
        if (State == EnemyPatrolState.Waiting) nextSpawnAt = Time.time;
    }

    private void ScheduleNext()
    {
        State = EnemyPatrolState.Waiting;
        nextSpawnAt = Time.time + RandomDuration(spawnDelay, 0.1f);
    }

    private static float RandomDuration(Vector2 range, float minimum)
    {
        float low = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
        float high = Mathf.Max(low, Mathf.Max(range.x, range.y));
        return Random.Range(low, high);
    }

    private bool IsPresent()
    {
        return State == EnemyPatrolState.Warning || State == EnemyPatrolState.Patrolling ||
            State == EnemyPatrolState.Chasing || State == EnemyPatrolState.Searching;
    }

    private void Update()
    {
        if (!ready) return;
        if (player == null)
        {
            if (IsPresent()) StopEncounters();
            return;
        }
        if (player.IsCaptured && State != EnemyPatrolState.Caught && State != EnemyPatrolState.Stopped)
        {
            StopEncounters();
            return;
        }
        if (Time.time < nextThinkAt) return;
        nextThinkAt = Time.time + Mathf.Max(0.05f, thinkInterval);
        Tick();
    }

    private void Tick()
    {
        if (State == EnemyPatrolState.Waiting)
        {
            if (Time.time >= nextSpawnAt && !TrySpawn()) nextSpawnAt = Time.time + 5f;
            return;
        }
        if (!IsPresent()) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            Despawn();
            return;
        }
        if (animatorHasSpeed && animator != null)
            animator.SetFloat(speedHash, agent.velocity.magnitude);

        if (State == EnemyPatrolState.Warning)
        {
            if (Time.time < warningEndsAt)
            {
                FollowPatrol();
                return;
            }
            visualRoot.SetActive(true);
            State = EnemyPatrolState.Patrolling;
        }

        bool visible = CanSeePlayer();
        if (visible)
        {
            lastSeenPosition = player.NavigationPosition;
            lastSeenAt = Time.time;
            if (State != EnemyPatrolState.Chasing) BeginChase();
        }
        if (!isActiveAndEnabled || !IsPresent()) return;
        if (State == EnemyPatrolState.Chasing)
        {
            if (visible && CanCatchPlayer())
            {
                CatchPlayer();
                return;
            }
            if (!visible && Time.time - lastSeenAt >= loseSightAfter)
            {
                BeginSearch();
                return;
            }
            RepathToLastSeen();
            CheckProgress();
            return;
        }
        if (State == EnemyPatrolState.Searching)
        {
            if (Time.time >= searchEndsAt)
            {
                if (Time.time >= lifeEndsAt) Despawn();
                else BeginPatrol();
                return;
            }
            CheckProgress();
            return;
        }
        if (Time.time >= lifeEndsAt) { Despawn(); return; }
        FollowPatrol();
    }

    private bool TrySample(Vector3 point, out Vector3 position)
    {
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (NavMesh.SamplePosition(point, out NavMeshHit hit,
            Mathf.Max(0.1f, navMeshSnapDistance), filter))
        {
            position = hit.position;
            return true;
        }
        position = point;
        return false;
    }

    private bool TrySpawn()
    {
        if (!player.isActiveAndEnabled || player.IsCaptured) return false;
        if (!TrySample(player.NavigationPosition, out Vector3 playerPosition))
        {
            WarnSpawn("Posisi Player belum dekat NavMesh. Cek hasil Bake dan Nav Mesh Snap Distance.");
            return false;
        }
        int first = Random.Range(0, spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform marker = spawnPoints[(first + i) % spawnPoints.Length];
            if (marker == null || !marker.gameObject.activeInHierarchy ||
                !TrySample(marker.position, out Vector3 spawn)) continue;
            float distance = Vector3.Distance(spawn, playerPosition);
            if (distance < minimumSpawnDistance || distance > maximumSpawnDistance) continue;
            if (avoidVisibleSpawn && VisibleFromCamera(spawn + Vector3.up * eyeHeight)) continue;
            NavMeshQueryFilter filter = new NavMeshQueryFilter
                { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.CalculatePath(spawn, playerPosition, filter, path) ||
                path.status != NavMeshPathStatus.PathComplete) continue;

            transform.position = spawn;
            agent.enabled = true;
            if (!agent.isOnNavMesh || !agent.Warp(spawn))
            {
                agent.enabled = false;
                continue;
            }
            agent.autoRepath = true;
            agent.updateRotation = true;
            agent.stoppingDistance = 0.1f;
            agent.speed = Mathf.Max(0.1f, patrolSpeed);
            agent.isStopped = false;
            State = warningDuration > 0f ? EnemyPatrolState.Warning : EnemyPatrolState.Patrolling;
            visualRoot.SetActive(warningDuration <= 0f);
            warningEndsAt = Time.time + warningDuration;
            lifeEndsAt = warningEndsAt + RandomDuration(encounterDuration, 1f);
            lastPatrolIndex = -1;
            nextWaypointAt = Time.time;
            ResetProgress();
            if (!PickPatrolPoint(true))
            {
                HideEnemy();
                State = EnemyPatrolState.Waiting;
                continue;
            }
            spawnWarningIssued = false;
            PlayLoop(patrolLoop, patrolVolume);
            if (appearanceSound != null) sound.PlayOneShot(appearanceSound);
            onSpawned.Invoke();
            return true;
        }
        WarnSpawn("Belum ada spawn aman yang terhubung ke Player. Cek NavMesh, jarak, dan titik spawn.");
        return false;
    }

    private void WarnSpawn(string message)
    {
        if (spawnWarningIssued) return;
        spawnWarningIssued = true;
        Debug.LogWarning("EnemyPatrol: " + message, this);
    }

    private bool VisibleFromCamera(Vector3 position)
    {
        Camera camera = playerCamera != null ? playerCamera : Camera.main;
        if (camera == null) return false;
        Vector3 viewport = camera.WorldToViewportPoint(position);
        return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
            viewport.y >= 0f && viewport.y <= 1f && !LineBlocked(camera.transform.position, position);
    }

    private bool PickPatrolPoint(bool preferNearPlayer)
    {
        int first = Random.Range(0, patrolPoints.Length);
        if (preferNearPlayer)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                float distance = (patrolPoints[i].position - player.NavigationPosition).sqrMagnitude;
                if (distance < nearest) { nearest = distance; first = i; }
            }
        }
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            int index = (first + i) % patrolPoints.Length;
            Transform marker = patrolPoints[index];
            if (marker == null || !marker.gameObject.activeInHierarchy ||
                (patrolPoints.Length > 1 && index == lastPatrolIndex) ||
                !TrySample(marker.position, out Vector3 destination)) continue;
            if ((destination - transform.position).sqrMagnitude <= arrivalDistance * arrivalDistance) continue;
            if (!agent.CalculatePath(destination, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            if (!agent.SetPath(path)) continue;
            agent.isStopped = false;
            lastPatrolIndex = index;
            ResetProgress();
            return true;
        }
        return false;
    }

    private void FollowPatrol()
    {
        if (agent.pathPending) return;
        if (agent.hasPath && agent.remainingDistance <= Mathf.Max(arrivalDistance, agent.stoppingDistance))
        {
            agent.ResetPath();
            nextWaypointAt = Time.time + RandomDuration(waypointPause, 0f);
        }
        if (!agent.hasPath && Time.time >= nextWaypointAt)
        {
            if (!PickPatrolPoint(false)) Despawn();
        }
        else CheckProgress();
    }

    private Vector3 EyePosition => eyes != null ? eyes.position : transform.position + Vector3.up * eyeHeight;

    private bool CanSeePlayer()
    {
        if (!player.isActiveAndEnabled || player.IsCaptured) return false;
        // Bersembunyi saat masih dilihat tidak langsung menghentikan pengejaran.
        if (player.IsHidden && State != EnemyPatrolState.Chasing) return false;
        Vector3 direction = player.SightPosition - EyePosition;
        if (direction.sqrMagnitude > sightDistance * sightDistance) return false;
        Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flat.sqrMagnitude > closeAwarenessDistance * closeAwarenessDistance &&
            Vector3.Angle(transform.forward, flat) > fieldOfView * 0.5f) return false;
        return !LineBlocked(EyePosition, player.SightPosition);
    }

    private bool LineBlocked(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < 0.0001f) return false;
        float distance = delta.magnitude;
        int count = Physics.RaycastNonAlloc(from, delta / distance, sightHits,
            distance, sightBlockingLayers, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length)
        {
            RaycastHit[] all = Physics.RaycastAll(from, delta / distance,
                distance, sightBlockingLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in all) if (BlocksSight(hit.collider)) return true;
        }
        else for (int i = 0; i < count; i++) if (BlocksSight(sightHits[i].collider)) return true;
        return false;
    }

    private bool BlocksSight(Collider candidate)
    {
        return candidate != null && !candidate.transform.IsChildOf(transform) &&
            (player == null || !candidate.transform.IsChildOf(player.transform));
    }

    private void BeginChase()
    {
        State = EnemyPatrolState.Chasing;
        visualRoot.SetActive(true);
        agent.speed = Mathf.Max(0.1f, chaseSpeed);
        nextRepathAt = 0f;
        ResetProgress();
        PlayLoop(chaseLoop != null ? chaseLoop : patrolLoop, chaseVolume);
        onChaseStarted.Invoke();
    }

    private void RepathToLastSeen()
    {
        if (Time.time < nextRepathAt) return;
        nextRepathAt = Time.time + Mathf.Max(0.1f, repathInterval);
        if (TrySample(lastSeenPosition, out Vector3 destination) &&
            agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.SetPath(path);
            agent.isStopped = false;
        }
        else if (State == EnemyPatrolState.Chasing && Time.time - lastProgressAt >= stuckTimeout)
            BeginSearch();
    }

    private void BeginSearch()
    {
        State = EnemyPatrolState.Searching;
        searchEndsAt = Time.time + Mathf.Max(0.1f, searchDuration);
        nextRepathAt = 0f;
        RepathToLastSeen();
        ResetProgress();
        onPlayerLost.Invoke();
    }

    private void BeginPatrol()
    {
        State = EnemyPatrolState.Patrolling;
        agent.speed = Mathf.Max(0.1f, patrolSpeed);
        agent.ResetPath();
        nextWaypointAt = Time.time;
        PlayLoop(patrolLoop, patrolVolume);
        ResetProgress();
    }

    private bool CanCatchPlayer()
    {
        Vector3 difference = player.NavigationPosition - transform.position;
        if (Mathf.Abs(difference.y) > maximumCatchHeightDifference) return false;
        difference.y = 0f;
        return difference.sqrMagnitude <= catchDistance * catchDistance &&
            !LineBlocked(EyePosition, player.SightPosition);
    }

    private void CatchPlayer()
    {
        if (!player.TryCapture()) return;
        State = EnemyPatrolState.Caught;
        agent.isStopped = true;
        agent.ResetPath();
        sound.Stop();
        visualRoot.SetActive(false);
        onPlayerCaught.Invoke();
    }

    private void ResetProgress()
    {
        lastProgressAt = Time.time;
        lastProgressPosition = transform.position;
    }

    private void CheckProgress()
    {
        if (!agent.hasPath || agent.pathPending || agent.remainingDistance <= arrivalDistance) return;
        if ((transform.position - lastProgressPosition).sqrMagnitude >= 0.04f)
        {
            ResetProgress();
            return;
        }
        if (Time.time - lastProgressAt < stuckTimeout) return;
        agent.ResetPath();
        ResetProgress();
        if (State == EnemyPatrolState.Chasing) BeginSearch();
        else if (State == EnemyPatrolState.Searching) searchEndsAt = Mathf.Min(searchEndsAt, Time.time + 1f);
        else nextWaypointAt = Time.time;
    }

    private void PlayLoop(AudioClip clip, float volume)
    {
        sound.volume = volume;
        if (clip == null) { sound.Stop(); return; }
        if (sound.clip == clip && sound.isPlaying) return;
        sound.clip = clip;
        sound.loop = true;
        sound.Play();
    }

    private void Despawn()
    {
        HideEnemy();
        ScheduleNext();
        onDespawned.Invoke();
    }

    private void HideEnemy()
    {
        if (agent != null)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.enabled = false;
        }
        if (sound != null) sound.Stop();
        if (visualRoot != null && visualRoot != gameObject) visualRoot.SetActive(false);
    }

    private void OnDisable()
    {
        HideEnemy();
        State = EnemyPatrolState.Stopped;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = EyePosition;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, sightDistance);
        Gizmos.DrawRay(origin, Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up) * transform.forward * sightDistance);
        Gizmos.DrawRay(origin, Quaternion.AngleAxis(fieldOfView * 0.5f, Vector3.up) * transform.forward * sightDistance);
    }
}
