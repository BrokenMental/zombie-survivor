using UnityEngine;

namespace ZombieSurvivor
{
    [CreateAssetMenu(menuName = "Zombie Survivor/Weapon Settings")]
    public sealed class WeaponSettings : ScriptableObject
    {
        public string displayName = "Pistol";
        [Min(1)] public int damage = 15;
        [Min(.05f)] public float interval = .45f;
        [Min(.1f)] public float range = 14;
        [Min(.1f)] public float projectileSpeed = 25;
        [Min(1)] public int pellets = 1;
        [Range(0, 180)] public float spread = 0;
    }
}
