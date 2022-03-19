using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Transform hoursPivot,minutesPivot,secondsPivot;

    private const float hoursToDegrees = -30.0f, minutesToDegrees = -6.0f, secondsToDegrees = -6.0f;


    private void rotateArm()
    {
        TimeSpan time = DateTime.Now.TimeOfDay;
        // time.TotalHours范围是[0,24]，但是旋转的时候多转一圈也没什么关系，因此可以直接算
        hoursPivot.localRotation = Quaternion.Euler(0,0, hoursToDegrees *(float)time.TotalHours);

        minutesPivot.localRotation = Quaternion.Euler(0,0,minutesToDegrees * (float)time.TotalMinutes);

        secondsPivot.localRotation = Quaternion.Euler(0,0,secondsToDegrees * (float)time.TotalSeconds);
    }
    private void Awake()
    {
        rotateArm();
    }

    // Start is called before the first frame update
    void Start()
    {
        rotateArm();
    }

    // Update is called once per frame
    void Update()
    {
        rotateArm();
    }
}
