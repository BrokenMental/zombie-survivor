using System;
using UnityEngine;

namespace ZombieSurvivor
{
    [CreateAssetMenu(menuName = "Zombie Survivor/Wave Settings")]
    public sealed class WaveSettings : ScriptableObject
    {
        [Serializable]
        public sealed class Phase
        {
            [Min(0)] public float startsAt;
            [Min(.1f)] public float spawnInterval = 1.5f;
            [Min(1)] public int populationLimit = 30;
            [Min(0)] public int fastEvery; // 0: normal only; N: every Nth spawn is fast.
        }
        public Phase[] phases =
        {
            new Phase(),
            new Phase { startsAt = 60, spawnInterval = .9f, populationLimit = 60, fastEvery = 3 },
            new Phase { startsAt = 120, spawnInterval = .5f, populationLimit = 100, fastEvery = 2 }
        };

        public Phase At(float elapsed)
        {
            Phase selected = phases[0];
            foreach (var phase in phases)
                if (phase.startsAt <= elapsed && phase.startsAt >= selected.startsAt) selected = phase;
            return selected;
        }
    }
}
