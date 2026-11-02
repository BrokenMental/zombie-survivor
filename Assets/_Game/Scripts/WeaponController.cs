using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieSurvivor
{
    public sealed class WeaponController : MonoBehaviour
    {
        public EncounterSession Session;
        public WeaponSettings Pistol;
        public WeaponSettings Shotgun;
        public Projectile ProjectilePrefab;
        public Transform GunVisual;
        public int ActiveSlot { get; private set; }
        public int ShotsFired { get; private set; }
        public int ActiveProjectiles
        {
            get { int count = 0; foreach (var bullet in bullets) if (bullet.gameObject.activeSelf) count++; return count; }
        }
        public WeaponSettings ActiveWeapon => ActiveSlot == 0 ? Pistol : Shotgun;
        private readonly List<Projectile> bullets = new List<Projectile>();
        private readonly float[] readyAt = new float[2];
        private float nextShot;
        private float nextScan;
        private EnemyController target;
        private Vector3 aim = Vector3.forward;

        public void ResetWeapons()
        {
            foreach (var bullet in bullets) bullet.gameObject.SetActive(false);
            ActiveSlot = 0;
            ShotsFired = 0;
            readyAt[0] = readyAt[1] = nextShot = nextScan = 0;
            target = null;
            aim = Vector3.forward;
        }
        public void SwitchWeapon()
        {
            if (!Session.IsRunning) return;
            ActiveSlot = 1 - ActiveSlot;
            target = null;
            nextScan = 0;
            // Both the fired weapon's cooldown and the shared fire gate survive switching.
        }
        private void Update()
        {
            if (!Session.IsRunning) return;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) SwitchWeapon();
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + .15f;
                target = null;
                float nearest = ActiveWeapon.range * ActiveWeapon.range;
                foreach (var enemy in Session.Enemies)
                {
                    if (!enemy.gameObject.activeSelf || enemy.Health.IsDead) continue;
                    float distance = (enemy.transform.position - transform.position).sqrMagnitude;
                    if (distance <= nearest) { nearest = distance; target = enemy; }
                }
            }
            if (target == null || !target.gameObject.activeSelf || target.Health.IsDead) return;
            Vector3 delta = target.transform.position - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude > ActiveWeapon.range * ActiveWeapon.range) return;
            if (delta.sqrMagnitude > .001f) aim = delta.normalized;
            if (Time.time < nextShot || Time.time < readyAt[ActiveSlot]) return;
            var settings = ActiveWeapon;
            for (int i = 0; i < settings.pellets; i++)
            {
                float angle = settings.pellets == 1 ? 0 : Mathf.Lerp(-settings.spread / 2, settings.spread / 2, (float)i / (settings.pellets - 1));
                Projectile bullet = null;
                foreach (var candidate in bullets) if (!candidate.gameObject.activeSelf) { bullet = candidate; break; }
                if (bullet == null) { bullet = Instantiate(ProjectilePrefab, Session.transform); bullets.Add(bullet); }
                bullet.Launch(Session, transform.position, Quaternion.Euler(0, angle, 0) * aim, settings);
            }
            ShotsFired++;
            nextShot = readyAt[ActiveSlot] = Time.time + settings.interval;
        }
        private void LateUpdate()
        {
            if (GunVisual != null) GunVisual.rotation = Quaternion.LookRotation(aim);
        }
        private void OnGUI()
        {
            if (Session == null || !Session.IsRunning) return;
            using (new PrototypeGUI(true))
                if (GUI.Button(new Rect(PrototypeGUI.Width - 190, PrototypeGUI.Height - 100, 175, 70), ActiveWeapon.displayName + "\nSwitch (Space)")) SwitchWeapon();
        }
    }
}
