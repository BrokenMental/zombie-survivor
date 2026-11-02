using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieSurvivor
{
    public sealed class EncounterSession : MonoBehaviour
    {
        public enum SessionState { Ready, Playing, Won, Lost }
        public const float SurvivalDuration = 180;
        public PlayerController Player;
        public Camera ArenaCamera;
        public EnemyController EnemyPrefab;
        public EnemySettings Settings;
        public EnemySettings FastSettings;
        public WaveSettings Waves;
        public WeaponController Weapons;
        public Health PlayerHealth { get; private set; }
        public SessionState State { get; private set; }
        public bool IsRunning => State == SessionState.Playing;
        public WaveSettings.Phase CurrentWave => Waves.At(Elapsed);
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public IReadOnlyList<EnemyController> Enemies => enemies;
        public int ActiveCount
        {
            get { int count = 0; foreach (var enemy in enemies) if (enemy.gameObject.activeSelf) count++; return count; }
        }
        private readonly List<EnemyController> enemies = new List<EnemyController>();
        private float nextSpawn;
        private int spawnCount;

        private void Start()
        {
            Application.targetFrameRate = 60;
            PlayerHealth = Player.GetComponent<Health>();
            PlayerHealth.Died += OnPlayerDeath;
            ResetEncounter(false);
        }
        private void OnDestroy() { if (PlayerHealth != null) PlayerHealth.Died -= OnPlayerDeath; }
        private void OnPlayerDeath() => Finish(SessionState.Lost);

        private void Finish(SessionState result)
        {
            if (!IsRunning) return;
            State = result;
            Player.enabled = false;
        }

        public void Restart() => ResetEncounter(true);
        public void ReturnToTitle() => ResetEncounter(false);

        private void ResetEncounter(bool play)
        {
            foreach (var enemy in enemies) enemy.gameObject.SetActive(false);
            Player.transform.SetPositionAndRotation(Vector3.up, Quaternion.identity);
            PlayerHealth.ResetHealth(100, .75f);
            Player.enabled = play;
            Elapsed = 0;
            Kills = 0;
            nextSpawn = Waves.At(0).spawnInterval;
            spawnCount = 0;
            State = play ? SessionState.Playing : SessionState.Ready;
            if (Weapons != null) Weapons.ResetWeapons();
        }

        private void Update()
        {
            if (!IsRunning)
            {
                if (Keyboard.current != null)
                {
                    if (State == SessionState.Ready && Keyboard.current.enterKey.wasPressedThisFrame) Restart();
                    else if (State != SessionState.Ready && Keyboard.current.rKey.wasPressedThisFrame) Restart();
                }
                return;
            }
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (!IsRunning || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            var previousWave = CurrentWave;
            Elapsed = Mathf.Min(SurvivalDuration, Elapsed + deltaTime);
            if (PlayerHealth.IsDead) { Finish(SessionState.Lost); return; }
            if (Elapsed >= SurvivalDuration) { Finish(SessionState.Won); return; }
            nextSpawn -= deltaTime;
            if (previousWave != CurrentWave) nextSpawn = Mathf.Min(nextSpawn, CurrentWave.spawnInterval);
            if (nextSpawn <= 0)
            {
                TrySpawn();
                nextSpawn = Mathf.Max(.1f, CurrentWave.spawnInterval);
            }
        }

        public bool TrySpawn()
        {
            if (!IsRunning || ActiveCount >= CurrentWave.populationLimit) return false;
            var definition = CurrentWave.fastEvery > 0 && (spawnCount + 1) % CurrentWave.fastEvery == 0 ? FastSettings : Settings;
            var planes = GeometryUtility.CalculateFrustumPlanes(ArenaCamera);
            for (int attempt = 0; attempt < 128; attempt++)
            {
                Vector3 point = new Vector3(Random.Range(-24f, 24f), 1, Random.Range(-24f, 24f));
                if ((point - Player.transform.position).sqrMagnitude < definition.minimumSpawnDistance * definition.minimumSpawnDistance) continue;
                // Check the complete capsule, including a margin, against camera frustum planes.
                var bounds = new Bounds(point, new Vector3(1.5f, 2.5f, 1.5f));
                if (GeometryUtility.TestPlanesAABB(planes, bounds)) continue;
                EnemyController available = null;
                foreach (var enemy in enemies) if (!enemy.gameObject.activeSelf) { available = enemy; break; }
                if (available == null)
                {
                    available = Instantiate(EnemyPrefab, transform);
                    enemies.Add(available);
                }
                available.Spawn(this, definition, point);
                spawnCount++;
                return true;
            }
            return false;
        }
        public void RegisterKill() { if (IsRunning) Kills++; }

        private void OnGUI()
        {
            if (PlayerHealth == null) return;
            using (new PrototypeGUI(true))
                DrawHUD();
        }

        private void DrawHUD()
        {
            float width = PrototypeGUI.Width;
            float height = PrototypeGUI.Height;
            if (State == SessionState.Ready)
            {
                GUI.Box(new Rect(width / 2f - 140, height / 2f - 100, 280, 190), "Zombie Survivor\nSurvive for 3 minutes\nMove: WASD / left touch drag\nSwitch weapon: Space / right button");
                if (GUI.Button(new Rect(width / 2f - 100, height / 2f + 25, 200, 45), "Start (Enter)")) Restart();
                return;
            }
            GUI.Label(new Rect(16, 50, 448, 50), $"HP {PlayerHealth.Current}/{PlayerHealth.Maximum}   Time {Elapsed:F0}/180\nEnemies {ActiveCount}   Kills {Kills}");
            if (!IsRunning)
            {
                string result = State == SessionState.Won ? "Survived!" : "Defeated";
                GUI.Box(new Rect(width / 2f - 140, height / 2f - 100, 280, 210), $"{result}\nTime {Elapsed:F1}s   Kills {Kills}");
                if (GUI.Button(new Rect(width / 2f - 100, height / 2f - 25, 200, 45), "Restart (R)")) Restart();
                if (GUI.Button(new Rect(width / 2f - 100, height / 2f + 35, 200, 45), "Title")) ReturnToTitle();
            }
        }
    }
}
