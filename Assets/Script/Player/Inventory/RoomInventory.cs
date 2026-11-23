using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

public class RoomInventory : MonoBehaviour
{
    [Header("Inventory Slots")]
    [SerializeField] private string[] items = new string[10];

    [Header("Events")]
    [SerializeField] private UnityEvent<string> onItemAdded =
        new UnityEvent<string>();

    [SerializeField] private UnityEvent<string> onItemRemoved =
        new UnityEvent<string>();

    [SerializeField] private UnityEvent<string> onMessage =
        new UnityEvent<string>();

    private readonly Dictionary<string, string> itemNames =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private CharacterController characterController;

    public int SlotCount => items?.Length ?? 0;
    public UnityEvent<string> ItemAdded => onItemAdded;
    public UnityEvent<string> ItemRemoved => onItemRemoved;

    internal Vector3 UsePosition => characterController != null
        ? characterController.transform.TransformPoint(
            characterController.center
        )
        : transform.position;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public string GetItemId(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount
            ? items[slotIndex]
            : null;
    }

    public string GetItemName(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        if (itemNames.TryGetValue(itemId, out string displayName))
            return displayName;

        string readableId = itemId.Replace('_', ' ').Replace('-', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(readableId);
    }

    public bool HasItem(string itemId)
    {
        return FindItemSlot(itemId) >= 0;
    }

    public bool TryAddItem(string itemId)
    {
        return TryAddItem(itemId, null);
    }

    public bool TryAddItem(string itemId, string displayName)
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

        for (int i = 0; i < SlotCount; i++)
        {
            if (!string.IsNullOrWhiteSpace(items[i]))
                continue;

            items[i] = itemId;

            if (!string.IsNullOrWhiteSpace(displayName))
                itemNames[itemId] = displayName.Trim();

            onItemAdded?.Invoke(itemId);
            return true;
        }

        ShowMessage("Inventory penuh.");
        return false;
    }

    public bool TryRemoveItem(string itemId)
    {
        int slotIndex = FindItemSlot(itemId);

        if (slotIndex < 0)
            return false;

        items[slotIndex] = string.Empty;
        itemNames.Remove(itemId);

        onItemRemoved?.Invoke(itemId);
        return true;
    }

    public void ShowMessage(string message)
    {
        Debug.Log(message, this);
        onMessage?.Invoke(message);
    }

    private int FindItemSlot(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return -1;

        for (int i = 0; i < SlotCount; i++)
        {
            if (string.Equals(items[i], itemId, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }
}