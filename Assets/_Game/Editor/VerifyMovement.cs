using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ZombieSurvivor.Editor
{
    [InitializeOnLoad]
    public static class VerifyMovement
    {
        private const string Pending = "ZombieSurvivor.VerifyMovement";
        static VerifyMovement()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                {
                    SessionState.SetBool(Pending, false);
                    new GameObject("Movement verification").AddComponent<MovementProbe>();
                }
            };
        }

        public static void Run()
        {
            double deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += () => { if (EditorApplication.timeSinceStartup > deadline) EditorApplication.Exit(2); };
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Arena.unity");
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
    }

    public sealed class MovementProbe : MonoBehaviour
    {
        private bool failed;
        private Keyboard keyboard;
        private void OnEnable() => Application.logMessageReceived += ObserveLog;
        private void OnDisable() => Application.logMessageReceived -= ObserveLog;
        private void ObserveLog(string message, string stack, LogType type)
        {
            // Unity 6000.3 editor search indexing can throw during batch startup.
            // Keep it visible in the log, while evaluating runtime errors separately.
            if (type == LogType.Exception && stack.Contains("UnityEditor.Search.SearchDatabase")) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }
        private void Check(bool condition, string message)
        {
            if (!condition) { failed = true; Debug.LogError("Movement verification: " + message); }
        }
        private IEnumerator Start()
        {
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            var player = FindFirstObjectByType<PlayerController>();
            var camera = Camera.main;
            if (player == null || camera == null) { EditorApplication.Exit(1); yield break; }
            yield return null;
            Vector3 start = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(.5f);
            Check(player.transform.position.z > start.z + 1, "W must move forward");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            start = player.transform.position;
            yield return new WaitForSeconds(.2f);
            Check(Vector3.Distance(start, player.transform.position) < .01f, "release must stop movement");
            player.transform.position = new Vector3(23.9f, 1, 23.9f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            yield return new WaitForSeconds(.2f);
            Check(player.transform.position.x <= 24 && player.transform.position.z <= 24, "arena bounds");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            Check(Vector3.Distance(camera.transform.position - player.transform.position, new Vector3(0, 18, -12)) < .01f, "camera must follow");
            InputSystem.RemoveDevice(keyboard);
            Debug.Log(failed ? "MOVEMENT VERIFICATION FAILED" : "MOVEMENT VERIFICATION PASSED");
            EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
