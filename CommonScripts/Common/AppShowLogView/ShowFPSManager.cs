using System;
using UnityEngine;
using UniRx;

public class ShowFPSManager : MonoSingleton<ShowFPSManager>
{
    int m_iFps = 0;
    int m_iTmpFrames = 0;
    float m_fLastTime = 0;

    GUIStyle textStyle = new GUIStyle();

    string fpsStr = string.Empty;
    bool isShow = false;

    int textureWidth { get { return 10; } }
    int textureHeight { get { return 75; } }

    private void Awake()
    {
        updataFPS();
        textStyle.fontSize = 20;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;

        Observable.Timer(TimeSpan.FromSeconds(1)).Repeat().Subscribe(_ =>
        {
            updataFPS();
        }).AddTo(this);
    }

    public void startShow()
    {
        isShow = true;
    }

    void updataFPS()
    {
        fpsStr = m_iFps.ToString();
    }

    private void Update()
    {
        getFPS();
    }

    void getFPS()
    {
        ++m_iTmpFrames;
        if (Time.realtimeSinceStartup - 1 > m_fLastTime)
        {
            m_iFps = m_iTmpFrames;
            m_iTmpFrames = 0;
            m_fLastTime = Time.realtimeSinceStartup;
        }
    }

    private void OnGUI()
    {
        if (isShow)
        {
            GUI.Label(new Rect(20, (Screen.height / 2) + 200, textureWidth, textureHeight), $"FPS:{fpsStr}", textStyle);
        }
    }
}
