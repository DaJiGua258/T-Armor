using UnityEngine;

namespace QFramework.Event
{
    public class WeaponEvent
    {
        public struct GetTargetRig
        {
            public Rigidbody2D TargetRig;
        }

        public struct UpdateBulletLayerMask
        {
            public LayerMask LayerMask;
        }
    }
}