using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// The dummy's yellow bolt. Flies straight at where your chest was when it was fired (so a lean or duck dodges it).
    /// A single shield absorbs it and gives a little ring energy back; a double shield reflects it back, faster and harder.
    /// </summary>
    public class EnemyBolt : MonoBehaviour
    {
        public float speed = 9f;
        public float damage = 10f;
        public float radius = 0.1f;
        public float lifetime = 6f;

        [Header("When reflected by a double shield")]
        public float reflectSpeedMultiplier = 2f;
        public float reflectDamage = 25f;

        private TrainingDummy owner;
        private bool reflected;
        private float age;
        private Renderer rend;
        private MaterialPropertyBlock block;

        private static readonly List<EnemyBolt> all = new List<EnemyBolt>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly Color Yellow = new Color(1f, 0.85f, 0.15f);
        public static readonly Color ReflectGreen = new Color(0.3f, 1f, 0.5f);

        public static EnemyBolt Spawn(Material material, Vector3 position, Vector3 direction, float speed, float damage, TrainingDummy owner)
        {
            GameObject go = Fx.Shape(PrimitiveType.Sphere, material, Vector3.one * 0.2f);
            go.name = "Enemy Bolt";
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(direction);
            EnemyBolt b = go.AddComponent<EnemyBolt>();
            b.speed = speed;
            b.damage = damage;
            b.owner = owner;
            b.rend = go.GetComponent<Renderer>();
            b.block = new MaterialPropertyBlock();
            b.Tint(Yellow);
            all.Add(b);
            return b;
        }

        public static void ClearAll()
        {
            for (int i = all.Count - 1; i >= 0; i--)
                if (all[i] != null) Destroy(all[i].gameObject);
            all.Clear();
        }

        private void OnDestroy()
        {
            all.Remove(this);
        }

        private void Tint(Color c)
        {
            if (rend == null) return;
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            rend.SetPropertyBlock(block);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 dir = transform.forward;
            Vector3 from = transform.position;
            Vector3 to = from + dir * (speed * Time.deltaTime);

            if (reflected)
            {
                IDamageable target;
                Vector3 point;
                float fraction;
                if (HitShapes.Segment(from, to, radius, out target, out point, out fraction))
                {
                    target.TakeDamage(damage, point, dir * 3f);
                    MoveLog.Add("Reflected bolt HIT for " + Mathf.RoundToInt(damage));
                    Destroy(gameObject);
                    return;
                }
                transform.position = to;
                return;
            }

            PlayerHealth player = PlayerHealth.Instance;
            if (player != null && player.IsAlive)
            {
                ShieldMove shield = player.shield;
                Vector3 sp;
                if (shield != null && shield.Intercept(from, to, radius, out sp))
                {
                    if (shield.IsDoubled)
                    {
                        Reflect(shield, sp);
                        return;
                    }
                    if (shield.AbsorbHit())
                    {
                        Impact.Spark(sp, new Color(0.7f, 1f, 0.4f), 0.3f);
                        MoveLog.Add("Shield absorbed bolt (+" + Mathf.RoundToInt(shield.absorbRefund) + " energy)");
                        Destroy(gameObject);
                        return;
                    }
                }

                Vector3 hit;
                if (player.SegmentHits(from, to, radius, out hit))
                {
                    player.Hit(damage, hit);
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position = to;
        }

        private void Reflect(ShieldMove shield, Vector3 point)
        {
            reflected = true;
            damage = reflectDamage;
            speed *= reflectSpeedMultiplier;
            age = 0f;

            Vector3 back = -transform.forward;
            if (owner != null && owner.IsAlive)
            {
                Vector3 toOwner = owner.ChestPoint - point;
                if (toOwner.sqrMagnitude > 0.01f) back = toOwner.normalized;
            }
            transform.position = point;
            transform.rotation = Quaternion.LookRotation(back);
            Tint(ReflectGreen);
            shield.PulseHands(0.9f, 0.15f);
            Impact.Spark(point, ReflectGreen, 0.35f);
            MoveLog.Add("Bolt REFLECTED (" + Mathf.RoundToInt(damage) + " dmg)");
        }
    }
}
