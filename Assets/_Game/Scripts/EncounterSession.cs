using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieSurvivor
{
    public sealed class EncounterSession : MonoBehaviour
    {
        public PlayerController Player;
        public Camera ArenaCamera;
        public EnemyController EnemyPrefab;
        public EnemySettings Settings;
        public Health PlayerHealth { get; private set; }
        public bool IsRunning { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public IReadOnlyList<EnemyController> Enemies => enemies;
        public int ActiveCount
        {
            get { int count = 0; foreach (var enemy in enemies) if (enemy.gameObject.activeSelf) count++; return count; }
        }
        private readonly List<EnemyController> enemies = new List<EnemyController>();
        private float nextSpawn;

        private void Start()
        {
            PlayerHealth = Player.GetComponent<Health>();
            PlayerHealth.Died += OnPlayerDeath;
            Restart();
        }
        private void OnDestroy() { if (PlayerHealth != null) PlayerHealth.Died -= OnPlayerDeath; }
        private void OnPlayerDeath() { IsRunning = false; Player.enabled = false; }

        public void Restart()
        {
            foreach (var enemy in enemies) enemy.gameObject.SetActive(false);
            Player.transform.SetPositionAndRotation(Vector3.up, Quaternion.identity);
            PlayerHealth.ResetHealth(100, .75f);
            Player.enabled = true;
            Elapsed = 0;
            Kills = 0;
            nextSpawn = Settings.spawnInterval;
            IsRunning = true;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
                return;
            }
            Elapsed += Time.deltaTime;
            nextSpawn -= Time.deltaTime;
            if (nextSpawn <= 0)
            {
                TrySpawn();
                nextSpawn = Mathf.Max(.1f, Settings.spawnInterval);
            }
        }

        public bool TrySpawn()
        {
            if (!IsRunning || ActiveCount >= Settings.populationLimit) return false;
            var planes = GeometryUtility.CalculateFrustumPlanes(ArenaCamera);
            for (int attempt = 0; attempt < 128; attempt++)
            {
                Vector3 point = new Vector3(Random.Range(-24f, 24f), 1, Random.Range(-24f, 24f));
                if ((point - Player.transform.position).sqrMagnitude < Settings.minimumSpawnDistance * Settings.minimumSpawnDistance) continue;
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
                available.Spawn(this, Settings, point);
                return true;
            }
            return false;
        }
        public void RegisterKill() { if (IsRunning) Kills++; }

        private void OnGUI()
        {
            if (PlayerHealth == null) return;
            GUI.Label(new Rect(16, 50, 400, 30), $"HP {PlayerHealth.Current}/{PlayerHealth.Maximum}   Enemies {ActiveCount}   Kills {Kills}");
            if (!IsRunning)
            {
                GUI.Box(new Rect(Screen.width / 2f - 120, Screen.height / 2f - 60, 240, 120), $"Defeated - {Elapsed:F1}s");
                if (GUI.Button(new Rect(Screen.width / 2f - 90, Screen.height / 2f, 180, 40), "Restart (R)")) Restart();
            }
        }
    }
}
