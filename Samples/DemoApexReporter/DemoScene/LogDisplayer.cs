using UnityEngine;
using System.Collections;

public class LogDisplayer : MonoBehaviour
{
    string currentLog;
    Queue logQueue = new Queue();
    float counter = 0f;

    void Start()
    {
    }

    private void Update()
    {
        if (logQueue.Count > 0)
        {
            counter += Time.deltaTime;

            if (counter > 5.0f)
            {
                counter -= 5.0f;
                logQueue.Dequeue();
                RebuildLog();
            }
        }
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        currentLog = logString;
        string newString = "\n [" + type + "] : " + currentLog;
        
        logQueue.Enqueue(newString);
        
        if (type == LogType.Exception)
        {
            newString = "\n" + stackTrace;
            logQueue.Enqueue(newString);
        }

        RebuildLog();
    }

    void RebuildLog()
    {
        currentLog = string.Empty;

        foreach (string log in logQueue)
        {
            currentLog += log;
        }
    }

    void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.red;
        GUILayout.Label(currentLog, labelStyle);
    }
}
