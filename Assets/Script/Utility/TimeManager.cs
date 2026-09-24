using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static Action OnMinuteChanged;

    public static Action OnHourChanged;

    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    public int minuteStart;

    public int hourStart;

    public float secondToIngameMinute = 0.5f;
    private float timer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
   
    void Start()
    {
        Minute = minuteStart;
        Hour = hourStart;
        timer = secondToIngameMinute;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Minute++;
            OnMinuteChanged?.Invoke();
            
            if (Minute >= 60)
            {
                Hour++;
                Minute = 0;
                OnHourChanged?.Invoke();
            }

            if (Hour >= 24)
            {
                Hour = 0;
            }
            timer = secondToIngameMinute;
        }
    
       
    }

}
