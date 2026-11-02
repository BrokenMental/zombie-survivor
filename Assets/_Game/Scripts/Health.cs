using System;
using UnityEngine;

namespace ZombieSurvivor
{
    public sealed class Health : MonoBehaviour
    {
        public int Current { get; private set; }
        public int Maximum { get; private set; }
        public bool IsDead => Current <= 0;
        public event Action Died;
        private float invulnerability;
        private float nextDamageTime;

        public void ResetHealth(int maximum, float immunitySeconds)
        {
            Maximum = Mathf.Max(1, maximum);
            Current = Maximum;
            invulnerability = Mathf.Max(0, immunitySeconds);
            nextDamageTime = float.NegativeInfinity;
        }

        public bool TakeDamage(int damage)
        {
            if (damage <= 0 || IsDead || Time.time < nextDamageTime) return false;
            Current = Mathf.Max(0, Current - damage);
            nextDamageTime = Time.time + invulnerability;
            if (IsDead) Died?.Invoke();
            return true;
        }
    }
}
