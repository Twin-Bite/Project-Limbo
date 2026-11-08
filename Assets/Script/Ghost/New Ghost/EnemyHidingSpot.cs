using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class EnemyHidingSpot : MonoBehaviour
{
    private BoxCollider zone;
    private void Awake() { zone = GetComponent<BoxCollider>(); }
    private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

    public bool Contains(Vector3 worldPosition)
    {
        if (!isActiveAndEnabled) return false;
        if (zone == null) zone = GetComponent<BoxCollider>();
        if (zone == null || !zone.enabled) return false;
        Vector3 local = zone.transform.InverseTransformPoint(worldPosition) - zone.center;
        Vector3 half = zone.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y &&
            Mathf.Abs(local.z) <= half.z;
    }
}
