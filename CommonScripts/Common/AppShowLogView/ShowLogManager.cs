using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class ShowLogManager : MonoSingleton<ShowLogManager>
{
    float Distance
    {
        get => distance;
        set
        {
            distance = Mathf.Clamp01(value);
        }
    }

    int maxLogCount { get { return 50; } }

    int logDataCount { get; set; }

    StringBuilder myLog = new StringBuilder();
    bool isEnable = false;

    GUIStyle textStyle = new GUIStyle();
    GUIStyle buttonStyle = new GUIStyle();

    float oldY = 0;
    float newY = 0;
    float currentShakeDistance = 0;
    float distance = 0.75f;

    bool isAlreadyShow = false;

    public void appShowConsoleLogView()
    {
        if (isAlreadyShow)
        {
            return;
        }
      
        isAlreadyShow = true;
        Distance = 0.5f;

        textStyle.fontSize = 20;
        Color textColor = Color.black;
        textColor.a = 0.5f;
        textStyle.normal.background = makeTexture(textColor);
        textStyle.normal.textColor = Color.white;

        Color btnColor = Color.black;
        btnColor.a = 0.5f;
        buttonStyle.fontSize = 20;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.background = makeTexture(btnColor);
        buttonStyle.normal.textColor = Color.white;

        Application.logMessageReceived += Log;
    }

    protected override void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        base.OnDestroy();
    }

    Texture2D makeTexture(Color color)
    {
        Color[] pix = new Color[Screen.width * Screen.height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = color;
        }

        Texture2D result = new Texture2D(Screen.width, Screen.height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
    void Log(string logString, string stackTrace, LogType logType)
    {
        if (LogType.Warning == logType)
        {
            return;
        }

        logDataCount++;
        if (logDataCount > maxLogCount)
        {
            logDataCount--;
            myLog.Remove(0, myLog.ToString().IndexOf("\n"));
        }

        switch (logType)
        {
            case LogType.Log:
                myLog.Append("<color=white>");
                break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                myLog.Append("<color=red>");
                break;
        }
        myLog.Append($"{logString}</color>\n");
    }

    Vector2 scrollPos = Vector2.zero;

    private void OnGUI()
    {
        if (!isEnable)
        {
            return;
        }

        GUI.Label(new Rect(10, 10, Screen.width - 10, Screen.height - 60), myLog.ToString(), textStyle);
      
        if (GUI.Button(new Rect(0, Screen.height - 40, Screen.width, 40), "Clear Log", buttonStyle))
        {
            myLog.Clear();
            logDataCount = 0;
        }
    }

    void Update()
    {
        Shake();
    }

    void Shake()
    {
        newY = Input.acceleration.y;
        currentShakeDistance = newY - oldY;
        oldY = newY;

        if (currentShakeDistance > Distance)
        {
            isEnable = !isEnable;
        }
    }
}

