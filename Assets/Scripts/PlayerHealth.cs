using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// The player's health. Enemy bolts test themselves against two spheres (head and torso) that follow the
    /// headset, so leaning or ducking really dodges. At zero health the screen fades to red and, after a few
    /// seconds, the whole fight restarts (health, ring energy, dummy).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        public RingRig rig;
        public RingEnergy energy;
        public RingController controller;
        public ShieldMove shield;

        [Header("Stats")]
        public float maxHealth = 100f;
        [Tooltip("Seconds between 0 health and the fight restarting.")]
        public float restartDelay = 3f;

        [Header("Hit area")]
        [Tooltip("How far below the headset (meters) the torso sphere sits. Enemy bolts aim here.")]
        public float torsoDrop = 0.4f;
        public float torsoRadius = 0.18f;
        public float headRadius = 0.13f;
        [Tooltip("Damage multiplier when an enemy bolt hits your head.")]
        public float headshotMultiplier = 1.5f;

        public float Health { get; private set; }
        public float Fraction { get { return maxHealth > 0f ? Mathf.Clamp01(Health / maxHealth) : 0f; } }
        public bool IsAlive { get; private set; }

        /// <summary>False until the player has picked the ring hand, so the dummy doesn't shoot at the menu prompt.</summary>
        public bool FightStarted { get; set; }

        /// <summary>1 right after a hit, fading to 0. The HUD draws the red edge flash from it.</summary>
        public float HitFlash { get; private set; }

        /// <summary>0 normally; rises to 1 while dead (the HUD fades the screen).</summary>
        public float DeathFade { get; private set; }

        private float deathTime;

        public Vector3 ChestPoint
        {
            get { return HeadPosition + Vector3.down * torsoDrop; }
        }

        public Vector3 HeadPoint { get { return HeadPosition; } }

        private Vector3 HeadPosition
        {
            get { return rig != null && rig.head != null ? rig.head.transform.position : new Vector3(0f, 1.6f, 0f); }
        }

        private void Awake()
        {
            Instance = this;
            Health = maxHealth;
            IsAlive = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Does a bolt travelling from 'a' to 'b' touch the head or torso? 'point' is where.</summary>
        public bool SegmentHits(Vector3 a, Vector3 b, float boltRadius, out Vector3 point, out bool head)
        {
            point = b;
            head = false;
            if (!IsAlive) return false;
            Vector3 headPos = HeadPosition;
            Vector3 c1, c2;
            float d1 = Dist(headPos, a, b, out c1);
            float d2 = Dist(ChestPoint, a, b, out c2);
            bool hitHead = d1 <= headRadius + boltRadius;
            bool hitTorso = d2 <= torsoRadius + boltRadius;
            if (!hitHead && !hitTorso) return false;
            head = hitHead && (!hitTorso || d1 - headRadius < d2 - torsoRadius);
            point = head ? c1 : c2;
            return true;
        }

        private static float Dist(Vector3 p, Vector3 a, Vector3 b, out Vector3 closest)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            closest = a + ab * t;
            return (p - closest).magnitude;
        }

        public void Hit(float amount, Vector3 point, bool headshot = false)
        {
            if (!IsAlive) return;
            if (headshot) amount *= headshotMultiplier;
            Health = Mathf.Max(0f, Health - amount);
            HitFlash = 1f;
            if (rig != null)
            {
                if (rig.leftHand != null) rig.leftHand.Haptic(0.9f, 0.2f);
                if (rig.rightHand != null) rig.rightHand.Haptic(0.9f, 0.2f);
            }
            Impact.Spark(point, new Color(1f, 0.25f, 0.2f), 0.3f);
            MoveLog.Add((headshot ? "HEADSHOT! " : "") + "YOU WERE HIT for " + Mathf.RoundToInt(amount) + " (health " + Mathf.RoundToInt(Health) + ")");
            if (Health <= 0f) Die();
        }

        private void Die()
        {
            IsAlive = false;
            deathTime = Time.time;
            MoveLog.Add("You were knocked out. Restarting...");
            if (controller != null) controller.CancelAllMoves();
        }

        private void Update()
        {
            if (HitFlash > 0f) HitFlash = Mathf.Max(0f, HitFlash - Time.deltaTime * 2.5f);

            if (IsAlive)
            {
                DeathFade = Mathf.MoveTowards(DeathFade, 0f, Time.deltaTime * 2f);
                return;
            }

            float since = Time.time - deathTime;
            DeathFade = Mathf.Clamp01(since / 0.8f);
            if (since >= restartDelay) RestartFight();
        }

        public void RestartFight()
        {
            Health = maxHealth;
            IsAlive = true;
            HitFlash = 0f;
            if (energy != null) energy.Refill();
            EnemyBolt.ClearAll();
            PlayerMovement mover = FindAnyObjectByType<PlayerMovement>();
            if (mover != null) mover.ResetPosition();
            TrainingDummy[] dummies = FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None);
            for (int i = 0; i < dummies.Length; i++) dummies[i].ResetFight();
            MoveLog.Add("Fight restarted");
        }
    }
}
