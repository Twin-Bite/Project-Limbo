using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RoomLockWheelData
{
    public Transform wheel;

    [Tooltip("Sumbu putaran roda kunci.")]
    public Vector3 localAxis = Vector3.right;

    public bool reverseDirection;

    [Range(0, 9)]
    public int startingDigit;

    public UnityEvent<int> onDigitChanged = new UnityEvent<int>();
}

public class RoomCombinationLock : MonoBehaviour
{
    private const int DigitCount = 10;
    private const float StepAngle = 360f / DigitCount;

    [Header("Wheels")]
    [SerializeField] private RoomLockWheelData[] wheels =
    {
        new RoomLockWheelData(),
        new RoomLockWheelData(),
        new RoomLockWheelData()
    };

    [Header("Combination")]
    [SerializeField] private int[] correctCode = { 3, 7, 2};

    [Tooltip("Periksa kombinasi setelah roda selesai diputar.")]
    [SerializeField] private bool automaticCheck = true;

    [Header("Rotation")]
    [Min(0f)]
    [SerializeField] private float rotationDuration = 0.15f;

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();
    [SerializeField] private UnityEvent onWrongCode = new UnityEvent();
    [SerializeField] private UnityEvent onReset = new UnityEvent();

    private int[] currentDigits;
    private bool[] rotating;
    private Quaternion[] zeroRotations;

    private bool ready;
    public bool IsUnlocked{ get; private set; }
    
    private void Awake()
    {
        if (!ValidateConfiguration())
        {
            return;
        }

        currentDigits = new int[wheels.Length];
        rotating = new bool[wheels.Length];
        zeroRotations = new Quaternion[wheels.Length];

        for (int i = 0; i < wheels.Length; i++)
        {
            zeroRotations[i] = wheels[i].wheel.localRotation;
        }

        ready = true;
        SetStartingDigits();
    }

    private void Start()
    {
        if (!ready)
        {
            return;
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].onDigitChanged.Invoke(currentDigits[i]);
        }

        if (automaticCheck)
        {
            EvaluateCode(false);
        }
    }

    public void RotateNext(int wheelIndex)
    {
        RequestRotation(wheelIndex, 1);
    }

    public void RotatePrevious(int wheelIndex)
    {
        RequestRotation(wheelIndex, -1);
    }

    // Buat tombol buka
    public void CheckCode()
    {
        EvaluateCode(true);
    }

    // Reset Puzzle
    public void ResetPuzzle()
    {
        if (!ready)
        {
            return;
        }

        StopAllCoroutines();

        IsUnlocked = true;
        SetStartingDigits();

        onReset.Invoke();

        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].onDigitChanged.Invoke(currentDigits[i]);
        }

        if (automaticCheck)
        {
            EvaluateCode(false);
        }
    }

    private void RequestRotation(int wheelIndex, int direction)
    {
        if (!isActiveAndEnabled || !ready || IsUnlocked)
        {
            return;
        }

        if (wheelIndex < 0 || wheelIndex >= wheels.Length)
        {
            Debug.LogError(
                $"Index roda {wheelIndex} ga valid.",
                this
            );

            return;
        }

        if (rotating[wheelIndex])
        {
            return;
        }

        rotating[wheelIndex] = true;

        StartCoroutine(
            RotateWheelRoutine(wheelIndex, direction)
        );
    }

    private IEnumerator RotateWheelRoutine(int wheelIndex, int direction)
    {
        RoomLockWheelData data = wheels[wheelIndex];
        Transform wheel = data.wheel;

        int nextDigit = (currentDigits[wheelIndex] + direction + DigitCount) % DigitCount;
        Quaternion startRotation = wheel.localRotation;

        Vector3 axis = data.localAxis.normalized;

        float rotationSign = data.reverseDirection ? -1f : 1f;
        float angle = direction * StepAngle * rotationSign;

        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / rotationDuration);

            wheel.localRotation =
                startRotation *
                Quaternion.AngleAxis(angle * progress, axis);

            yield return null;
        }

        // Angka baru berlaku setelah putaran selesai.
        currentDigits[wheelIndex] = nextDigit;

        AlignWheel(wheelIndex);
        rotating[wheelIndex] = false;

        data.onDigitChanged.Invoke(nextDigit);

        if (automaticCheck)
        {
            EvaluateCode(false);
        }
    }

    private void EvaluateCode(bool reportWrongCode)
    {
        if (!isActiveAndEnabled || !ready || IsUnlocked)
        {
            return;
        }

        // Jangan membuka saat masih ada roda yang bergerak.
        for (int i = 0; i < rotating.Length; i++)
        {
            if (rotating[i])
            {
                return;
            }
        }

        for (int i = 0; i < correctCode.Length; i++)
        {
            if (currentDigits[i] != correctCode[i])
            {
                if (reportWrongCode)
                {
                    onWrongCode.Invoke();
                }

                return;
            }
        }

        // Set status sebelum memanggil event.
        // Event unlock hanya berjalan sekali sampai puzzle direset.
        IsUnlocked = true;
        onUnlocked.Invoke();
    }

    private void SetStartingDigits()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            currentDigits[i] = wheels[i].startingDigit;
            rotating[i] = false;

            AlignWheel(i);
        }
    }

    private void AlignWheel(int wheelIndex)
    {
        RoomLockWheelData data = wheels[wheelIndex];

        if (data.wheel == null)
        {
            return;
        }

        float rotationSign = data.reverseDirection ? -1f : 1f;

        float angle =
            currentDigits[wheelIndex] * StepAngle * rotationSign;

        data.wheel.localRotation =
            zeroRotations[wheelIndex] *
            Quaternion.AngleAxis(
                angle,
                data.localAxis.normalized
            );
    }

    private bool ValidateConfiguration()
    {
        if (wheels == null || wheels.Length == 0)
        {
            return ConfigurationError(
                "Array Wheels belum diisi."
            );
        }

        if (correctCode == null ||
            correctCode.Length != wheels.Length)
        {
            return ConfigurationError(
                "Jumlah angka Correct Code harus sama " +
                "dengan jumlah Wheels."
            );
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null || wheels[i].wheel == null)
            {
                return ConfigurationError(
                    $"Transform pada Wheels[{i}] belum diisi."
                );
            }

            if (wheels[i].localAxis.sqrMagnitude < 0.0001f)
            {
                return ConfigurationError(
                    $"Local Axis pada Wheels[{i}] tidak boleh nol."
                );
            }

            if (correctCode[i] < 0 ||
                correctCode[i] >= DigitCount ||
                wheels[i].startingDigit < 0 ||
                wheels[i].startingDigit >= DigitCount)
            {
                return ConfigurationError(
                    $"Angka pada index {i} harus antara 0 dan 9."
                );
            }

            for (int j = 0; j < i; j++)
            {
                if (wheels[i].wheel == wheels[j].wheel)
                {
                    return ConfigurationError(
                        "Setiap elemen Wheels harus memakai " +
                        "Transform roda yang berbeda."
                    );
                }
            }
        }

        return true;
    }

    private bool ConfigurationError(string message)
    {
        Debug.LogError(message, this);
        return false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (!ready)
        {
            return;
        }

        // Batalkan putaran yang belum selesai.
        // Kembalikan model ke angka terakhir yang sudah berlaku.
        for (int i = 0; i < wheels.Length; i++)
        {
            rotating[i] = false;
            AlignWheel(i);
        }
    }
}
