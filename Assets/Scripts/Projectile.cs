using UnityEngine;

namespace RingFighter
{
    /// <summary>A glowing ball of energy that flies straight and hurts the first damageable thing it touches.</summary>
    public class Projectile : MonoBehaviour
    {
        public float speed = 18f;
        public float damage = 10f;
        public float radius = 0.08f;
        public float knockback = 3f;
        public float lifetime = 3f;

        private float age;

        public static Projectile Spawn(Material material, Vector3 position, Vector3 direction, float speed, float damage, float radius, float knockback)
        {
            GameObject go = Fx.Shape(PrimitiveType.Sphere, material, Vector3.one * radius * 2f);
            go.name = "Energy Projectile";
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(direction);
            Projectile p = go.AddComponent<Projectile>();
            p.speed = speed;
            p.damage = damage;
            p.radius = radius;
            p.knockback = knockback;
            return p;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                MoveLog.Add("Shot missed (expired)");
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;
            Vector3 dir = transform.forward;
            Vector3 from = transform.position;

            // 1) Targets, by plain math (works even if colliders are missing in a build).
            IDamageable shapeTarget;
            Vector3 shapePoint;
            float fraction;
            bool shapeHit = HitShapes.Segment(from, from + dir * step, radius, out shapeTarget, out shapePoint, out fraction);
            float shapeDist = shapeHit ? fraction * step : float.MaxValue;

            // 2) Everything else (floor, walls) through physics.
            Physics.SyncTransforms();
            RaycastHit hit;
            bool physHit = Physics.SphereCast(from, radius, dir, out hit, step, ~0, QueryTriggerInteraction.Ignore);
            float physDist = physHit ? hit.distance : float.MaxValue;

            if (shapeHit && shapeDist <= physDist)
            {
                shapeTarget.TakeDamage(damage, shapePoint, dir * knockback);
                MoveLog.Add("HIT for " + Mathf.RoundToInt(damage));
                Destroy(gameObject);
                return;
            }
            if (physHit)
            {
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                {
                    target.TakeDamage(damage, hit.point, dir * knockback);
                    MoveLog.Add("HIT " + hit.collider.name + " for " + Mathf.RoundToInt(damage));
                }
                else
                {
                    MoveLog.Add("Shot hit " + hit.collider.name + " (not a target)");
                }
                Destroy(gameObject);
                return;
            }

            transform.position += dir * step;
        }
    }
}
