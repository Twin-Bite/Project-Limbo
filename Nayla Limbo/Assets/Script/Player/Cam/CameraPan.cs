using UnityEngine;

public class CameraPan : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Pan Settings")]
    public float maxYaw = 12f;
    public float followSpeed = 3f;

    [Header("Collision Check")]
    public LayerMask obstructionLayer; // layer tembok / bangunan
    public float sphereRadius = 0.2f;

    private Quaternion baseRotation;
    private float currentYaw = 0f;

    private void Awake()
    {
        baseRotation = transform.rotation;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void OnEnable()
    {
        baseRotation = transform.rotation;
        currentYaw = 0f;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        // if (dir.sqrMagnitude < 0.01f) return;

        // Quaternion lookRot = Quaternion.LookRotation(dir);

        // float targetYaw = Mathf.DeltaAngle(
        //     baseRotation.eulerAngles.y,
        //     lookRot.eulerAngles.y
        // );
        Vector3 localPos = transform.InverseTransformPoint(player.position);
        float targetYaw = Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg;
        
        targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);

        // ===== OBSTRUCTION CHECK =====
        Quaternion testRot = Quaternion.Euler(
            baseRotation.eulerAngles.x,
            baseRotation.eulerAngles.y + targetYaw,
            baseRotation.eulerAngles.z
        );

        Vector3 testDir = testRot * Vector3.forward;
        Vector3 origin = transform.position;
        float distance = Vector3.Distance(origin, player.position);

        bool blocked = Physics.SphereCast(
            origin,
            sphereRadius,
            testDir,
            out RaycastHit hit,
            distance,
            obstructionLayer
        );

        if (!blocked)
        {
            // hanya update yaw kalau aman
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * followSpeed);
        }

        Quaternion finalRot = Quaternion.Euler(
            baseRotation.eulerAngles.x,
            baseRotation.eulerAngles.y + currentYaw,
            baseRotation.eulerAngles.z
        );

        transform.rotation = finalRot;
    }
}
