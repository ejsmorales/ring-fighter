using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// A practice opponent: drifts around the arena, flinches and gets knocked back when hit,
    /// shows a health bar, falls over when its health runs out, then comes back after a few seconds.
    /// It fights back: a yellow charge-up glow, then one bolt at your chest every few seconds. Later, replace the shapes with a real character model: assign
    /// 'visualsRoot' with your own model and the placeholder shapes are skipped.
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        public float maxHealth = 200f;
        public float respawnDelay = 3.5f;

        [Header("Attack")]
        public bool attacks = true;
        [Tooltip("Seconds from one shot to the next.")]
        public float attackInterval = 3f;
        [Tooltip("Seconds of yellow glow before each shot. Lean or raise your shield in this time.")]
        public float chargeTime = 0.8f;
        public float boltSpeed = 7f;
        public float boltDamage = 10f;
        [Tooltip("Glowing yellow material for the charge-up orb and the bolt.")]
        public Material attackMaterial;

        [Tooltip("Chance (0-1) that a shot is aimed at your head instead of your chest. The glow turns orange when it is.")]
        public float headShotChance = 0.15f;

        [Header("Headshots (on the dummy)")]
        [Tooltip("Hits at least this high above the dummy's feet (meters) count as headshots.")]
        public float headMinHeight = 1.72f;
        public float headshotMultiplier = 1.5f;

        [Header("Whirlwind: lift, fall, slow")]
        [Tooltip("Seconds inside the whirlwind before it lifts the dummy.")]
        public float liftAfter = 0.15f;
        [Tooltip("How high (meters) the whirlwind lifts it.")]
        public float liftHeight = 2.2f;
        [Tooltip("Fall damage when it lands from the full lift height. Shorter falls do proportionally less.")]
        public float maxFallDamage = 40f;
        [Tooltip("Seconds after a lift before the whirlwind can lift it again.")]
        public float liftCooldown = 2.5f;
        [Tooltip("Gravity used for the fall (m/s squared). Higher = snappier.")]
        public float fallGravity = 14f;
        [Tooltip("After the whirlwind touches it, its attacks come this much slower (1.25 = 20% fewer shots)...")]
        public float slowedIntervalMultiplier = 1.25f;
        [Tooltip("...for this many seconds after the last touch.")]
        public float slowDuration = 6f;

        [Header("Brain (EnemyBrain)")]
        [Tooltip("Ticked when an EnemyBrain moves this dummy. Unticked, it wanders back and forth on its own (the old behavior).")]
        public bool brainControlled;
        [Tooltip("Half the size (meters, X and Z) of the open area he can move in, around the middle of the arena. Matches the open space inside the parked car rows.")]
        public Vector2 arenaHalfSize = new Vector2(20f, 18f);

        [Header("Teleport (started by EnemyBrain)")]
        [Tooltip("Seconds he flickers in place (the warning) before he vanishes.")]
        public float teleportShimmerTime = 1.1f;
        [Tooltip("Seconds he is invisible between the two spots.")]
        public float teleportGapTime = 0.15f;
        [Tooltip("Seconds of charge-up glow for the quick shot right after he reappears.")]
        public float teleportShotCharge = 0.35f;
        [Tooltip("Speed (m/s) of that quick shot.")]
        public float teleportBoltSpeed = 11f;

        [Header("Movement")]
        public Vector3 wanderExtents = new Vector3(2.2f, 0f, 0.8f);
        public float wanderSpeed = 0.55f;
        public Transform lookTarget;   // usually the player's head

        [Header("Look")]
        public Material bodyMaterial;
        public Material healthBarMaterial;
        public Material healthBarBackMaterial;
        public GameObject visualsRoot; // optional: your own model instead of the placeholder shapes
        [Tooltip("Tick when visualsRoot is an animated character: its own animations play knock-outs instead of the code tipping it over.")]
        public bool animatedModel;
        [Tooltip("Normal body tint. Grey for the placeholder shapes; white keeps a textured model's real colors.")]
        public Color normalTint = new Color(0.78f, 0.78f, 0.82f);

        [Header("Hand muzzle (animated model)")]
        [Tooltip("If set (the model's right hand bone), the charge-up glow and bolts come from here instead of the chest.")]
        public Transform muzzleBone;
        [Tooltip("How far in front of the hand (meters, toward the player) the muzzle sits.")]
        public float muzzleForwardOffset = 0.15f;
        [Tooltip("After getting back up from a knock-out, the dummy stands still and holds fire this long (seconds) while the get-up animation plays.")]
        public float getUpTime = 0f;

        [Header("Hit area")]
        [Tooltip("Radius (meters) of the invisible capsule that attacks test against.")]
        public float hitRadius = 0.35f;
        [Tooltip("Height (meters) of the invisible capsule that attacks test against.")]
        public float hitHeight = 2.15f;
        [Tooltip("Chest height (meters): where aim assist steers shots and reflected bolts fly.")]
        public float chestHeight = 1.25f;
        [Tooltip("Height (meters) of the floating health bar.")]
        public float healthBarHeight = 2.45f;

        public bool IsAlive { get; private set; }
        /// <summary>True during the yellow/orange charge-up glow (read by the animation driver).</summary>
        public bool IsCharging { get { return charging; } }
        /// <summary>True while a whirlwind holds it in the air.</summary>
        public bool IsAirborne { get { return air != Air.Grounded; } }
        /// <summary>True while it is getting back up after a knock-out (no moving, no shooting).</summary>
        public bool IsGettingUp { get { return Time.time < getUpUntil; } }
        /// <summary>Raised whenever a hit is shown (damage number). The animation driver plays a flinch.</summary>
        public event System.Action Damaged;
        /// <summary>Raised on every bit of damage with the amount (EnemyBrain uses it to decide to escape).</summary>
        public event System.Action<float> DamageTaken;
        /// <summary>Raised when the teleport warning starts (where he is, where he will appear).</summary>
        public event System.Action<Vector3, Vector3> TeleportStarted;
        /// <summary>Raised the moment he jumps (from, to).</summary>
        public event System.Action<Vector3, Vector3> Teleported;

        /// <summary>Velocity (m/s, flat) the brain wants him to move at. Used when brainControlled is ticked.</summary>
        public Vector3 DesiredVelocity { get; set; }
        /// <summary>Where he stands on the floor (without knockback or whirlwind height).</summary>
        public Vector3 GroundPosition { get { return groundPos; } }
        /// <summary>'p' kept inside the open area, 'margin' meters from its edges.</summary>
        public Vector3 ClampToArena(Vector3 p, float margin)
        {
            p.x = Mathf.Clamp(p.x, -arenaHalfSize.x + margin, arenaHalfSize.x - margin);
            p.z = Mathf.Clamp(p.z, -arenaHalfSize.y + margin, arenaHalfSize.y - margin);
            return p;
        }

        public bool InsideArena(Vector3 p, float margin)
        {
            return Mathf.Abs(p.x) <= arenaHalfSize.x - margin && Mathf.Abs(p.z) <= arenaHalfSize.y - margin;
        }

        /// <summary>Direction out of the arena if 'p' is within 'margin' of an edge (zero if not near one).</summary>
        public Vector3 OutwardNearEdge(Vector3 p, float margin)
        {
            Vector3 o = Vector3.zero;
            if (p.x > arenaHalfSize.x - margin) o.x = 1f;
            else if (p.x < -arenaHalfSize.x + margin) o.x = -1f;
            if (p.z > arenaHalfSize.y - margin) o.z = 1f;
            else if (p.z < -arenaHalfSize.y + margin) o.z = -1f;
            return o.normalized;
        }

        /// <summary>Height of the arena floor under him (his starting height).</summary>
        public float FloorHeight { get { return home.y; } }
        public bool IsTeleporting { get { return teleport != Teleport.None; } }
        /// <summary>Attacks pass through him while knocked out and during the teleport/evade flicker.</summary>
        public bool IsTargetable { get { return IsAlive && teleport == Teleport.None; } }
        /// <summary>True when he is free to move, dodge, shoot or teleport.</summary>
        public bool CanAct { get { return IsAlive && air == Air.Grounded && !IsGettingUp && teleport == Teleport.None; } }

        public float Health { get; private set; }

        private Vector3 home;
        private Vector3 knockOffset;
        private float flashTimer;
        private float deathTime;
        private float wanderPhase;
        private Renderer[] renderers;
        private Transform bar;
        private Transform barFill;
        private MaterialPropertyBlock block;
        private Transform visuals;
        private Transform aimPoint;
        private float pendingDamage;
        private bool pendingHeadshot;
        private float nextFireTime;
        private bool wasFighting;
        private Transform orb;
        private Renderer orbRenderer;
        private MaterialPropertyBlock orbBlock;
        private bool charging;
        private bool chargeAimsHead;

        // whirlwind effects
        private enum Air { Grounded, Rising, Falling }
        private Air air = Air.Grounded;
        private float airHeight;
        private float airVelocity;
        private float windContact;
        private float windSeen = -10f;
        private float liftReadyTime;
        private float slowedUntil = -1f;
        private float getUpUntil = -1f;
        private float wanderClock;
        private Vector3 groundPos;

        private enum Teleport { None, Shimmer, Gone }
        private Teleport teleport = Teleport.None;
        private float teleportUntil;
        private Vector3 teleportTo;
        private bool quickShot;
        private bool teleportQuickShot = true;
        private float activeChargeTime;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            home = transform.position;
            groundPos = home;
            activeChargeTime = chargeTime;
            wanderPhase = Random.value * 10f;
            block = new MaterialPropertyBlock();
            BuildVisuals();

            // A kinematic Rigidbody tells physics this object moves, so hit tests use its current position.
            // The scene builder adds one; this covers dummies placed by hand.
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Attacks also test this capsule with plain math, so hits work even without colliders.
            HitShapes.Register(this, transform, hitRadius, hitHeight);

            // Chest-height point that aim assist steers shots toward.
            aimPoint = new GameObject("Aim Point").transform;
            aimPoint.SetParent(transform, false);
            aimPoint.localPosition = new Vector3(0f, chestHeight, 0f);
            AimAssist.Register(aimPoint, this);

            Revive();
        }

        private void OnDestroy()
        {
            if (orb != null) Destroy(orb.gameObject);
            if (aimPoint != null) AimAssist.Unregister(aimPoint);
            HitShapes.Unregister(this);
        }

        private void Start()
        {
            Invoke(nameof(PhysicsSelfTest), 0.5f);
        }

        // Fires a test ray at itself and reports the result on the debug panel, so a build that lost its
        // colliders is obvious. Hits still work through HitShapes either way.
        private void PhysicsSelfTest()
        {
            int colliders = GetComponentsInChildren<Collider>().Length;
            Physics.SyncTransforms();
            Vector3 origin = transform.position + Vector3.up * 1.0f - transform.forward * 2.5f;
            RaycastHit hit;
            bool ok = Physics.Raycast(origin, transform.forward, out hit, 5f, ~0, QueryTriggerInteraction.Ignore)
                      && hit.collider.GetComponentInParent<TrainingDummy>() == this;
            MoveLog.Status = "Dummy colliders " + colliders + ", physics test " + (ok ? "OK" : "FAILED") + " (hits use backup)";
        }

        private void BuildVisuals()
        {
            GameObject vis = visualsRoot;
            if (vis == null)
            {
                vis = new GameObject("Visuals");
                vis.transform.SetParent(transform, false);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(vis.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(vis.transform, false);
                head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
                head.transform.localScale = Vector3.one * 0.38f;

                if (bodyMaterial != null)
                {
                    body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
                    head.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
                }
            }
            visuals = vis.transform;
            renderers = vis.GetComponentsInChildren<Renderer>();

            GameObject barRoot = new GameObject("Health Bar");
            barRoot.transform.SetParent(transform, false);
            barRoot.transform.localPosition = new Vector3(0f, healthBarHeight, 0f);
            bar = barRoot.transform;
            Fx.Shape(PrimitiveType.Cube, healthBarBackMaterial, new Vector3(0.62f, 0.07f, 0.02f), bar);
            GameObject fill = Fx.Shape(PrimitiveType.Cube, healthBarMaterial, new Vector3(0.6f, 0.05f, 0.02f), bar);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            barFill = fill.transform;
        }

        /// <summary>The whirlwind is touching the dummy: slows its attacks and, after a moment, lifts it.</summary>
        public void OnWhirlwind(float deltaTime)
        {
            if (!IsAlive || IsTeleporting) return;
            slowedUntil = Time.time + slowDuration;
            windSeen = Time.time;
            if (air != Air.Grounded || Time.time < liftReadyTime) return;
            if (windContact <= 0f) MoveLog.Add("Whirlwind caught the dummy");
            windContact += deltaTime;
            if (windContact >= liftAfter)
            {
                windContact = 0f;
                air = Air.Rising;
                airVelocity = 0f;
                MoveLog.Add("Dummy lifted by whirlwind");
            }
        }

        /// <summary>A nearby whirlwind drags him toward its center. He can still walk out if he moves away.</summary>
        public void OnPull(Vector3 center, float speed, float deltaTime)
        {
            if (!CanAct) return;
            Vector3 to = center - (brainControlled ? groundPos : transform.position);
            to.y = 0f;
            float d = to.magnitude;
            if (d < 0.05f) return;
            Vector3 step = to / d * Mathf.Min(speed * deltaTime, d);
            if (brainControlled) groundPos += step;
            else knockOffset += step;
        }

        private bool Slowed { get { return Time.time < slowedUntil; } }

        private void UpdateAir()
        {
            if (Time.time - windSeen > 0.25f && air == Air.Grounded) windContact = 0f;

            if (air == Air.Rising)
            {
                airHeight = Mathf.MoveTowards(airHeight, liftHeight, 9f * Time.deltaTime);
                if (airHeight >= liftHeight - 0.001f)
                {
                    air = Air.Falling;
                    airVelocity = 0f;
                }
            }
            else if (air == Air.Falling)
            {
                airVelocity -= fallGravity * Time.deltaTime;
                airHeight += airVelocity * Time.deltaTime;
                if (airHeight <= 0f)
                {
                    airHeight = 0f;
                    air = Air.Grounded;
                    liftReadyTime = Time.time + liftCooldown;
                    float fall = maxFallDamage;   // it always falls from the same lift height
                    Vector3 feet = transform.position;
                    MoveLog.Add("Dummy landed: fall damage " + Mathf.RoundToInt(fall));
                    TakeDamage(fall, feet + Vector3.up * 0.3f, Vector3.zero, DamageKind.Fall);
                }
            }
        }

        /// <summary>Chest-height point on the dummy (where reflected bolts fly).</summary>
        public Vector3 ChestPoint
        {
            get { return aimPoint != null ? aimPoint.position : transform.position + Vector3.up * chestHeight; }
        }

        /// <summary>Back to full health and a fresh attack timer (the fight restarts after the player is knocked out).</summary>
        public void ResetFight()
        {
            groundPos = home;   // back to his starting spot
            Revive();
        }

        // ---- Hooks for EnemyBrain ----

        /// <summary>Bring the next shot forward so it starts charging after 'extraDelay' seconds. False if it can't (already charging, busy...).</summary>
        public bool HurryNextShot(float extraDelay)
        {
            if (!wasFighting || charging || !CanAct) return false;
            float soonest = Time.time + activeChargeTime + extraDelay;
            if (nextFireTime <= soonest) return false;
            nextFireTime = soonest;
            return true;
        }

        /// <summary>
        /// Start the teleport: flicker in place (untouchable), vanish, reappear at 'destination'. With 'quickShot' he
        /// fires a fast bolt right after reappearing; without it (an evade) he waits a full attack interval instead.
        /// </summary>
        public bool BeginTeleport(Vector3 destination, bool withQuickShot = true)
        {
            if (!CanAct) return false;
            teleport = Teleport.Shimmer;
            teleportQuickShot = withQuickShot;
            SetCollidersEnabled(false);   // attacks pass through during the flicker
            teleportUntil = Time.time + teleportShimmerTime;
            destination.y = home.y;
            teleportTo = destination;
            charging = false;
            HideOrb();
            if (TeleportStarted != null) TeleportStarted(transform.position, teleportTo);
            return true;
        }

        private void UpdateTeleport()
        {
            if (teleport == Teleport.Shimmer)
            {
                SetModelVisible(Mathf.Repeat(Time.time * 16f, 1f) > 0.35f);   // flicker = the warning
                if (Time.time >= teleportUntil)
                {
                    teleport = Teleport.Gone;
                    teleportUntil = Time.time + teleportGapTime;
                    SetModelVisible(false);
                    Vector3 from = transform.position;
                    groundPos = teleportTo;
                    knockOffset = Vector3.zero;
                    if (Teleported != null) Teleported(from, teleportTo);
                }
            }
            else if (teleport == Teleport.Gone && Time.time >= teleportUntil)
            {
                teleport = Teleport.None;
                SetModelVisible(true);
                if (lookTarget != null)
                {
                    Vector3 toPlayer = lookTarget.position - groundPos;
                    toPlayer.y = 0f;
                    if (toPlayer.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toPlayer);
                }
                SetCollidersEnabled(true);
                wasFighting = true;
                if (teleportQuickShot)
                {
                    // Quick shot from the new spot.
                    quickShot = true;
                    activeChargeTime = teleportShotCharge;
                    nextFireTime = Time.time + teleportShotCharge;
                }
                else
                {
                    // Evade: regroup before shooting again.
                    quickShot = false;
                    activeChargeTime = chargeTime;
                    nextFireTime = Time.time + attackInterval;
                }
            }
        }

        private void CancelTeleport()
        {
            if (teleport != Teleport.None && IsAlive) SetCollidersEnabled(true);
            teleport = Teleport.None;
            quickShot = false;
            activeChargeTime = chargeTime;
            SetModelVisible(true);
        }

        private void SetModelVisible(bool on)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = on;
        }

        private void Revive()
        {
            bool wasDown = !IsAlive && Health <= 0f && deathTime > 0f;
            getUpUntil = wasDown ? Time.time + getUpTime : -1f;
            wasFighting = false;   // fresh attack timer (it used to fire instantly after respawning)
            charging = false;
            CancelTeleport();
            IsAlive = true;
            air = Air.Grounded;
            airHeight = 0f;
            windContact = 0f;
            slowedUntil = -1f;
            Health = maxHealth;
            knockOffset = Vector3.zero;
            if (visuals != null)
            {
                visuals.localRotation = Quaternion.identity;
                visuals.localPosition = Vector3.zero;
            }
            SetCollidersEnabled(true);
            HideOrb();
            if (bar != null) bar.gameObject.SetActive(true);
            UpdateBar();
        }

        public void TakeDamage(float amount, Vector3 point, Vector3 force)
        {
            TakeDamage(amount, point, force, DamageKind.Direct);
        }

        public void TakeDamage(float amount, Vector3 point, Vector3 force, DamageKind kind)
        {
            if (!IsTargetable) return;

            // Direct hits to the head do extra.
            bool headshot = kind == DamageKind.Direct && point.y - transform.position.y >= headMinHeight;
            if (headshot) amount *= headshotMultiplier;
            if (DamageTaken != null) DamageTaken(amount);

            Health = Mathf.Max(0f, Health - amount);
            flashTimer = 0.12f;

            // Continuous damage (whirlwind, laser) arrives in tiny pieces; bundle it so feedback isn't spammed.
            pendingDamage += amount;
            if (headshot) pendingHeadshot = true;
            if (amount >= 1f || pendingDamage >= 5f)
            {
                Color c = pendingHeadshot ? new Color(1f, 0.45f, 0.2f) : (kind == DamageKind.Fall ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.85f, 0.3f));
                Impact.Hit(point, pendingDamage, c, lookTarget, true, pendingHeadshot ? "!" : null);
                if (Damaged != null) Damaged();
                if (pendingHeadshot) MoveLog.Add("HEADSHOT on the dummy");
                pendingDamage = 0f;
                pendingHeadshot = false;
            }
            force.y = 0f;
            knockOffset += force * 0.15f;
            knockOffset = Vector3.ClampMagnitude(knockOffset, 1.5f);
            UpdateBar();

            if (Health <= 0f)
            {
                MoveLog.Add("Dummy knocked out!");
                Die();
            }
        }

        private void Die()
        {
            IsAlive = false;
            deathTime = Time.time;
            CancelTeleport();
            transform.position -= Vector3.up * airHeight;   // knocked out mid-air: drop to the floor
            air = Air.Grounded;
            airHeight = 0f;
            HideOrb();
            SetCollidersEnabled(false);
            if (bar != null) bar.gameObject.SetActive(false);
        }

        private void SetCollidersEnabled(bool on)
        {
            Collider[] cols = GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = on;
        }

        private void Update()
        {
            if (IsAlive)
            {
                UpdateAlive();
                UpdateAttack();
            }
            else
                UpdateDead();

            UpdateFlash();
            FaceCamera();
        }

        private void UpdateAlive()
        {
            UpdateTeleport();
            Vector3 basePos;
            if (brainControlled)
            {
                // The brain decides where to go; he holds still while getting up, flying or teleporting.
                Vector3 v = CanAct ? DesiredVelocity : Vector3.zero;
                v.y = 0f;
                groundPos += v * Time.deltaTime;
                groundPos = ClampToArena(groundPos, 0f);
                basePos = groundPos;
            }
            else
            {
                if (!IsGettingUp) wanderClock += Time.deltaTime;   // stands still while getting up
                float t = wanderClock * wanderSpeed + wanderPhase;
                basePos = home + new Vector3(Mathf.Sin(t) * wanderExtents.x, 0f, Mathf.Sin(t * 0.63f + 1.3f) * wanderExtents.z);
            }
            knockOffset = Vector3.Lerp(knockOffset, Vector3.zero, 3f * Time.deltaTime);
            UpdateAir();
            transform.position = basePos + knockOffset + Vector3.up * airHeight;

            if (lookTarget != null)
            {
                Vector3 toPlayer = lookTarget.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), 6f * Time.deltaTime);
            }
        }

        // ---- Attacking: glow, then fire at the player's chest ----

        private void UpdateAttack()
        {
            PlayerHealth player = PlayerHealth.Instance;
            bool fighting = attacks && player != null && player.IsAlive && player.FightStarted;
            if (!fighting)
            {
                wasFighting = false;
                charging = false;
                HideOrb();
                return;
            }

            // Held in the air by a whirlwind: no shooting, and the timer waits for it to land.
            if (air != Air.Grounded)
            {
                nextFireTime += Time.deltaTime;
                charging = false;
                HideOrb();
                return;
            }

            // Teleporting: the timer waits; the quick shot is set up when he reappears.
            if (IsTeleporting)
            {
                charging = false;
                HideOrb();
                return;
            }

            // Getting up after a knock-out: no shooting yet.
            if (IsGettingUp)
            {
                wasFighting = false;
                charging = false;
                HideOrb();
                return;
            }

            if (!wasFighting)
            {
                wasFighting = true;
                nextFireTime = Time.time + Mathf.Max(attackInterval, chargeTime + 0.5f) * 0.7f;
            }

            Vector3 muzzle = muzzleBone != null
                ? muzzleBone.position + transform.forward * muzzleForwardOffset
                : transform.position + Vector3.up * 1.4f + transform.forward * 0.45f;
            float untilFire = nextFireTime - Time.time;
            if (untilFire <= 0f)
            {
                HideOrb();
                bool atHead = charging && chargeAimsHead;
                charging = false;
                Vector3 target = atHead ? player.HeadPoint : player.ChestPoint;
                Vector3 dir = target - muzzle;
                if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
                EnemyBolt.Spawn(GetAttackMaterial(), muzzle, dir.normalized, quickShot ? teleportBoltSpeed : boltSpeed, boltDamage, this, atHead);
                MoveLog.Add((quickShot ? "Dummy quick-fires after teleporting" : "Dummy fires") + (atHead ? " (AT YOUR HEAD)" : ""));
                quickShot = false;
                activeChargeTime = chargeTime;
                nextFireTime = Time.time + attackInterval * (Slowed ? slowedIntervalMultiplier : 1f);
            }
            else if (untilFire <= activeChargeTime)
            {
                if (!charging)
                {
                    charging = true;
                    chargeAimsHead = Random.value < headShotChance;   // decided when the glow starts, shown by its color
                }
                float t = 1f - untilFire / Mathf.Max(0.01f, activeChargeTime);
                ShowOrb(muzzle, t);
            }
            else
            {
                HideOrb();
            }
        }

        private Material GetAttackMaterial()
        {
            if (attackMaterial == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                attackMaterial = new Material(sh);
                attackMaterial.color = EnemyBolt.Yellow;
            }
            return attackMaterial;
        }

        private void ShowOrb(Vector3 position, float t)
        {
            if (orb == null)
            {
                orb = Fx.Shape(PrimitiveType.Sphere, GetAttackMaterial(), Vector3.one * 0.05f).transform;
                orb.name = "Charge Orb";
                orbRenderer = orb.GetComponent<Renderer>();
                orbBlock = new MaterialPropertyBlock();
            }
            Color tint = chargeAimsHead ? EnemyBolt.HeadOrange : EnemyBolt.Yellow;
            orbBlock.SetColor(BaseColorId, tint);
            orbBlock.SetColor(ColorId, tint);
            orbRenderer.SetPropertyBlock(orbBlock);
            orb.gameObject.SetActive(true);
            orb.position = position;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 40f);
            orb.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.3f, t) * pulse;
        }

        private void HideOrb()
        {
            if (orb != null) orb.gameObject.SetActive(false);
        }

        private void UpdateDead()
        {
            float since = Time.time - deathTime;
            // Topple over backwards in the first 0.6 seconds.
            float fall = Mathf.Clamp01(since / 0.6f);
            if (visuals != null && !animatedModel)   // an animated model plays its own knock-out
            {
                visuals.localRotation = Quaternion.Euler(-90f * fall, 0f, 0f);
                visuals.localPosition = new Vector3(0f, 0.15f * fall, 0f);
            }
            if (since >= respawnDelay)
                Revive();
        }

        private void UpdateFlash()
        {
            if (renderers == null) return;
            bool flashing = flashTimer > 0f;
            if (flashing) flashTimer -= Time.deltaTime;

            Color alive = Slowed ? new Color(0.6f, 0.78f, 1f) : normalTint;   // bluish while its attacks are slowed
            Color down = normalTint * 0.55f;
            down.a = 1f;
            Color c = flashing ? new Color(1f, 0.35f, 0.3f) : (IsAlive ? alive : down);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].SetPropertyBlock(block);
        }

        private void FaceCamera()
        {
            if (bar == null || lookTarget == null || !bar.gameObject.activeSelf) return;
            Vector3 away = bar.position - lookTarget.position;
            if (away.sqrMagnitude > 0.0001f)
                bar.rotation = Quaternion.LookRotation(away);
        }

        private void UpdateBar()
        {
            if (barFill == null) return;
            float f = maxHealth > 0f ? Health / maxHealth : 0f;
            Vector3 s = barFill.localScale;
            s.x = 0.6f * f;
            barFill.localScale = s;
            Vector3 p = barFill.localPosition;
            p.x = -0.3f * (1f - f);
            barFill.localPosition = p;
        }
    }
}
