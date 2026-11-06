using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class RoomPlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoomInventory inventory;
    [SerializeField] private Transform interactionPoint;
    [Header("Interaction")]
    [Min(0.1f)] [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    private InputAction interactAction;
    private bool interactionEnabled = true;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<RoomInventory>();
        if (inventory == null)
        {
            Debug.LogError("RoomInventory belum dipasang pada Player.", this);
            enabled = false;
            return;
        }
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
    }

    private void OnEnable()
    {
        if (interactAction == null) return;
        interactAction.performed += OnInteractPerformed;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        if (interactAction == null) return;
        interactAction.performed -= OnInteractPerformed;
        interactAction.Disable();
    }

    private void OnDestroy() { interactAction?.Dispose(); }
    private void OnInteractPerformed(InputAction.CallbackContext context) { TryInteract(); }
    public void SetInteractionEnabled(bool value) { interactionEnabled = value; }

    private Vector3 GetInteractionCenter()
    {
        if (interactionPoint != null) return interactionPoint.position;
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null && controller.enabled) return controller.bounds.center;
        return transform.position + Vector3.up;
    }

    public void TryInteract()
    {
        if (!isActiveAndEnabled || !interactionEnabled || inventory == null) return;
        Vector3 center = GetInteractionCenter();
        Collider[] nearby = Physics.OverlapSphere(
            center, interactionDistance, interactionLayers, QueryTriggerInteraction.Collide);

        RoomPickup nearestPickup = null;
        RoomKeyDoor nearestDoor = null;
        RoomLockInspector nearestInspector = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (Collider candidate in nearby)
        {
            if (candidate.transform.IsChildOf(transform)) continue;
            RoomPickup pickup = candidate.GetComponentInParent<RoomPickup>();
            RoomKeyDoor door = candidate.GetComponentInParent<RoomKeyDoor>();
            RoomLockInspector inspector = candidate.GetComponentInParent<RoomLockInspector>();
            bool canPickUp = pickup != null && pickup.isActiveAndEnabled;
            bool canUseDoor = door != null && door.isActiveAndEnabled;
            bool canInspect = inspector != null && inspector.CanInspect;
            if (!canPickUp && !canUseDoor && !canInspect) continue;

            float distance = (candidate.ClosestPoint(center) - center).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearestInspector = canInspect ? inspector : null;
            nearestPickup = !canInspect && canPickUp ? pickup : null;
            nearestDoor = !canInspect && !canPickUp ? door : null;
        }

        if (nearestInspector != null) nearestInspector.Interact(this);
        else if (nearestPickup != null) nearestPickup.Interact(inventory);
        else if (nearestDoor != null) nearestDoor.Interact(inventory);
        else inventory.ShowMessage("Tidak ada objek interaksi dalam jangkauan.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetInteractionCenter(), interactionDistance);
    }
}
