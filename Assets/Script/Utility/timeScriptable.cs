using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "timeScriptable", menuName = "Scriptable Objects/timeScriptable")]
public class timeScriptable : ScriptableObject
{
    [SerializeField] public List<TimeEventData> TimeEventData = new ();
    public static Action<string> OnEvent;

    public void Check(TimeEvent timeEvent)
    {
        if (TimeEventData.Any(c => c.Time == timeEvent))
        {
            Debug.Log("SDSD");
        }
    }
    
    public void Check(int Hour, int Minute)
    {
        var x = TimeEventData.FirstOrDefault(c => c.Time.Hour == Hour && c.Time.Minute == Minute);
        if (x != null)
        {
            Debug.Log(x.EventName);
            OnEvent?.Invoke(x.EventName);
        }
    }
}

[Serializable]
public class TimeEventData
{
    public TimeEvent Time;
    public string EventName;
}

[Serializable]
public class TimeEvent : IEquatable<TimeEvent>
{
    public int Hour;
    public int Minute;

    public bool Equals(TimeEvent other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Hour == other.Hour && Minute == other.Minute;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TimeEvent) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Hour * 397) ^ Minute;
        }
    }
}