using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class RoomInventoryMenuSetup
{
    private const string ScenePath = "Assets/Scenes/Day 1.unity";
    private const string UndoLabel = "Connect Room Inventory Menu";

    [MenuItem("Tools/Project Limbo/Connect Room Inventory Menu")]
    private static void Connect()
    {
        var scene = GetDayOneScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Connect the menu in Edit Mode.");

        var controllerObject = Root(scene, "UI Controller");
        var player = Root(scene, "Nayla");
        var controller = Required<UIToggleController>(controllerObject);
        var menu = Required<RoomInventoryMenu>(controllerObject);
        var inventory = Required<RoomInventory>(player);
        var pauseMenu = new SerializedObject(controller)
            .FindProperty("uiTarget").objectReferenceValue as GameObject;
        if (pauseMenu == null || pauseMenu.scene != scene || pauseMenu.name != "Pause Menu")
            throw new InvalidOperationException("UI Controller is not targeting Day 1's Pause Menu.");

        var pausePage = Child(pauseMenu, "Image");
        var inventoryPage = Child(pauseMenu, "GameObject");
        var list = Required<ScrollRect>(Child(inventoryPage, "Scroll View"));
        if (list.content == null || !list.content.IsChildOf(list.transform))
            throw new InvalidOperationException("Inventory Scroll View needs a Content child.");
        var openInventory = FindButton(pausePage, "INVENTORY");
        var backFromInventory = FindButton(inventoryPage, "Back");
        var use = FindButton(inventoryPage, "USE");
        var detailPanel = (RectTransform)use.transform.parent;
        var rowButtons = list.content.GetComponentsInChildren<Button>(true)
            .Where(button => button.transform.parent == list.content)
            .ToArray();
        var template = rowButtons.FirstOrDefault(button => button.name == "ItemButtonTemplate")
            ?? rowButtons.FirstOrDefault(button => HasButtonText(button, "KEY ITEM"));
        if (template == null || template.GetComponentInChildren<TMP_Text>(true) == null)
            throw new InvalidOperationException("No suitable inventory button template.");
        if (template.onClick.GetPersistentEventCount() != 0)
            throw new InvalidOperationException("The item template has pre-existing click events.");

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        try
        {
            Undo.RecordObject(template.gameObject, UndoLabel);
            template.name = "ItemButtonTemplate";
            foreach (var row in rowButtons)
            {
                if (row == template || HasButtonText(row, "KEY ITEM"))
                {
                    Undo.RecordObject(row.gameObject, UndoLabel);
                    row.gameObject.SetActive(false);
                }
            }

            var uiFont = use.GetComponentInChildren<TMP_Text>(true).font;
            var listRect = (RectTransform)list.transform;
            var empty = Label((RectTransform)inventoryPage.transform, "EmptyLabel",
                "Inventory kosong", uiFont, 36f, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                listRect.anchoredPosition, new Vector2(780f, 72f), true);
            var selected = Label(detailPanel, "SelectedItemLabel",
                string.Empty, uiFont, 34f, new Vector2(0f, 1f),
                Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -18f),
                new Vector2(-32f, 72f), false);
            var status = Label(detailPanel, "UseStatusLabel",
                string.Empty, uiFont, 25f, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0.5f, 1f), new Vector2(0f, -114f),
                new Vector2(-32f, 92f), false);

            var serializedMenu = new SerializedObject(menu);
            SetReference(serializedMenu, "inventory", inventory);
            SetReference(serializedMenu, "pauseController", controller);
            SetReference(serializedMenu, "pausePage", pausePage);
            SetReference(serializedMenu, "inventoryPage", inventoryPage);
            SetReference(serializedMenu, "itemList", list);
            SetReference(serializedMenu, "itemButtonTemplate", template);
            SetReference(serializedMenu, "emptyLabel", empty);
            SetReference(serializedMenu, "selectedItemLabel", selected);
            SetReference(serializedMenu, "useStatusLabel", status);
            SetReference(serializedMenu, "useButton", use);
            serializedMenu.ApplyModifiedProperties();

            ConnectEvent(openInventory, openInventory.onClick, menu.ShowInventoryPage,
                pausePage, inventoryPage);
            ConnectEvent(backFromInventory, backFromInventory.onClick, menu.ShowPausePage,
                pausePage, inventoryPage);
            ConnectEvent(use, use.onClick, menu.UseSelectedItem, pausePage, inventoryPage);
            ConnectEvent(controller, controller.onShow, menu.ShowPausePage, pausePage, inventoryPage);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = controllerObject;
            Debug.Log("[Room Inventory] Labels, item template, references and buttons connected.", menu);
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
    }

    [MenuItem("Tools/Project Limbo/Verify Inventory Pickup")]
    private static void VerifyPickup()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Verify the pickup in Play Mode.");

        var scene = GetDayOneScene();
        var player = Root(scene, "Nayla");
        var inventory = Required<RoomInventory>(player);
        var controller = Required<UIToggleController>(Root(scene, "UI Controller"));
        var menu = Required<RoomInventoryMenu>(controller.gameObject);
        var configuration = new SerializedObject(menu);
        var pausePage = (GameObject)configuration.FindProperty("pausePage").objectReferenceValue;
        var inventoryPage = (GameObject)configuration.FindProperty("inventoryPage").objectReferenceValue;
        var list = (ScrollRect)configuration.FindProperty("itemList").objectReferenceValue;
        var empty = (TMP_Text)configuration.FindProperty("emptyLabel").objectReferenceValue;
        var selected = (TMP_Text)configuration.FindProperty("selectedItemLabel").objectReferenceValue;
        var use = (Button)configuration.FindProperty("useButton").objectReferenceValue;
        const string expectedId = "vas_ruang_tamu";

        controller.ShowUI();
        FindButton(pausePage, "INVENTORY").onClick.Invoke();
        if (!inventoryPage.activeInHierarchy || pausePage.activeSelf)
            throw new InvalidOperationException("Inventory button did not switch pages.");

        if (!inventory.HasItem(expectedId))
        {
            if (!inventory.IsEmptyForSetupVerification() || !empty.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Start a fresh Play Mode for the empty-inventory check.");

            FindButton(inventoryPage, "Back").onClick.Invoke();
            if (inventoryPage.activeSelf || !pausePage.activeInHierarchy)
                throw new InvalidOperationException("Back button did not return to pause.");
            controller.HideUIImmediately();

            var vase = UnityEngine.Object.FindObjectsByType<RoomPickup>(
                FindObjectsInactive.Exclude)
                .Single(pickup => pickup.gameObject.scene == scene &&
                    (new SerializedObject(pickup).FindProperty("itemId").stringValue == expectedId));
            vase.Interact(inventory);
            if (!inventory.HasItem(expectedId) || vase.gameObject.activeSelf)
                throw new InvalidOperationException("Pickup did not add or hide the vase.");

            controller.ShowUI();
            FindButton(pausePage, "INVENTORY").onClick.Invoke();
        }

        string itemName = inventory.GetItemName(expectedId);
        var visibleButtons = list.content.GetComponentsInChildren<Button>(false);
        var item = visibleButtons.Single(button => HasButtonText(button, itemName));
        if (empty.gameObject.activeInHierarchy)
            throw new InvalidOperationException("Empty label is visible with an inventory item.");
        item.onClick.Invoke();
        if (!selected.gameObject.activeInHierarchy || selected.text != itemName ||
            !use.gameObject.activeInHierarchy || use.interactable)
            throw new InvalidOperationException("Inventory selection or Use state is incorrect.");
        Debug.Log("[Room Inventory] Pickup, rendered item and selection verified: " + itemName, menu);
    }

    private static bool IsEmptyForSetupVerification(this RoomInventory inventory)
    {
        for (var slot = 0; slot < inventory.SlotCount; slot++)
        {
            if (!string.IsNullOrWhiteSpace(inventory.GetItemId(slot)))
                return false;
        }
        return true;
    }

    private static Scene GetDayOneScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Make Day 1 the active scene.");
        return scene;
    }

    private static GameObject Root(Scene scene, string name) =>
        scene.GetRootGameObjects().Single(gameObject => gameObject.name == name);

    private static GameObject Child(GameObject parent, string name)
    {
        var child = parent.transform.Find(name);
        if (child == null)
            throw new InvalidOperationException(parent.name + " needs a child named " + name);
        return child.gameObject;
    }

    private static T Required<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent<T>(out var component))
            return component;
        throw new InvalidOperationException(gameObject.name + " needs " + typeof(T).Name);
    }

    private static bool HasButtonText(Button button, string text) =>
        string.Equals(button.GetComponentInChildren<TMP_Text>(true)?.text.Trim(),
            text, StringComparison.OrdinalIgnoreCase);

    private static Button FindButton(GameObject page, string caption) =>
        page.GetComponentsInChildren<Button>(true).Single(button => HasButtonText(button, caption));

    private static TextMeshProUGUI Label(RectTransform parent, string name, string label,
        TMP_FontAsset font, float fontSize, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 position, Vector2 size, bool active)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return Required<TextMeshProUGUI>(existing.gameObject);

        var child = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, UndoLabel);
        Undo.SetTransformParent(child.transform, parent, UndoLabel);
        Undo.RecordObject(child, UndoLabel);
        child.layer = parent.gameObject.layer;
        var rect = (RectTransform)child.transform;
        Undo.RecordObject(rect, UndoLabel);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition3D = new Vector3(position.x, position.y, 0f);
        rect.sizeDelta = size;

        var text = Undo.AddComponent<TextMeshProUGUI>(child);
        Undo.RecordObject(text, UndoLabel);
        text.font = font;
        text.fontSize = fontSize;
        text.color = new Color32(242, 239, 235, 255);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = label;
        child.SetActive(active);
        return text;
    }

    private static void SetReference(SerializedObject serialized, string field, UnityEngine.Object value)
    {
        var property = serialized.FindProperty(field);
        if (property == null)
            throw new InvalidOperationException("Missing inventory menu field " + field);
        property.objectReferenceValue = value;
    }

    private static void ConnectEvent(Component owner, UnityEvent events, UnityAction callback,
        GameObject pausePage, GameObject inventoryPage)
    {
        Undo.RecordObject(owner, UndoLabel);
        bool hasCallback = false;
        for (int i = events.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            var target = events.GetPersistentTarget(i);
            var method = events.GetPersistentMethodName(i);
            bool isPageToggle = (target == pausePage || target == inventoryPage) &&
                method == nameof(GameObject.SetActive);
            if (isPageToggle)
                UnityEventTools.RemovePersistentListener(events, i);
            else if (target == (UnityEngine.Object)callback.Target && method == callback.Method.Name)
                hasCallback = true;
        }
        if (!hasCallback)
            UnityEventTools.AddPersistentListener(events, callback);
        EditorUtility.SetDirty(owner);
    }
}
