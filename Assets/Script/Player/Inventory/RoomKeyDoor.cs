using UnityEngine;
using UnityEngine.Events;

public class RoomKeyDoor : MonoBehaviour
{
    [Header("Required Item")]
    [SerializeField] private string requiredItemId =
        "key_room_1";

    [SerializeField] private bool consumeKey = false;

    [Header("Lock State")]
    [SerializeField] private bool isUnlocked = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlocked =
        new UnityEvent();

    [SerializeField] private UnityEvent onMissingKey =
        new UnityEvent();

    [Tooltip("Dipanggil setiap pintu yang sudah terbuka digunakan.")]
    [SerializeField] private UnityEvent onOpenRequested =
        new UnityEvent();

    private bool processingInteraction;

    public bool IsUnlocked => isUnlocked;

    public void Interact(RoomInventory inventory)
    {
        if (!isActiveAndEnabled || processingInteraction)
        {
            return;
        }

        if (inventory == null)
        {
            Debug.LogError(
                "Inventory player tidak tersedia.",
                this
            );

            return;
        }

        processingInteraction = true;

        try
        {
            if (isUnlocked)
            {
                inventory.ShowMessage(
                    "Pintu sudah terbuka."
                );

                onOpenRequested.Invoke();
                return;
            }

            if (string.IsNullOrWhiteSpace(requiredItemId))
            {
                Debug.LogError(
                    "Required Item ID pada pintu belum diisi.",
                    this
                );

                return;
            }

            if (!inventory.HasItem(requiredItemId))
            {
                inventory.ShowMessage(
                    "Pintu terkunci. Kamu belum memiliki kuncinya."
                );

                onMissingKey.Invoke();
                return;
            }

            if (consumeKey &&
                !inventory.TryRemoveItem(requiredItemId))
            {
                return;
            }

            // Perbarui status sebelum menjalankan event.
            isUnlocked = true;

            inventory.ShowMessage(
                "Pintu telah dibuka"
            );

            onUnlocked.Invoke();
            onOpenRequested.Invoke();
        }
        finally
        {
            processingInteraction = false;
        }
    }
}