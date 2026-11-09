using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RoomLockWheelData
{
//some fuckass fix lmao
    public Transform wheel;
    public Vector3 localAxis = Vector3.up;
    public bool reverseDirection;
    [Range(0, 9)] public int startingDigit;
    public UnityEvent<int> onDigitChanged = new UnityEvent<int>();
}

[DisallowMultipleComponent]
public class RoomCombinationLock : MonoBehaviour
{
    [Header("Wheels - urutan sama dengan Correct Code")]
    [SerializeField] private RoomLockWheelData[] wheelData =
    {
        new RoomLockWheelData(),
        new RoomLockWheelData(),
        new RoomLockWheelData()
    };
    [SerializeField] private int[] correctCode = { 3, 7, 2 };

    [Header("Settings")]
    [SerializeField] private bool automaticCheck = true;
    [Min(0f)] [SerializeField] private float rotationDuration = 0.15f;

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();
    [SerializeField] private UnityEvent onWrongCode = new UnityEvent();
    [SerializeField] private UnityEvent onReset = new UnityEvent();

    private int[] currentDigits;
    private bool[] rotating;
    private Quaternion[] zeroRotations;
    public bool IsReady { get; private set; }
    public bool IsUnlocked { get; private set; }
    public UnityEvent OnUnlocked => onUnlocked;

    private void Awake()
    {
        if (wheelData == null || wheelData.Length == 0 ||
            correctCode == null || correctCode.Length != wheelData.Length)
        {
            Fail("Jumlah Wheel Data dan Correct Code harus sama dan tidak kosong.");
            return;
        }

        for (int i = 0; i < wheelData.Length; i++)
        {
            RoomLockWheelData data = wheelData[i];
            if (data == null || data.wheel == null ||
                data.wheel == transform || !data.wheel.IsChildOf(transform) ||
                data.localAxis.sqrMagnitude < 0.0001f ||
                data.startingDigit < 0 || data.startingDigit > 9 ||
                correctCode[i] < 0 || correctCode[i] > 9)
            {
                Fail("Wheel " + i + ": cek Transform, Local Axis, dan angka 0-9.");
                return;
            }

            for (int j = 0; j < i; j++)
            {
                Transform other = wheelData[j].wheel;
                if (data.wheel.IsChildOf(other) || other.IsChildOf(data.wheel))
                {
                    Fail("Setiap roda harus terpisah; jangan duplikat atau menumpuk parent roda.");
                    return;
                }
            }
        }

        currentDigits = new int[wheelData.Length];
        rotating = new bool[wheelData.Length];
        zeroRotations = new Quaternion[wheelData.Length];
        for (int i = 0; i < wheelData.Length; i++)
        {
            // Pose di Editor harus menunjukkan angka 0 pada garis pembaca.
            zeroRotations[i] = wheelData[i].wheel.localRotation;
            currentDigits[i] = wheelData[i].startingDigit;
            SnapWheel(i);
        }
        IsReady = true;
    }

    private void Start()
    {
        if (!IsReady) return;
        for (int i = 0; i < wheelData.Length; i++)
            wheelData[i].onDigitChanged.Invoke(currentDigits[i]);
        if (automaticCheck) Evaluate(false);
    }

    private void Fail(string message)
    {
        Debug.LogError("[RoomCombinationLock] " + message, this);
        enabled = false;
    }

    public int FindWheelIndex(Transform hitTransform)
    {
        if (!IsReady || hitTransform == null) return -1;
        for (int i = 0; i < wheelData.Length; i++)
            if (hitTransform.IsChildOf(wheelData[i].wheel)) return i;
        return -1;
    }

    public void RotateNext(int index) { Rotate(index, 1); }
    public void RotatePrevious(int index) { Rotate(index, -1); }

    private void Rotate(int index, int direction)
    {
        if (!isActiveAndEnabled || !IsReady || IsUnlocked ||
            index < 0 || index >= wheelData.Length || rotating[index]) return;
        rotating[index] = true;
        StartCoroutine(RotateRoutine(index, direction));
    }

    private IEnumerator RotateRoutine(int index, int direction)
    {
        RoomLockWheelData data = wheelData[index];
        Quaternion from = data.wheel.localRotation;
        float sign = data.reverseDirection ? -1f : 1f;
        float angle = direction * sign * 36f;
        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / rotationDuration));
            data.wheel.localRotation = from *
                Quaternion.AngleAxis(angle * t, data.localAxis.normalized);
            yield return null;
        }

        currentDigits[index] = (currentDigits[index] + direction + 10) % 10;
        SnapWheel(index);
        rotating[index] = false;
        data.onDigitChanged.Invoke(currentDigits[index]);
        if (automaticCheck) Evaluate(false);
    }

    private void SnapWheel(int index)
    {
        RoomLockWheelData data = wheelData[index];
        float sign = data.reverseDirection ? -1f : 1f;
        data.wheel.localRotation = zeroRotations[index] * Quaternion.AngleAxis(
            currentDigits[index] * sign * 36f, data.localAxis.normalized);
    }

    public void CheckCode() { Evaluate(true); }

    private void Evaluate(bool reportWrong)
    {
        if (!isActiveAndEnabled || !IsReady || IsUnlocked) return;
        for (int i = 0; i < rotating.Length; i++)
            if (rotating[i]) return;

        for (int i = 0; i < currentDigits.Length; i++)
        {
            if (currentDigits[i] == correctCode[i]) continue;
            if (reportWrong)
            {
                Debug.Log("[RoomCombinationLock] Kode belum tepat.", this);
                onWrongCode.Invoke();
            }
            return;
        }

        IsUnlocked = true;
        Debug.Log("[RoomCombinationLock] Gembok terbuka!", this);
        onUnlocked.Invoke();
    }

    public void ResetPuzzle()
    {
        if (!isActiveAndEnabled || !IsReady) return;
        StopAllCoroutines();
        IsUnlocked = false;
        for (int i = 0; i < wheelData.Length; i++)
        {
            rotating[i] = false;
            currentDigits[i] = wheelData[i].startingDigit;
            SnapWheel(i);
            wheelData[i].onDigitChanged.Invoke(currentDigits[i]);
        }
        onReset.Invoke();
        if (automaticCheck) Evaluate(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (!IsReady) return;
        for (int i = 0; i < wheelData.Length; i++)
        {
            rotating[i] = false;
            if (wheelData[i].wheel != null) SnapWheel(i);
        }
    }
}
