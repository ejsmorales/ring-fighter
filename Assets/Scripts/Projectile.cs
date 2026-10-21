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
        [Tooltip("True for charged blasts. Only changes how the shot looks (see ProjectileVfx); damage is unaffected.")]
        public bool isBlast;

        private float age;

        public static Projectile Spawn(Material material, Vector3 position, Vector3 direction, float speed, float damage, float radius, float knockback, bool blast = false)
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
            p.isBlast = blast;
            go.AddComponent<ProjectileVfx>();   // visuals only; harmless if the effects are unavailable
            return p;
        }

        // Tells the visual component how this shot ended, so it can play an end effect (a blast explodes).
        private void NotifyEnd(ProjectileVfx.End kind, Vector3 point)
        {
            ProjectileVfx vfx = GetComponent<ProjectileVfx>();
            if (vfx != null) vfx.OnEnded(kind, point);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                MoveLog.Add("Shot missed (expired)");
                NotifyEnd(ProjectileVfx.End.Expired, transform.position);
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;
            Vector3 dir = transform.forward;
            Vector3 from = transform.position;

            // 0) Enemy bolts in the way: they cancel, or the stronger one punches through.
            if (EnemyBolt.ClashWith(from, from + dir * step, radius, ref damage))
            {
                NotifyEnd(ProjectileVfx.End.Clash, from);
                Destroy(gameObject);
                return;
            }

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
                NotifyEnd(ProjectileVfx.End.Target, shapePoint);
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
                    NotifyEnd(ProjectileVfx.End.Target, hit.point);
                }
                else
                {
                    MoveLog.Add("Shot hit " + hit.collider.name + " (not a target)");
                    NotifyEnd(ProjectileVfx.End.Surface, hit.point);
                }
                Destroy(gameObject);
                return;
            }

            transform.position += dir * step;
        }
    }
}
