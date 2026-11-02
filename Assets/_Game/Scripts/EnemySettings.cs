using UnityEngine;

namespace ZombieSurvivor
{
    [CreateAssetMenu(menuName = "Zombie Survivor/Enemy Settings")]
    public sealed class EnemySettings : ScriptableObject
    {
        [Min(1)] public int health = 30;
        [Min(0)] public float speed = 2f;
        [Min(1)] public int contactDamage = 10;
        [Min(.1f)] public float contactRadius = 1f;
        [Min(.1f)] public float spawnInterval = 1.5f;
        [Min(1)] public int populationLimit = 30;
        [Min(1)] public float minimumSpawnDistance = 8f;
    }
}
