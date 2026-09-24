using System;
using TMPro;
using UnityEngine;

public class ClockSystem : MonoBehaviour
{
    public TextMeshProUGUI clockText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        TimeManager.OnMinuteChanged += UpdateTime;
        TimeManager.OnHourChanged += UpdateTime;
    }

    private void OnDisable()
    {
        TimeManager.OnMinuteChanged -= UpdateTime;
        TimeManager.OnHourChanged -= UpdateTime;
    }

   
    private void UpdateTime()
    {
        clockText.text = $"{TimeManager.Hour:00}:{TimeManager.Minute:00}";
    }

}
