using UnityEngine;
using UnityEngine.Events;

public class RoomDoor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoomManager roomManager;

    [Tooltip("Isi dengan Transform utama Player.")]
    [SerializeField] private Transform playerRoot;

    [Header("Door Settings")]
    [Range(0, 5)]
    [SerializeField] private int roomIndex;

    [Tooltip("Player harus berada di dalam trigger untuk memakai pintu.")]
    [SerializeField] private bool requirePlayerInRange = true;

    [Tooltip("Langsung gunakan pintu saat player masuk trigger.")]
    [SerializeField] private bool automaticOnEnter = false;

    [Header("Door Events")]
    [SerializeField] private UnityEvent onPlayerEnteredRange =
        new UnityEvent();

    [SerializeField] private UnityEvent onPlayerLeftRange =
        new UnityEvent();

    [SerializeField] private UnityEvent onWrongRoom =
        new UnityEvent();

    
    private int playerCollidersInRange;
    public bool PlayerInRange => playerCollidersInRange > 0;

    public void Interact()
    {
        if (!isActiveEnabled)
        {
            return;
        }

        if (roomManager == null)
        {
            Debug.LogError(
                "RoomManager belum diisi",
                this
            );

            return;
        }

        if (requirePlayerInRange && !PlayerInRange)
        {
            return;
        }

        if (roomManager.CurrentRoomIndex != roomIndex)
        {
            onWrongRoom.Invoke();
            return;
        }

        if (roomManager.IsTransitioning)
        {
            return;
        }

        roomManager.UseExitFrom(roomIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (roomManager == null ||
            roomManager.CurrentRoomIndex != roomIndex)
        {
            return;
        }

        playerCollidersInRange++;

        // Event cukup dipanggil saat collider pertama masuk.
        if (playerCollidersInRange != 1)
        {
            return;
        }

        onPlayerEnteredRange.Invoke();

        if (automaticOnEnter)
        {
            Interact();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other) ||
            playerCollidersInRange <= 0)
        {
            return;
        }

        playerCollidersInRange--;

        // Event dipanggil setelah semua collider player keluar.
        if (playerCollidersInRange == 0)
        {
            onPlayerLeftRange.Invoke();
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (playerRoot == null)
        {
            return false;
        }

        return other.transform == playerRoot ||
               other.transform.IsChildOf(playerRoot);
    }

    private void OnDisable()
    {
        playerCollidersInRange = 0;
    }
}
