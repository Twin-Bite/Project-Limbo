using UnityEngine;

[DisallowMultipleComponent]
public class EnemyTarget : MonoBehaviour
{
    [SerializeField] private Transform sightPoint;
    [SerializeField] private EnemyHidingSpot[] hidingSpots = new EnemyHidingSpot[0];
    [SerializeField] private bool manuallyHidden;
    private CharacterController controller;
    public bool IsCaptured { get; private set; }

    private void Awake() { controller = GetComponent<CharacterController>(); }

    public Vector3 BodyPosition => controller != null && controller.enabled
        ? controller.bounds.center : transform.position + Vector3.up;

    public Vector3 SightPosition => sightPoint != null
        ? sightPoint.position : BodyPosition + Vector3.up * 0.35f;

    public Vector3 NavigationPosition
    {
        get
        {
            if (controller == null || !controller.enabled) return transform.position;
            Bounds bounds = controller.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
    }

    public bool IsHidden
    {
        get
        {
            if (manuallyHidden) return true;
            if (hidingSpots == null) return false;
            Vector3 body = BodyPosition;
            foreach (EnemyHidingSpot spot in hidingSpots)
                if (spot != null && spot.Contains(body)) return true;
            return false;
        }
    }

    public void SetHidden(bool value) { manuallyHidden = value; }

    public bool TryCapture()
    {
        if (IsCaptured || !isActiveAndEnabled) return false;
        IsCaptured = true;
        return true;
    }

    public void ResetCapture() { IsCaptured = false; }
}