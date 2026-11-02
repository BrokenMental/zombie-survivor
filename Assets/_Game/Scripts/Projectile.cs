using UnityEngine;

namespace ZombieSurvivor
{
    public sealed class Projectile : MonoBehaviour
    {
        private EncounterSession session;
        private Vector3 direction;
        private float speed;
        private float remaining;
        private int damage;

        public void Launch(EncounterSession owner, Vector3 origin, Vector3 heading, WeaponSettings settings)
        {
            session = owner;
            transform.position = origin;
            direction = heading.normalized;
            speed = settings.projectileSpeed;
            remaining = settings.range;
            damage = settings.damage;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!session.IsRunning) return;
            Vector3 start = transform.position;
            float travel = Mathf.Min(remaining, speed * Time.deltaTime);
            EnemyController closest = null;
            float closestDistance = travel + 1;
            foreach (var enemy in session.Enemies)
            {
                if (!enemy.gameObject.activeSelf || enemy.Health.IsDead) continue;
                // Swept sphere test: fast bullets cannot skip enemies between frames.
                Vector3 relative = enemy.transform.position - start;
                float projection = Vector3.Dot(relative, direction);
                float perpendicular = relative.sqrMagnitude - projection * projection;
                const float radius = .6f;
                if (perpendicular > radius * radius) continue;
                float halfChord = Mathf.Sqrt(Mathf.Max(0, radius * radius - perpendicular));
                if (projection + halfChord < 0) continue;
                float hitDistance = Mathf.Max(0, projection - halfChord);
                if (hitDistance <= travel && hitDistance < closestDistance)
                { closest = enemy; closestDistance = hitDistance; }
            }
            if (closest != null)
            {
                transform.position = start + direction * closestDistance;
                closest.Health.TakeDamage(damage);
                gameObject.SetActive(false);
                return;
            }
            transform.position = start + direction * travel;
            remaining -= travel;
            if (remaining <= 0) gameObject.SetActive(false);
        }
    }
}
