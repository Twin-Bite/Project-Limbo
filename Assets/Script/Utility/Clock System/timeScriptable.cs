using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClockSchedule", menuName = "Limbo/Clock Schedule")]
public class timeScriptable : ScriptableObject
{
    public List<TimeEventData> TimeEventData = new List<TimeEventData>();

    // Callback C# lama. Untuk aksi di scene, isi Event Bindings pada TimeManager.
    public static Action<string> OnEvent;

    internal void CollectEventNames(int hour, int minute, List<string> results)
    {
        results.Clear();
        if (TimeEventData == null) return;

        foreach (TimeEventData entry in TimeEventData)
        {
            if (entry?.Time == null || entry.Time.Hour != hour || entry.Time.Minute != minute ||
                string.IsNullOrWhiteSpace(entry.EventName))
            {
                continue;
            }

            string eventName = entry.EventName.Trim();
            if (!results.Contains(eventName)) results.Add(eventName);
        }
    }

    [Obsolete("TimeManager sudah memicu jadwal secara otomatis.")]
    public void Check(TimeEvent timeEvent)
    {
        if (timeEvent != null) Check(timeEvent.Hour, timeEvent.Minute);
    }

    [Obsolete("TimeManager sudah memicu jadwal secara otomatis.")]
    public void Check(int Hour, int Minute)
    {
        var eventNames = new List<string>();
        CollectEventNames(Hour, Minute, eventNames);
        foreach (string eventName in eventNames)
            OnEvent?.Invoke(eventName);
    }
}

[Serializable]
public class TimeEventData
{
    public TimeEvent Time = new TimeEvent();
    public string EventName;
}

[Serializable]
public class TimeEvent : IEquatable<TimeEvent>
{
    [Range(0, 23)] public int Hour;
    [Range(0, 59)] public int Minute;

    public bool Equals(TimeEvent other)
    {
        return other != null && Hour == other.Hour && Minute == other.Minute;
    }

    public override bool Equals(object obj)
    {
        return obj is TimeEvent other && other.GetType() == GetType() && Equals(other);
    }

    public override int GetHashCode()
    {
        return unchecked((Hour * 397) ^ Minute);
    }
}
