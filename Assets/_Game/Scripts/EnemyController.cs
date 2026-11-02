using UnityEngine;

namespace ZombieSurvivor
{
    [RequireComponent(typeof(Health))]
    public sealed class EnemyController : MonoBehaviour
    {
        public Health Health { get; private set; }
        private EncounterSession session;
        private EnemySettings settings;
        public EnemySettings Definition => settings;
        private MaterialPropertyBlock appearance;
        private bool alive;

        private void Awake()
        {
            Health = GetComponent<Health>();
            Health.Died += OnDeath;
        }
        private void OnDestroy() { if (Health != null) Health.Died -= OnDeath; }

        public void Spawn(EncounterSession owner, EnemySettings data, Vector3 position)
        {
            session = owner;
            settings = data;
            transform.position = position;
            Health.ResetHealth(data.health, 0);
            alive = true;
            if (appearance == null) appearance = new MaterialPropertyBlock();
            appearance.SetColor("_BaseColor", data.color);
            GetComponent<Renderer>().SetPropertyBlock(appearance);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!alive || session == null || !session.IsRunning) return;
            Vector3 delta = session.Player.transform.position - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude > .01f)
            {
                Vector3 separation = Vector3.zero;
                foreach (var other in session.Enemies)
                {
                    if (other == this || !other.gameObject.activeSelf) continue;
                    Vector3 away = transform.position - other.transform.position;
                    away.y = 0;
                    float distance = away.sqrMagnitude;
                    if (distance > .001f && distance < 1f) separation += away.normalized * (1f - distance);
                }
                Vector3 direction = Vector3.ClampMagnitude(delta.normalized + separation, 1);
                transform.position += direction * (settings.speed * Time.deltaTime);
                var p = transform.position;
                transform.position = new Vector3(Mathf.Clamp(p.x, -24, 24), 1, Mathf.Clamp(p.z, -24, 24));
                transform.forward = delta.normalized;
            }
            if (delta.sqrMagnitude <= settings.contactRadius * settings.contactRadius)
                session.PlayerHealth.TakeDamage(settings.contactDamage);
        }

        private void OnDeath()
        {
            if (!alive) return;
            alive = false;
            session.RegisterKill();
            gameObject.SetActive(false);
        }
    }
}
