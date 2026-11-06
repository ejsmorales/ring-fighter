using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// The opponent's decision making (a "duelist"). TrainingDummy is the body (health, shooting, animations);
    /// this decides what the body does:
    ///   - Movement: keeps a preferred distance from you (backs off if you close in, approaches if you run) and
    ///     circles around you, changing direction and pausing at random so he is hard to predict.
    ///   - Dodging: sometimes sidesteps a bolt or blast you fire at him.
    ///   - Punishing openings: shoots sooner when your ring energy is low or right after your big attacks
    ///     (charged blast, laser, whirlwind).
    ///   - Teleport: every 12-20 s, or to escape when you charge a blast or hurt him a lot quickly, he flickers
    ///     (warning), vanishes and reappears 60-90 degrees to your left or right (never behind), then fires a quick bolt.
    /// All numbers are public so they can be tuned in the inspector.
    /// </summary>
    [RequireComponent(typeof(TrainingDummy))]
    public class EnemyBrain : MonoBehaviour
    {
        [Header("Duelist movement")]
        [Tooltip("Distance (meters) he tries to keep from you.")]
        public float preferredDistance = 5.5f;
        [Tooltip("He doesn't bother adjusting distance while within this much (meters) of the preferred distance.")]
        public float distanceTolerance = 1f;
        [Tooltip("Speed (m/s) when approaching or backing off.")]
        public float approachSpeed = 1.2f;
        [Tooltip("Speed (m/s) when circling around you.")]
        public float circleSpeed = 0.8f;
        [Tooltip("He switches circling direction every this many seconds (random between min and max).")]
        public Vector2 circleFlipEvery = new Vector2(3f, 6f);
        [Tooltip("Chance (0-1) that a direction change is a short stop instead.")]
        [Range(0f, 1f)] public float pauseChance = 0.35f;
        [Tooltip("He always backs off if you get closer than this (meters).")]
        public float minDistance = 2.5f;
        [Tooltip("How quickly he changes speed (m/s per second). Lower = heavier, more readable movement.")]
        public float acceleration = 3f;
        [Tooltip("He moves at this fraction of his speed while charging a shot, so the shot is easier to read.")]
        [Range(0f, 1f)] public float chargingSpeedFactor = 0.4f;

        [Header("Dodging your shots")]
        [Tooltip("Chance (0-1) that he sidesteps a shot aimed at him.")]
        [Range(0f, 1f)] public float dodgeChance = 0.4f;
        [Tooltip("Seconds between you firing and him starting to move.")]
        public float dodgeReaction = 0.06f;
        [Tooltip("How far (meters) a sidestep goes.")]
        public float dodgeDistance = 1.3f;
        [Tooltip("How long (seconds) a sidestep takes.")]
        public float dodgeTime = 0.22f;
        [Tooltip("After deciding about one shot, ignore further shots for this long (seconds).")]
        public float dodgeCooldown = 1.5f;

        [Header("Punishing openings")]
        [Tooltip("Your ring energy below this fraction (0-1) counts as an opening.")]
        [Range(0f, 1f)] public float lowEnergyFraction = 0.2f;
        [Tooltip("Seconds after your charged blast, laser or whirlwind ends that count as an opening.")]
        public float afterBigMoveWindow = 1.2f;
        [Tooltip("Seconds between two punishes.")]
        public float punishCooldown = 5f;

        [Header("Teleport")]
        [Tooltip("He teleports on his own every this many seconds (random between min and max).")]
        public Vector2 teleportEvery = new Vector2(15f, 25f);
        [Tooltip("Never two teleports closer together than this (seconds), even to escape.")]
        public float minTeleportGap = 6f;
        [Tooltip("He also teleports away if he takes this much damage within 'Escape Window' seconds...")]
        public float escapeDamage = 35f;
        public float escapeWindow = 2f;
        [Tooltip("...or when you start charging a blast.")]
        public bool escapeFromBlast = true;
        [Tooltip("Where he reappears: this many degrees left or right of where you face, so he stays in your side vision.")]
        public Vector2 flankAngle = new Vector2(35f, 55f);
        [Tooltip("He never reappears further than this many degrees from where you face (checked after keeping him in the arena).")]
        public float maxReappearAngle = 60f;

        /// <summary>What he's doing, for the debug panel.</summary>
        public string State { get; private set; }

        private TrainingDummy dummy;
        private Vector3 velocity;
        private float circleDir = 1f;
        private float nextFlip;
        private bool paused;

        private float nextDodgeAllowed;
        private float dodgeStartAt = -1f;
        private float dodgeUntil = -1f;
        private Vector3 dodgeDir;

        private bool bigMoveWas;
        private float lastBigMoveEnded = -100f;
        private float nextPunish;

        private bool fightWas;
        private float nextTeleport;
        private float lastTeleport = -100f;
        private readonly List<Vector2> recentDamage = new List<Vector2>();   // x = time, y = amount

        private void Awake()
        {
            dummy = GetComponent<TrainingDummy>();
            dummy.brainControlled = true;
            State = "waiting";
        }

        private void OnEnable()
        {
            Projectile.Spawned += OnPlayerShot;
            if (dummy != null) dummy.DamageTaken += OnDamage;
        }

        private void OnDisable()
        {
            Projectile.Spawned -= OnPlayerShot;
            if (dummy != null) dummy.DamageTaken -= OnDamage;
        }

        private void OnDamage(float amount)
        {
            recentDamage.Add(new Vector2(Time.time, amount));
        }

        private float RecentDamage()
        {
            float sum = 0f;
            for (int i = recentDamage.Count - 1; i >= 0; i--)
            {
                if (Time.time - recentDamage[i].x > escapeWindow) recentDamage.RemoveAt(i);
                else sum += recentDamage[i].y;
            }
            return sum;
        }

        // ---- Dodging ----

        private void OnPlayerShot(Projectile p)
        {
            if (p == null || dummy == null || !dummy.CanAct || Time.time < nextDodgeAllowed) return;
            Vector3 to = dummy.ChestPoint - p.transform.position;
            float d = to.magnitude;
            if (d < 1f || d > 25f) return;
            if (Vector3.Dot(p.transform.forward, to / d) < 0.97f) return;   // not aimed at him

            nextDodgeAllowed = Time.time + dodgeCooldown;
            if (Random.value > dodgeChance) return;

            Vector3 side = Vector3.Cross(Vector3.up, p.transform.forward);
            side.y = 0f;
            if (side.sqrMagnitude < 0.0001f) return;
            side.Normalize();
            if (Random.value < 0.5f) side = -side;
            Vector3 landing = dummy.GroundPosition + side * dodgeDistance;
            if (!dummy.InsideArena(landing, 0.5f)) side = -side;   // don't dodge into the edge

            dodgeDir = side;
            dodgeStartAt = Time.time + dodgeReaction;
            dodgeUntil = dodgeStartAt + Mathf.Max(0.05f, dodgeTime);
        }

        // ---- Every frame ----

        private void Update()
        {
            PlayerHealth player = PlayerHealth.Instance;
            bool fighting = player != null && player.IsAlive && player.FightStarted && dummy.IsAlive;
            if (!fighting || !dummy.CanAct)
            {
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, acceleration * 2f * Time.deltaTime);
                dummy.DesiredVelocity = velocity;
                dodgeUntil = -1f;
                fightWas = fighting;
                State = !fighting ? "waiting" : (dummy.IsTeleporting ? "TELEPORTING" : (dummy.IsAirborne ? "in the air" : "getting up"));
                return;
            }
            if (!fightWas)
            {
                fightWas = true;
                nextTeleport = Time.time + Random.Range(teleportEvery.x, teleportEvery.y);
                nextFlip = Time.time + Random.Range(circleFlipEvery.x, circleFlipEvery.y);
            }

            Vector3 playerPos = player.HeadPoint;
            Vector3 me = dummy.GroundPosition;
            playerPos.y = me.y;

            if (TryTeleport(player, playerPos, me)) return;
            Punish(player);
            Steer(playerPos, me);
        }

        private void Steer(Vector3 playerPos, Vector3 me)
        {
            // Sidestep in progress: fast and straight, no smoothing.
            if (Time.time >= dodgeStartAt && Time.time < dodgeUntil)
            {
                velocity = dodgeDir * (dodgeDistance / Mathf.Max(0.05f, dodgeTime));
                dummy.DesiredVelocity = velocity;
                State = "DODGING";
                if (dodgeStartAt > 0f && Time.time - dodgeStartAt < Time.deltaTime * 1.5f)
                {
                    MoveLog.Add("Dummy dodges");
                    Impact.Label(dummy.ChestPoint + Vector3.up * 0.6f, "DODGE", new Color(1f, 0.9f, 0.4f), dummy.lookTarget);
                }
                return;
            }

            Vector3 away = me - playerPos;
            float dist = away.magnitude;
            away = dist > 0.01f ? away / dist : Vector3.forward;
            Vector3 tangent = Vector3.Cross(Vector3.up, away) * circleDir;

            if (Time.time >= nextFlip)
            {
                nextFlip = Time.time + Random.Range(circleFlipEvery.x, circleFlipEvery.y);
                paused = Random.value < pauseChance;
                if (!paused) circleDir = -circleDir;
            }

            float radial = 0f;   // + = away from you
            if (dist < minDistance) radial = approachSpeed * 1.5f;
            else if (dist < preferredDistance - distanceTolerance) radial = approachSpeed;
            else if (dist > preferredDistance + distanceTolerance) radial = -approachSpeed;

            Vector3 desired = away * radial + tangent * (paused ? 0f : circleSpeed);

            // Near the edge of the arena: turn around instead of pressing into it.
            Vector3 outward = dummy.OutwardNearEdge(me, 1f);
            if (outward != Vector3.zero)
            {
                if (Vector3.Dot(tangent, outward) > 0f) circleDir = -circleDir;
                float outwardSpeed = Vector3.Dot(desired, outward);
                if (outwardSpeed > 0f) desired -= outward * outwardSpeed;
            }

            if (dummy.IsCharging) desired *= chargingSpeedFactor;
            velocity = Vector3.MoveTowards(velocity, desired, acceleration * Time.deltaTime);
            dummy.DesiredVelocity = velocity;

            if (dummy.IsCharging) State = "aiming";
            else if (radial > 0f) State = "backing off";
            else if (radial < 0f) State = "closing in";
            else State = paused ? "watching" : "circling";
        }

        private void Punish(PlayerHealth player)
        {
            bool bigNow = false;
            RingController rc = player.controller;
            if (rc != null)
            {
                IReadOnlyList<Move> moves = rc.ActiveMoves;
                for (int i = 0; i < moves.Count; i++)
                    if (moves[i] is ChargedBlastMove || moves[i] is LaserMove || moves[i] is WhirlwindMove) bigNow = true;
            }
            if (bigMoveWas && !bigNow) lastBigMoveEnded = Time.time;
            bigMoveWas = bigNow;

            bool recharging = false;
            if (rc != null)
            {
                IReadOnlyList<Move> active = rc.ActiveMoves;
                for (int i = 0; i < active.Count; i++)
                    if (active[i] is RechargeMove) recharging = true;
            }
            bool lowEnergy = player.energy != null && player.energy.Fraction < lowEnergyFraction;
            bool afterBig = Time.time - lastBigMoveEnded < afterBigMoveWindow;
            if ((lowEnergy || afterBig || recharging) && Time.time >= nextPunish && dummy.HurryNextShot(0.15f))
            {
                nextPunish = Time.time + punishCooldown;
                string why = recharging ? "you're recharging" : (lowEnergy ? "your energy is low" : "after your big attack");
                MoveLog.Add("Dummy punishes an opening (" + why + ")");
            }
        }

        private bool TryTeleport(PlayerHealth player, Vector3 playerPos, Vector3 me)
        {
            if (Time.time - lastTeleport < minTeleportGap) return false;

            string reason = null;
            if (escapeFromBlast && player.controller != null && BlastCharging(player.controller)) reason = "escaping your blast";
            else if (RecentDamage() >= escapeDamage) reason = "escaping damage";
            else if (Time.time >= nextTeleport && !dummy.IsCharging) reason = "timer";
            if (reason == null) return false;

            // To your left or right of where you face, at his preferred distance, never behind you. Tries both
            // sides, then smaller angles and distances, so keeping him inside the arena can't push him behind you.
            Vector3 facing = player.rig != null ? player.rig.FlatForward() : Vector3.forward;
            float side = Random.value < 0.5f ? -1f : 1f;
            float baseAngle = Random.Range(flankAngle.x, flankAngle.y);
            Vector3 dest = playerPos + facing * Mathf.Min(preferredDistance, 3f);   // last resort: in front of you
            bool ok = false;
            float[] angleScale = { 1f, 1f, 0.5f, 0.5f, 0f };
            float[] distScale = { 1f, 1f, 0.75f, 0.75f, 0.6f };
            for (int attempt = 0; attempt < angleScale.Length && !ok; attempt++)
            {
                float angle = baseAngle * angleScale[attempt] * side;
                Vector3 candidate = playerPos + Quaternion.Euler(0f, angle, 0f) * facing * (preferredDistance * distScale[attempt]);
                Vector3 offset = candidate - playerPos;
                bool inside = dummy.InsideArena(candidate, 0.5f);
                bool inView = Vector3.Angle(facing, offset) <= maxReappearAngle;
                if (inside && inView)
                {
                    dest = candidate;
                    ok = true;
                }
                side = -side;   // alternate sides
            }
            if (!ok) return false;   // nowhere good (you're at the very edge): skip this teleport
            dest.y = me.y;

            bool evade = reason != "timer";
            if (!dummy.BeginTeleport(dest, !evade)) return false;
            if (evade) Impact.Label(dummy.ChestPoint + Vector3.up * 0.6f, "EVADE", new Color(1f, 0.85f, 0.3f), dummy.lookTarget, 0.18f);
            lastTeleport = Time.time;
            nextTeleport = Time.time + Random.Range(teleportEvery.x, teleportEvery.y);
            recentDamage.Clear();
            velocity = Vector3.zero;
            dummy.DesiredVelocity = Vector3.zero;
            State = "TELEPORTING";
            MoveLog.Add(evade ? "Dummy EVADES (" + reason + "): untouchable while flickering, no counter-shot" : "Dummy teleports (timer)");
            return true;
        }

        private static bool BlastCharging(RingController rc)
        {
            IReadOnlyList<Move> moves = rc.ActiveMoves;
            for (int i = 0; i < moves.Count; i++)
                if (moves[i] is ChargedBlastMove) return true;
            return false;
        }
    }
}
