using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class RoomItemUseZone : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Jika diisi, ID dan Consume Key mengikuti pintu ini.")]
    [SerializeField] private RoomKeyDoor keyDoor;

    [Header("Custom Target")]
    [SerializeField] private string requiredItemId = "key_room_1";
    [SerializeField] private bool consumeItem;
    [SerializeField] private bool useOnce = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onItemUsed = new UnityEvent();

    private static readonly List<RoomItemUseZone> activeZones =
        new List<RoomItemUseZone>();

    private BoxCollider area;
    private bool isUsing;
    private bool used;

    internal RoomKeyDoor KeyDoor => keyDoor;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ClearActiveZones()
    {
        activeZones.Clear();
    }

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        area = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        activeZones.Add(this);
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    public static RoomItemUseZone FindAvailable(
        RoomInventory inventory,
        string itemId
    )
    {
        if (inventory == null || !inventory.HasItem(itemId))
            return null;

        Vector3 playerPosition = inventory.UsePosition;
        RoomItemUseZone nearestZone = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (RoomItemUseZone zone in activeZones)
        {
            if (zone == null || !zone.CanUse(inventory, itemId))
                continue;

            Vector3 center = zone.area.transform.TransformPoint(
                zone.area.center
            );

            float distance = (center - playerPosition).sqrMagnitude;

            if (distance >= nearestDistance)
                continue;

            nearestZone = zone;
            nearestDistance = distance;
        }

        return nearestZone;
    }

    public bool TryUse(
        RoomInventory inventory,
        string itemId,
        Action beforeAction = null
    )
    {
        if (!CanUse(inventory, itemId))
            return false;

        isUsing = true;

        try
        {
            if (keyDoor != null)
            {
                if (!keyDoor.TryUseKey(this, inventory, itemId, beforeAction))
                    return false;
            }
            else
            {
                if (consumeItem && !inventory.TryRemoveItem(itemId))
                    return false;

                used = true;
                beforeAction?.Invoke();
            }

            onItemUsed?.Invoke();
            return true;
        }
        finally
        {
            isUsing = false;
        }
    }

    internal bool Contains(RoomInventory inventory)
    {
        if (!isActiveAndEnabled ||
            area == null ||
            !area.enabled ||
            !area.isTrigger ||
            inventory == null ||
            !inventory.isActiveAndEnabled)
        {
            return false;
        }

        Vector3 localPoint = area.transform.InverseTransformPoint(
            inventory.UsePosition
        ) - area.center;

        Vector3 halfSize = area.size * 0.5f;

        return Mathf.Abs(localPoint.x) <= halfSize.x &&
            Mathf.Abs(localPoint.y) <= halfSize.y &&
            Mathf.Abs(localPoint.z) <= halfSize.z;
    }

    private bool CanUse(RoomInventory inventory, string itemId)
    {
        if (isUsing || !Contains(inventory))
            return false;

        if (keyDoor != null)
            return keyDoor.CanUseKey(inventory, itemId);

        return (!useOnce || !used) &&
            inventory.HasItem(itemId) &&
            string.Equals(requiredItemId, itemId, StringComparison.Ordinal);
    }
}