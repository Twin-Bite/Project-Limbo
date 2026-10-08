using UnityEngine;
using UnityEngine.Events;

public class RoomInventory : MonoBehaviour
{
    [Header("Inventory Slots")]
    [SerializeField] private string[] items = new string[10];

    [Header("Events")]
    [SerializeField] private UnityEvent<string> onItemAdded = new UnityEvent<string>();
    [SerializeField] private UnityEvent<string> onItemRemoved = new UnityEvent<string>();
    [SerializeField] private UnityEvent<string> onMessage = new UnityEvent<string>();

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || items == null)
        {
            return false;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryAddItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogError("Item ID belum diisi.", this);
            return false;
        }

        if (HasItem(itemId))
        {
            ShowMessage("Item ini sudah ada di inventory.");
            return false;
        }

        if (items != null)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(items[i]))
                {
                    items[i] = itemId;
                    onItemAdded.Invoke(itemId);

                    return true;
                }
            }
        }

        ShowMessage("Inventory penuh.");
        return false;
    }

    public bool TryRemoveItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || items == null)
        {
            return false;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == itemId)
            {
                items[i] = string.Empty;
                onItemRemoved.Invoke(itemId);

                return true;
            }
        }

        return false;
    }

    public void ShowMessage(string message)
    {
        Debug.Log(message, this);
        onMessage.Invoke(message);
    }
}
