using System;
using UnityEngine;
using UnityEngine.Events;

public class RoomKeyDoor : MonoBehaviour
{
    [Header("Required Item")]
    [SerializeField] private string requiredItemId = "key_room_1";
    [SerializeField] private bool consumeKey;

    [Tooltip("Saat terkunci, gunakan kunci dari Inventory di zona pintu.")]
    [SerializeField] private bool useFromInventoryMenu;

    [Header("Lock State")]
    [SerializeField] private bool isUnlocked;

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();
    [SerializeField] private UnityEvent onMissingKey = new UnityEvent();
    [SerializeField] private UnityEvent onOpenRequested = new UnityEvent();

    private bool processingInteraction;

    public bool IsUnlocked => isUnlocked;
    public string RequiredItemId => requiredItemId;

    public void Interact(RoomInventory inventory)
    {
        if (!isActiveAndEnabled || processingInteraction || inventory == null)
            return;

        processingInteraction = true;

        try
        {
            if (isUnlocked)
            {
                inventory.ShowMessage("Pintu sudah terbuka.");
                onOpenRequested?.Invoke();
                return;
            }

            if (string.IsNullOrWhiteSpace(requiredItemId))
            {
                Debug.LogError("Required Item ID pada pintu belum diisi.", this);
                return;
            }

            if (!inventory.HasItem(requiredItemId))
            {
                inventory.ShowMessage(
                    "Pintu terkunci. Kamu belum memiliki kuncinya."
                );

                onMissingKey?.Invoke();
                return;
            }

            if (useFromInventoryMenu)
            {
                inventory.ShowMessage(
                    "Pilih kunci di Inventory dan tekan Use di dekat pintu."
                );

                return;
            }

            Unlock(inventory, null);
        }
        finally
        {
            processingInteraction = false;
        }
    }

    internal bool CanUseKey(RoomInventory inventory, string selectedItemId)
    {
        return isActiveAndEnabled &&
            !processingInteraction &&
            !isUnlocked &&
            inventory != null &&
            inventory.HasItem(selectedItemId) &&
            string.Equals(
                requiredItemId,
                selectedItemId,
                StringComparison.Ordinal
            );
    }

    internal bool TryUseKey(
        RoomItemUseZone zone,
        RoomInventory inventory,
        string selectedItemId,
        Action beforeOpening
    )
    {
        if (zone == null ||
            zone.KeyDoor != this ||
            !zone.Contains(inventory) ||
            !CanUseKey(inventory, selectedItemId))
        {
            return false;
        }

        processingInteraction = true;

        try
        {
            return Unlock(inventory, beforeOpening);
        }
        finally
        {
            processingInteraction = false;
        }
    }

    private bool Unlock(RoomInventory inventory, Action beforeOpening)
    {
        if (consumeKey && !inventory.TryRemoveItem(requiredItemId))
            return false;

        isUnlocked = true;

        beforeOpening?.Invoke();

        inventory.ShowMessage("Pintu telah dibuka.");
        onUnlocked?.Invoke();
        onOpenRequested?.Invoke();

        return true;
    }
}