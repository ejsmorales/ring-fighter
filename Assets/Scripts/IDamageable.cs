using UnityEngine;

namespace RingFighter
{
    /// <summary>Anything the ring's attacks can hurt: the training dummy now, real opponents later.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount, Vector3 point, Vector3 force);
    }
}
