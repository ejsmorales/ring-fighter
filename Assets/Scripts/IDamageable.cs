using UnityEngine;

namespace RingFighter
{
    /// <summary>What kind of damage this is. Only Direct hits (bolts, blasts, the laser) can be headshots.</summary>
    public enum DamageKind { Direct, Area, Fall }

    /// <summary>Anything the ring's attacks can hurt: the training dummy now, real opponents later.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        /// <summary>False while attacks pass through it (knocked out, or the enemy's evade/teleport flicker).</summary>
        bool IsTargetable { get; }
        void TakeDamage(float amount, Vector3 point, Vector3 force);
        void TakeDamage(float amount, Vector3 point, Vector3 force, DamageKind kind);

        /// <summary>The whirlwind is touching this target for 'deltaTime' seconds (lifts it, slows its attacks).</summary>
        void OnWhirlwind(float deltaTime);

        /// <summary>A nearby whirlwind drags this target toward 'center' at up to 'speed' m/s this frame.</summary>
        void OnPull(Vector3 center, float speed, float deltaTime);
    }
}
