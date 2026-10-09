using UnityEngine;
using UnityEngine.InputSystem;

public class RoomPlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoomInventory inventory;
    [SerializeField] private Camera playerCamera;

    [Header("Interaction")]
    [Min(0.1f)]
    [SerializeField] private float interactionDistance = 2.5f;

    [SerializeField] private LayerMask interactionLayers = ~0;

    private InputAction interactAction;
    private bool interactionEnabled = true;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<RoomInventory>();
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (inventory == null || playerCamera == null)
        {
            Debug.LogError(
                "Inventory atau Player Camera belum diisi.",
                this
            );

            enabled = false;
            return;
        }

        interactAction = new InputAction(
            name: "Interact",
            type: InputActionType.Button,
            binding: "<Keyboard>/e"
        );
    }

    private void OnEnable()
    {
        if (interactAction == null)
        {
            return;
        }

        interactAction.performed += OnInteractPerformed;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        if (interactAction == null)
        {
            return;
        }

        interactAction.performed -= OnInteractPerformed;
        interactAction.Disable();
    }

    private void OnDestroy()
    {
        interactAction?.Dispose();
    }

    private void OnInteractPerformed(
        InputAction.CallbackContext context
    )
    {
        TryInteract();
    }

    // Bisa dihubungkan ke UnityEvent untuk menonaktifkan
    // interaksi saat fade, cutscene, atau puzzle.
    public void SetInteractionEnabled(bool value)
    {
        interactionEnabled = value;
    }

    public void TryInteract()
    {
        if (!isActiveAndEnabled ||
            !interactionEnabled ||
            inventory == null ||
            playerCamera == null)
        {
            return;
        }

        bool hitSomething = Physics.Raycast(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            out RaycastHit hit,
            interactionDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hitSomething)
        {
            return;
        }

        RoomPickup pickup =
            hit.collider.GetComponentInParent<RoomPickup>();

        if (pickup != null && pickup.isActiveAndEnabled)
        {
            pickup.Interact(inventory);
            return;
        }

        RoomKeyDoor keyDoor =
    hit.collider.GetComponentInParent<RoomKeyDoor>();

if (keyDoor != null && keyDoor.isActiveAndEnabled)
{
    keyDoor.Interact(inventory);
    return;
}

RoomDoor roomDoor =
    hit.collider.GetComponentInParent<RoomDoor>();

if (roomDoor != null && roomDoor.isActiveAndEnabled)
{
    roomDoor.Interact();
}
    }
}