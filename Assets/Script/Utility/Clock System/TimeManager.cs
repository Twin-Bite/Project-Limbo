using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class TimeManager : MonoBehaviour
{
    [Serializable]
    private class ClockEventBinding
    {
        public string eventName;
        public UnityEvent onReached = new UnityEvent();
    }

    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int MinutesPerDay = HoursPerDay * MinutesPerHour;
    private const int MaxMinutesPerFrame = 120;

    // Jalur kompatibilitas untuk script yang sudah membaca jam dari C#.
    public static Action OnMinuteChanged;
    public static Action OnHourChanged;
    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    [Header("Scene Clock")]
    [Range(0, 23)] public int hourStart;
    [Range(0, 59)] public int minuteStart;
    [Min(0.01f)] public float secondToIngameMinute = 0.5f;
    [Min(0)] public int doorTimeAdd;
    [SerializeField] private bool startPaused;

    [Header("Schedule")]
    [SerializeField] private timeScriptable data;
    [SerializeField] private bool fireEventsAtStart = true;
    [SerializeField] private ClockEventBinding[] eventBindings = Array.Empty<ClockEventBinding>();

    [Header("Clock Display")]
    [SerializeField] private UnityEvent<string> onTimeChanged = new UnityEvent<string>();

    private readonly List<string> dueEventNames = new List<string>();
    private int minuteOfDay;
    private double elapsedSeconds;
    private long pendingManualMinutes;
    private bool isPaused;
    private bool hasStarted;
    private bool isPublishing;
    private int budgetFrame = -1;
    private int minutesProcessedThisFrame;

    public int CurrentHour => minuteOfDay / MinutesPerHour;
    public int CurrentMinute => minuteOfDay % MinutesPerHour;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Hour = 0;
        Minute = 0;
        OnMinuteChanged = null;
        OnHourChanged = null;
        timeScriptable.OnEvent = null;
    }

    private void Awake()
    {
        minuteOfDay = Mathf.Clamp(hourStart, 0, HoursPerDay - 1) * MinutesPerHour
            + Mathf.Clamp(minuteStart, 0, MinutesPerHour - 1);
        isPaused = startPaused;
        Hour = CurrentHour;
        Minute = CurrentMinute;
    }

    private void Start()
    {
        hasStarted = true;
        PublishTime(hourChanged: false, checkSchedule: fireEventsAtStart);
        ProcessTime();
    }

    private void Update()
    {
        if (!isPaused)
            elapsedSeconds += Time.deltaTime;

        ProcessTime();
    }

    public void TogglePause(bool paused)
    {
        isPaused = paused;
    }

    public void AddTime()
    {
        AddMinutes(doorTimeAdd);
    }

    public void AddMinutes(int minutes)
    {
        if (minutes <= 0 || !enabled || !gameObject.activeInHierarchy) return;

        pendingManualMinutes += minutes;
        ProcessTime();
    }

    private void ProcessTime()
    {
        if (!hasStarted || isPublishing) return;

        if (budgetFrame != Time.frameCount)
        {
            budgetFrame = Time.frameCount;
            minutesProcessedThisFrame = 0;
        }

        double secondsPerMinute = Mathf.Max(secondToIngameMinute, 0.01f);

        // Lompatan besar dibagi ke beberapa frame. Jadwal tetap dicek berurutan.
        while (minutesProcessedThisFrame < MaxMinutesPerFrame && isActiveAndEnabled)
        {
            if (pendingManualMinutes > 0)
            {
                pendingManualMinutes--;
            }
            else
            {
                if (isPaused || Time.timeScale <= 0f || elapsedSeconds < secondsPerMinute)
                    break;

                elapsedSeconds -= secondsPerMinute;
            }

            minutesProcessedThisFrame++;
            int oldHour = CurrentHour;
            minuteOfDay = (minuteOfDay + 1) % MinutesPerDay;
            PublishTime(hourChanged: oldHour != CurrentHour, checkSchedule: true);
        }
    }

    private void PublishTime(bool hourChanged, bool checkSchedule)
    {
        // Listener yang memanggil AddMinutes akan menambah antrean.
        isPublishing = true;

        try
        {
            Hour = CurrentHour;
            Minute = CurrentMinute;
            onTimeChanged?.Invoke($"{Hour:00}:{Minute:00}");
            OnMinuteChanged?.Invoke();
            if (hourChanged) OnHourChanged?.Invoke();

            if (checkSchedule && data != null && isActiveAndEnabled)
                RunScheduledEvents();
        }
        finally
        {
            isPublishing = false;
        }
    }

    private void RunScheduledEvents()
    {
        data.CollectEventNames(CurrentHour, CurrentMinute, dueEventNames);

        foreach (string eventName in dueEventNames)
        {
            if (eventBindings != null)
            {
                foreach (ClockEventBinding binding in eventBindings)
                {
                    if (!isActiveAndEnabled) return;
                    if (binding != null && string.Equals(binding.eventName?.Trim(), eventName,
                        StringComparison.Ordinal))
                    {
                        binding.onReached?.Invoke();
                    }
                }
            }

            if (!isActiveAndEnabled) return;
            timeScriptable.OnEvent?.Invoke(eventName);
        }
    }
}
