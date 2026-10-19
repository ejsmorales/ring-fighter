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
        public float boltSpeed = 9f;
        public float boltDamage = 10f;
        [Tooltip("Glowing yellow material for the charge-up orb and the bolt.")]
        public Material attackMaterial;

        [Header("Movement")]
        public Vector3 wanderExtents = new Vector3(2.2f, 0f, 1.2f);
        public float wanderSpeed = 0.55f;
        public Transform lookTarget;   // usually the player's head

        [Header("Look")]
        public Material bodyMaterial;
        public Material healthBarMaterial;
        public Material healthBarBackMaterial;
        public GameObject visualsRoot; // optional: your own model instead of the placeholder shapes

        [Header("Hit area")]
        [Tooltip("Radius (meters) of the invisible capsule that attacks test against.")]
        public float hitRadius = 0.35f;
        [Tooltip("Height (meters) of the invisible capsule that attacks test against.")]
        public float hitHeight = 2.15f;

        public bool IsAlive { get; private set; }
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
        private float nextFireTime;
        private bool wasFighting;
        private Transform orb;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            home = transform.position;
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
            aimPoint.localPosition = new Vector3(0f, 1.25f, 0f);
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
            barRoot.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            bar = barRoot.transform;
            Fx.Shape(PrimitiveType.Cube, healthBarBackMaterial, new Vector3(0.62f, 0.07f, 0.02f), bar);
            GameObject fill = Fx.Shape(PrimitiveType.Cube, healthBarMaterial, new Vector3(0.6f, 0.05f, 0.02f), bar);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            barFill = fill.transform;
        }

        /// <summary>Chest-height point on the dummy (where reflected bolts fly).</summary>
        public Vector3 ChestPoint
        {
            get { return aimPoint != null ? aimPoint.position : transform.position + Vector3.up * 1.25f; }
        }

        /// <summary>Back to full health and a fresh attack timer (the fight restarts after the player is knocked out).</summary>
        public void ResetFight()
        {
            Revive();
            wasFighting = false;
        }

        private void Revive()
        {
            IsAlive = true;
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
            if (!IsAlive) return;
            Health = Mathf.Max(0f, Health - amount);
            flashTimer = 0.12f;

            // Continuous damage (whirlwind) arrives in tiny pieces; bundle it so feedback isn't spammed.
            pendingDamage += amount;
            if (amount >= 1f || pendingDamage >= 5f)
            {
                Impact.Hit(point, pendingDamage, new Color(1f, 0.85f, 0.3f), lookTarget);
                pendingDamage = 0f;
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
            float t = Time.time * wanderSpeed + wanderPhase;
            Vector3 wander = new Vector3(Mathf.Sin(t) * wanderExtents.x, 0f, Mathf.Sin(t * 0.63f + 1.3f) * wanderExtents.z);
            knockOffset = Vector3.Lerp(knockOffset, Vector3.zero, 3f * Time.deltaTime);
            transform.position = home + wander + knockOffset;

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
                HideOrb();
                return;
            }

            if (!wasFighting)
            {
                wasFighting = true;
                nextFireTime = Time.time + Mathf.Max(attackInterval, chargeTime + 0.5f) * 0.7f;
            }

            Vector3 muzzle = transform.position + Vector3.up * 1.4f + transform.forward * 0.45f;
            float untilFire = nextFireTime - Time.time;
            if (untilFire <= 0f)
            {
                HideOrb();
                Vector3 dir = player.ChestPoint - muzzle;
                if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
                EnemyBolt.Spawn(GetAttackMaterial(), muzzle, dir.normalized, boltSpeed, boltDamage, this);
                MoveLog.Add("Dummy fires");
                nextFireTime = Time.time + attackInterval;
            }
            else if (untilFire <= chargeTime)
            {
                float t = 1f - untilFire / Mathf.Max(0.01f, chargeTime);
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
            }
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
            if (visuals != null)
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

            Color c = flashing ? new Color(1f, 0.35f, 0.3f) : (IsAlive ? new Color(0.78f, 0.78f, 0.82f) : new Color(0.35f, 0.35f, 0.4f));
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
