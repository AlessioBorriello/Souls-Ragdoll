using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameControl : MonoBehaviour
{


    // Adjust via the Inspector
    public bool showDebug = false;
    public int maxLines = 8;
    private Queue<string> queue = new Queue<string>();
    private string currentText = "";
    public float xLeftOffset = 300;
    public float yBottomOffset = 150;
    public float width = 240;
    public float height = 120;

    private float screenWidth = Screen.width;

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Delete oldest message
        if (queue.Count >= maxLines) queue.Dequeue();

        queue.Enqueue(logString);

        var builder = new StringBuilder();
        foreach (string st in queue)
        {
            builder.Append(st).Append("\n");
        }

        currentText = builder.ToString();
    }

    void OnGUI()
    {
        if (!showDebug) return;
        GUI.Label(new Rect(screenWidth - xLeftOffset, Screen.height - yBottomOffset, width, height), currentText, GUI.skin.textArea);
    }

}
