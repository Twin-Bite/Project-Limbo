using UnityEngine;
using UnityEngine.Events;

public class RoomPickup : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private string itemId = "key_room_1";

    [SerializeField] private string itemName =
        "Kunci Ruangan 1";

    [Header("Events")]
    [SerializeField] private UnityEvent onPickedUp =
        new UnityEvent();

    private bool pickedUp;

    public void Interact(RoomInventory inventory)
    {
        if (!isActiveAndEnabled || pickedUp)
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

        // Cegah pengambilan berulang saat event berjalan.
        pickedUp = true;

        if (!inventory.TryAddItem(itemId))
        {
            pickedUp = false;
            return;
        }

        inventory.ShowMessage(
            "Item diambil: " + itemName
        );

        onPickedUp.Invoke();

        gameObject.SetActive(false);
    }
}