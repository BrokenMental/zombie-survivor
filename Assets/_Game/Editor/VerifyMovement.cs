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
            var session = FindFirstObjectByType<EncounterSession>();
            if (session == null) { Check(false, "encounter missing"); EditorApplication.Exit(1); yield break; }
            var weapons = session.Weapons;
            weapons.enabled = false; // Isolate contact-damage checks from automatic fire.
            session.Restart();
            session.enabled = false; // Keep spawns under test control; enemies still run normally.
            yield return null;
            yield return null;
            Check(session.TrySpawn(), "spawn should find offscreen location");
            var enemy = session.Enemies[0];
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            Check(!GeometryUtility.TestPlanesAABB(planes, new Bounds(enemy.transform.position, new Vector3(1.5f, 2.5f, 1.5f))), "spawn must be outside view");
            Check(Vector3.Distance(enemy.transform.position, player.transform.position) >= session.Settings.minimumSpawnDistance, "spawn minimum distance");
            float distance = Vector3.Distance(enemy.transform.position, player.transform.position);
            yield return new WaitForSeconds(.25f);
            Check(Vector3.Distance(enemy.transform.position, player.transform.position) < distance, "enemy must approach player");
            enemy.transform.position = player.transform.position;
            yield return null;
            yield return null;
            Check(session.PlayerHealth.Current == 90, "contact damage once");
            Check(!session.PlayerHealth.TakeDamage(10), "immunity must block repeated damage");
            yield return new WaitForSeconds(.8f);
            Check(session.PlayerHealth.Current == 80, "contact damage resumes after immunity");
            enemy.Health.TakeDamage(100);
            enemy.Health.TakeDamage(100);
            Check(session.Kills == 1 && session.ActiveCount == 0, "enemy death counted once and pooled");
            Check(session.TrySpawn() && session.Enemies.Count == 1 && enemy.Health.Current == enemy.Health.Maximum, "pooled enemy health reset");
            while (session.ActiveCount < session.Settings.populationLimit)
                if (!session.TrySpawn()) { Check(false, "cannot fill population"); break; }
            Check(!session.TrySpawn() && session.ActiveCount == session.Settings.populationLimit, "population cap");
            int deaths = 0;
            session.PlayerHealth.Died += () => deaths++;
            yield return new WaitForSeconds(.8f);
            session.PlayerHealth.TakeDamage(1000);
            session.PlayerHealth.TakeDamage(1000);
            Check(deaths == 1 && !session.IsRunning && !player.enabled, "player death stops encounter once");
            Vector3 stopped = enemy.transform.position;
            float elapsed = session.Elapsed;
            yield return new WaitForSeconds(.2f);
            Check(enemy.transform.position == stopped && session.Elapsed == elapsed, "death freezes enemies and timer");
            session.Restart();
            Check(session.ActiveCount == 0 && session.Kills == 0 && session.Elapsed == 0 && session.PlayerHealth.Current == 100 && player.enabled, "restart clears encounter state");
            Check(session.PlayerHealth.TakeDamage(1), "restart clears immunity");
            session.Restart();
            session.TrySpawn();
            session.TrySpawn();
            var near = session.Enemies[0];
            var far = session.Enemies[1];
            near.enabled = far.enabled = false;
            near.transform.position = new Vector3(0, 1, 4);
            far.transform.position = new Vector3(0, 1, 6);
            weapons.enabled = true;
            yield return new WaitForSeconds(.28f);
            Check(weapons.ShotsFired == 1 && near.Health.Current == 15 && far.Health.Current == 30, "pistol hits nearest enemy only");
            weapons.SwitchWeapon();
            yield return new WaitForSeconds(.03f);
            Check(weapons.ShotsFired == 1, "switch must preserve shared fire interval");
            weapons.SwitchWeapon();
            weapons.SwitchWeapon();
            weapons.SwitchWeapon();
            yield return new WaitForSeconds(.03f);
            Check(weapons.ShotsFired == 1, "repeated switches must not bypass cooldown");
            yield return new WaitForSeconds(.35f);
            Check(near.Health.IsDead && session.Kills == 1 && far.Health.Current == 30, "pistol second hit kills without piercing");
            session.Restart();
            weapons.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                session.TrySpawn();
                session.Enemies[i].enabled = false;
                session.Enemies[i].transform.position = new Vector3((i - 1) * 1.456f, 1, 4);
            }
            weapons.SwitchWeapon();
            weapons.enabled = true;
            yield return new WaitForSeconds(.05f);
            Check(weapons.ActiveSlot == 1 && weapons.ShotsFired == 1 && weapons.ActiveProjectiles == 5, "shotgun emits five pellets");
            yield return new WaitForSeconds(.3f);
            Check(session.Enemies[0].Health.Current < 30 && session.Enemies[1].Health.Current < 30 && session.Enemies[2].Health.Current < 30, "shotgun spread hits multiple enemies");
            session.Restart();
            session.TrySpawn();
            session.Enemies[0].enabled = false;
            session.Enemies[0].transform.position = new Vector3(0, 1, 20);
            yield return new WaitForSeconds(.2f);
            Check(weapons.ShotsFired == 0, "out-of-range targets must not trigger fire");
            session.Restart();
            yield return new WaitForSeconds(.2f);
            Check(weapons.ShotsFired == 0 && weapons.ActiveProjectiles == 0, "empty arena must not fire");
            session.TrySpawn();
            session.Enemies[0].enabled = false;
            session.Enemies[0].transform.position = new Vector3(0, 1, 10);
            keyboard = InputSystem.AddDevice<Keyboard>();
            Vector3 movingStart = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            yield return new WaitForSeconds(.05f);
            // Scan may be up to 150ms away.
            yield return new WaitForSeconds(.15f);
            Check(weapons.ActiveProjectiles > 0, "projectile must remain in flight before reset");
            Check(player.transform.position.x < movingStart.x && weapons.ShotsFired > 0, "movement and automatic fire work together");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            yield return null;
            Check(weapons.ActiveSlot == 1, "Space input switches weapon");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.RemoveDevice(keyboard);
            session.PlayerHealth.TakeDamage(1000);
            int shotsAtDeath = weapons.ShotsFired;
            yield return new WaitForSeconds(.2f);
            Check(weapons.ShotsFired == shotsAtDeath, "death stops firing");
            session.Restart();
            Check(weapons.ActiveSlot == 0 && weapons.ShotsFired == 0 && weapons.ActiveProjectiles == 0, "restart clears weapon and projectile state");
            Debug.Log(failed ? "WEAPON VERIFICATION FAILED" : "WEAPON VERIFICATION PASSED");
            Debug.Log(failed ? "ENCOUNTER VERIFICATION FAILED" : "ENCOUNTER VERIFICATION PASSED");
            Debug.Log(failed ? "MOVEMENT VERIFICATION FAILED" : "MOVEMENT VERIFICATION PASSED");
            EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
