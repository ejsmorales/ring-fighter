using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// A practice opponent: drifts around the arena, flinches and gets knocked back when hit,
    /// shows a health bar, falls over when its health runs out, then comes back after a few seconds.
    /// It doesn't attack yet. Later, replace the shapes with a real character model: assign
    /// 'visualsRoot' with your own model and the placeholder shapes are skipped.
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        public float maxHealth = 100f;
        public float respawnDelay = 3.5f;

        [Header("Movement")]
        public Vector3 wanderExtents = new Vector3(2.2f, 0f, 1.2f);
        public float wanderSpeed = 0.55f;
        public Transform lookTarget;   // usually the player's head

        [Header("Look")]
        public Material bodyMaterial;
        public Material healthBarMaterial;
        public Material healthBarBackMaterial;
        public GameObject visualsRoot; // optional: your own model instead of the placeholder shapes

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

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            home = transform.position;
            wanderPhase = Random.value * 10f;
            block = new MaterialPropertyBlock();
            BuildVisuals();
            Revive();
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
            if (bar != null) bar.gameObject.SetActive(true);
            UpdateBar();
        }

        public void TakeDamage(float amount, Vector3 point, Vector3 force)
        {
            if (!IsAlive) return;
            Health = Mathf.Max(0f, Health - amount);
            flashTimer = 0.12f;
            force.y = 0f;
            knockOffset += force * 0.15f;
            knockOffset = Vector3.ClampMagnitude(knockOffset, 1.5f);
            UpdateBar();

            if (Health <= 0f)
                Die();
        }

        private void Die()
        {
            IsAlive = false;
            deathTime = Time.time;
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
                UpdateAlive();
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
