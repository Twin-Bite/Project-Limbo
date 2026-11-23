using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoomInventoryMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private RoomInventory inventory;
    [SerializeField] private UIToggleController pauseController;

    [Header("Pages")]
    [SerializeField] private GameObject pausePage;
    [SerializeField] private GameObject inventoryPage;

    [Header("Inventory List")]
    [SerializeField] private ScrollRect itemList;
    [SerializeField] private Button itemButtonTemplate;
    [SerializeField] private TMP_Text emptyLabel;

    [Header("Selected Item")]
    [SerializeField] private TMP_Text selectedItemLabel;
    [SerializeField] private TMP_Text useStatusLabel;
    [SerializeField] private Button useButton;

    private sealed class ItemRow
    {
        public Button Button;
        public TMP_Text Label;
        public ColorBlock DefaultColors;
        public string ItemId;
    }

    private readonly List<ItemRow> rows = new List<ItemRow>();

    private bool ready;
    private bool viewingInventory;
    private string selectedItemId;

    private void Awake()
    {
        ready = inventory != null &&
            pauseController != null &&
            pausePage != null &&
            inventoryPage != null &&
            itemList != null &&
            itemList.content != null &&
            itemButtonTemplate != null &&
            itemButtonTemplate.GetComponentInChildren<TMP_Text>(true) != null &&
            emptyLabel != null &&
            selectedItemLabel != null &&
            useStatusLabel != null &&
            useButton != null;

        if (!ready)
        {
            Debug.LogError(
                "[RoomInventoryMenu] Ada referensi inventory atau UI yang belum diisi.",
                this
            );

            enabled = false;
            return;
        }

        itemButtonTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (!ready)
            return;

        inventory.ItemAdded?.AddListener(OnInventoryChanged);
        inventory.ItemRemoved?.AddListener(OnInventoryChanged);

        ShowPausePage();
    }

    private void OnDisable()
    {
        if (inventory == null)
            return;

        inventory.ItemAdded?.RemoveListener(OnInventoryChanged);
        inventory.ItemRemoved?.RemoveListener(OnInventoryChanged);
    }

    public void ShowPausePage()
    {
        if (!ready)
            return;

        viewingInventory = false;
        selectedItemId = null;

        inventoryPage.SetActive(false);
        pausePage.SetActive(true);
    }

    public void ShowInventoryPage()
    {
        if (!ready || !pauseController.IsVisible)
            return;

        viewingInventory = true;
        selectedItemId = null;

        pausePage.SetActive(false);
        inventoryPage.SetActive(true);

        RefreshItems();

        Canvas.ForceUpdateCanvases();
        itemList.verticalNormalizedPosition = 1f;
    }

    public void UseSelectedItem()
    {
        if (!viewingInventory || !pauseController.IsVisible)
            return;

        RoomItemUseZone zone = RoomItemUseZone.FindAvailable(
            inventory,
            selectedItemId
        );

        if (zone != null && zone.TryUse(
            inventory,
            selectedItemId,
            pauseController.HideUIImmediately
        ))
        {
            return;
        }

        RefreshSelection();
    }

    private void OnInventoryChanged(string itemId)
    {
        if (viewingInventory)
            RefreshItems();
    }

    private void RefreshItems()
    {
        int visibleRows = 0;

        for (int slot = 0; slot < inventory.SlotCount; slot++)
        {
            string itemId = inventory.GetItemId(slot);

            if (string.IsNullOrWhiteSpace(itemId))
                continue;

            ItemRow row = GetOrCreateRow(visibleRows++);
            row.ItemId = itemId;
            row.Label.text = inventory.GetItemName(itemId);
            row.Button.gameObject.SetActive(true);
        }

        for (int i = visibleRows; i < rows.Count; i++)
            rows[i].Button.gameObject.SetActive(false);

        emptyLabel.gameObject.SetActive(visibleRows == 0);

        if (!inventory.HasItem(selectedItemId))
            selectedItemId = null;

        RefreshSelection();
    }

    private ItemRow GetOrCreateRow(int index)
    {
        if (index < rows.Count)
            return rows[index];

        Button button = Instantiate(itemButtonTemplate, itemList.content);
        button.name = "Key Item " + (index + 1);

        var row = new ItemRow
        {
            Button = button,
            Label = button.GetComponentInChildren<TMP_Text>(true),
            DefaultColors = button.colors
        };

        button.onClick.AddListener(() => SelectItem(row.ItemId));

        rows.Add(row);
        return row;
    }

    private void SelectItem(string itemId)
    {
        if (!viewingInventory || !inventory.HasItem(itemId))
            return;

        selectedItemId = itemId;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        bool hasSelection = inventory.HasItem(selectedItemId);

        foreach (ItemRow row in rows)
        {
            ColorBlock colors = row.DefaultColors;

            if (hasSelection && string.Equals(
                row.ItemId,
                selectedItemId,
                StringComparison.Ordinal
            ))
            {
                colors.normalColor = colors.selectedColor;
            }

            row.Button.colors = colors;
        }

        selectedItemLabel.gameObject.SetActive(hasSelection);
        useStatusLabel.gameObject.SetActive(hasSelection);
        useButton.gameObject.SetActive(hasSelection);

        if (!hasSelection)
            return;

        selectedItemLabel.text = inventory.GetItemName(selectedItemId);

        bool canUse = RoomItemUseZone.FindAvailable(
            inventory,
            selectedItemId
        ) != null;

        useButton.interactable = canUse;

        useStatusLabel.text = canUse
            ? string.Empty
            : "Move into the area that needs this item.";
    }
}