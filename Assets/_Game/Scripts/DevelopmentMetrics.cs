#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace ZombieSurvivor
{
    // Development builds only. Reports application frame intervals, not GPU timings.
    public sealed class DevelopmentMetrics : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install() => new GameObject("Development Metrics").AddComponent<DevelopmentMetrics>();
        private EncounterSession session;
        private readonly List<float> frames = new List<float>(4096);
        private float nextReport;
        private int peakEnemies;
        private EncounterSession.SessionState lastState;
        private bool stress;
        private bool soak;

        private IEnumerator Start()
        {
            yield return null;
            session = FindFirstObjectByType<EncounterSession>();
            nextReport = Time.realtimeSinceStartup + 30;
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            {
                stress = intent.Call<bool>("getBooleanExtra", "zs_stress", false);
                soak = intent.Call<bool>("getBooleanExtra", "zs_soak", false);
            }
#endif
            if (stress)
            {
                session.Restart();
                session.Weapons.enabled = false;
                session.Advance(120);
                session.enabled = false;
                session.PlayerHealth.ResetHealth(1000000000, 0);
                Debug.Log("ZS_STRESS development-only: frozen session clock, weapons disabled, high health, target 100 enemies");
            }
            else if (soak)
            {
                session.Restart();
                session.PlayerHealth.ResetHealth(1000000000, 0);
                Debug.Log("ZS_SOAK development-only: high health for real-time 180-second lifecycle verification; not a balance test");
            }
        }
        private void Update()
        {
            if (session == null) return;
            if (stress)
                while (session.ActiveCount < 100)
                    if (!session.TrySpawn()) break;
            if (session.IsRunning)
            {
                frames.Add(Time.unscaledDeltaTime * 1000);
                peakEnemies = Mathf.Max(peakEnemies, session.ActiveCount);
            }
            if (Time.realtimeSinceStartup >= nextReport || session.State != lastState)
            {
                if (frames.Count > 0)
                {
                    frames.Sort();
                    float sum = 0;
                    foreach (float frame in frames) sum += frame;
                    float p95 = frames[Mathf.Min(frames.Count - 1, Mathf.FloorToInt(frames.Count * .95f))];
                    Debug.Log($"ZS_METRICS stress={stress} soak={soak} state={session.State} elapsed={session.Elapsed:F2} hp={session.PlayerHealth.Current} kills={session.Kills} enemies={session.ActiveCount} peakEnemies={peakEnemies} samples={frames.Count} meanMs={sum / frames.Count:F2} p95Ms={p95:F2} memoryMB={Profiler.GetTotalAllocatedMemoryLong() / 1048576f:F1}");
                    frames.Clear();
                    peakEnemies = 0;
                }
                lastState = session.State;
                nextReport = Time.realtimeSinceStartup + 30;
            }
        }
    }
}
#endif
