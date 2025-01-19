using System.Collections.Generic;
using UnityEngine;

public class Logger : MonoBehaviour
{
    private List<(Vector3, object, bool)> messages;

    public static Logger Instance;
    private void Start() {
        Instance = this;
        messages = new List<(Vector3, object, bool)> ();
    }

    private void FixedUpdate() {
        for (int i = messages.Count - 1; i >= 0; i--) {
            if (messages[i].Item3) {
                messages.RemoveAt(i);
            }
        }
    }

    public void Log(Vector3 point, object message) {
        messages.Add((point, message, Time.inFixedTimeStep));
    }

    private void OnDrawGizmos() {
        if (messages == null)
            return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 30;

#if UNITY_EDITOR
        foreach (var item in messages) {
            UnityEditor.Handles.Label(item.Item1, item.Item2.ToString(), style);
        }
#endif

        for (int i = messages.Count - 1; i >= 0; i--) {
            if (!messages[i].Item3) {
                messages.RemoveAt(i);
            }
        }
    }
}
