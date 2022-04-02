using TMPro;
using UnityEngine;

public class FrameRateCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI display;

    [SerializeField, Range(0.1f, 2.0f)] private float sampleDuration = 1.0f;

    private int m_FrameCount;
    private float m_Duration;
    private float m_BestDuration;
    private float m_WorstDuration;

    private enum DisplayMode
    {
        FPS,
        MS
    }

    [SerializeField] private DisplayMode displayMode = DisplayMode.FPS;


    private static string textFPSTemplate = "FPS\nmax:{0:0}\navg:{1:0}\nmin:{2:0}";

    private static string textMSTemplate = "MS\nmax:{0:1}\navg:{1:1}\nmin:{2:1}";
    // Start is called before the first frame update

    private void ResetCounter()
    {
        m_FrameCount = 0;
        m_Duration = 0.0f;
        m_BestDuration = float.MaxValue;
        m_WorstDuration = float.MinValue;
    }

    private void UpdateText()
    {
        switch (displayMode)
        {
            case DisplayMode.FPS:
                display.SetText(textFPSTemplate, 1.0f / m_BestDuration, m_FrameCount / m_Duration,
                    1.0f / m_WorstDuration);
                break;
            case DisplayMode.MS:
                display.SetText(textMSTemplate, 1000.0f * m_BestDuration, 1000.0f * m_Duration / m_FrameCount,
                    1000.0f * m_WorstDuration);
                break;
        }
    }

    void Start()
    {
        ResetCounter();
    }

    // Update is called once per frame
    void Update()
    {
        // 上一帧到这一帧经过的时间（绝对时间）
        float frameDuration = Time.unscaledDeltaTime;
        ++m_FrameCount;
        m_Duration += frameDuration;

        m_BestDuration = Mathf.Min(m_BestDuration, frameDuration);
        m_WorstDuration = Mathf.Max(m_WorstDuration, frameDuration);

        if (m_Duration < sampleDuration) return;
        UpdateText();
        ResetCounter();
    }
}