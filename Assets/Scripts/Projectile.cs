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
            RaycastHit hit;
            // Moving targets change position in Update, so push those changes to the physics system first.
            Physics.SyncTransforms();
            // SphereCast instead of relying on physics collisions, so fast shots never skip through targets.
            if (Physics.SphereCast(transform.position, radius, dir, out hit, step, ~0, QueryTriggerInteraction.Ignore))
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
