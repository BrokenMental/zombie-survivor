using UnityEngine;

namespace ZombieSurvivor
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        private readonly Vector3 offset = new Vector3(0, 18, -12);
        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(-offset);
        }
    }
}
