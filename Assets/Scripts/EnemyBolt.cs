using System.Collections.Generic;
using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// The dummy's bolt (yellow; orange when it is going for your head). Flies straight at where you were when it
    /// was fired, so a lean, duck or dash dodges it. A single shield absorbs it and gives a little ring energy back;
    /// a double shield reflects it back, faster and harder. Your own bolts and blasts clash with it (equal damage
    /// cancels; the stronger one punches through, losing half of the weaker one's damage) and your laser cuts it.
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
        private bool dead;
        private float age;
        private Renderer rend;
        private MaterialPropertyBlock block;

        private static readonly List<EnemyBolt> all = new List<EnemyBolt>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly Color Yellow = new Color(1f, 0.85f, 0.15f);
        public static readonly Color HeadOrange = new Color(1f, 0.4f, 0.08f);
        public static readonly Color ReflectGreen = new Color(0.3f, 1f, 0.5f);
        private static readonly Color ClashBlack = new Color(0.08f, 0f, 0.14f);

        public static EnemyBolt Spawn(Material material, Vector3 position, Vector3 direction, float speed, float damage, TrainingDummy owner, bool headShot = false)
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
            b.Tint(headShot ? HeadOrange : Yellow);
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

        private void Kill()
        {
            dead = true;
            all.Remove(this);
            Destroy(gameObject);
        }

        // ---- Clashing with the player's attacks ----

        /// <summary>
        /// Called by each player projectile every frame with the path it is about to travel. If it meets an enemy
        /// bolt: equal damage cancels both; otherwise the stronger one carries on with its damage reduced by half
        /// of the weaker one's. Returns true if the player's projectile is used up ('damage' is updated either way).
        /// </summary>
        public static bool ClashWith(Vector3 from, Vector3 to, float projectileRadius, ref float damage)
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (i >= all.Count) continue;
                EnemyBolt b = all[i];
                if (b == null || b.dead || b.reflected) continue;

                // The enemy bolt covers about this much ground this frame; test both paths against each other.
                Vector3 fwd = b.transform.forward;
                float reach = b.speed * Time.deltaTime;
                Vector3 c1, c2;
                float gap = HitShapes.ClosestSegSeg(from, to, b.transform.position - fwd * reach, b.transform.position + fwd * reach, out float s, out float t, out c1, out c2);
                if (gap > projectileRadius + b.radius) continue;

                Vector3 point = (c1 + c2) * 0.5f;
                float mine = damage;
                float theirs = b.damage;
                ClashBurst(point);

                if (Mathf.Abs(mine - theirs) < 0.01f)
                {
                    b.Kill();
                    damage = 0f;
                    MoveLog.Add("Bolts cancelled (" + Mathf.RoundToInt(mine) + " vs " + Mathf.RoundToInt(theirs) + ")");
                    return true;
                }
                if (mine > theirs)
                {
                    damage = mine - 0.5f * theirs;
                    b.Kill();
                    MoveLog.Add("Your shot broke through (" + Mathf.RoundToInt(mine) + " vs " + Mathf.RoundToInt(theirs) + ", now " + Mathf.RoundToInt(damage) + ")");
                    continue;
                }

                b.damage = theirs - 0.5f * mine;
                damage = 0f;
                MoveLog.Add("Enemy bolt overpowered your shot (" + Mathf.RoundToInt(mine) + " vs " + Mathf.RoundToInt(theirs) + ", now " + Mathf.RoundToInt(b.damage) + ")");
                return true;
            }
            return false;
        }

        /// <summary>The laser slices through any enemy bolt within 'width' of the line from 'from' to 'to'.</summary>
        public static void CutAlong(Vector3 from, Vector3 to, float width)
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (i >= all.Count) continue;
                EnemyBolt b = all[i];
                if (b == null || b.dead || b.reflected) continue;
                Vector3 closest;
                if (!SegmentPointDistance(from, to, b.transform.position, out closest, width + b.radius)) continue;
                ClashBurst(b.transform.position);
                MoveLog.Add("Laser cut an enemy bolt");
                b.Kill();
            }
        }

        private static bool SegmentPointDistance(Vector3 a, Vector3 b, Vector3 p, out Vector3 closest, float maxDistance)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            closest = a + ab * t;
            return (p - closest).magnitude <= maxDistance;
        }

        // A small dark burst where bolts collide.
        private static void ClashBurst(Vector3 point)
        {
            Impact.Spark(point, ClashBlack, 0.4f);
        }

        // ---- Flight ----

        private void Tint(Color c)
        {
            if (rend == null) return;
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            rend.SetPropertyBlock(block);
        }

        private void Update()
        {
            if (dead) return;
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Kill();
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
                    Kill();
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
                        Kill();
                        return;
                    }
                }

                Vector3 hit;
                bool head;
                if (player.SegmentHits(from, to, radius, out hit, out head))
                {
                    player.Hit(damage, hit, head);
                    Kill();
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
