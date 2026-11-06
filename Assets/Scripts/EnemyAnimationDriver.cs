using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Plays the enemy character's animations from what the TrainingDummy is doing. It only reads the dummy's
    /// state; all gameplay stays in TrainingDummy. Animator parameters (set up by the scene builder's controller):
    ///   Alive (bool), Airborne (bool), Charging (bool), Hit (trigger), and if movement clips exist,
    ///   MoveX / MoveZ (floats, -1 to 1): his sideways and forward speed, so he strafes and walks instead of sliding.
    /// Also points the dummy's muzzle at the model's right hand, where the laser-pointer pen is.
    /// </summary>
    [RequireComponent(typeof(TrainingDummy))]
    public class EnemyAnimationDriver : MonoBehaviour
    {
        public Animator animator;
        [Tooltip("Turns the model around its vertical axis (degrees) if it faces the wrong way.")]
        public float modelYawDegrees = 0f;
        [Tooltip("Shortest gap (seconds) between two flinch animations, so the laser doesn't restart it every frame.")]
        public float minHitInterval = 0.8f;
        [Tooltip("Ground speed (m/s) that plays the walk/strafe clips at full strength. Raise it if his feet move too fast for how far he goes.")]
        public float fullMoveSpeed = 1.1f;
        [Tooltip("Smoothing (seconds) for the walk/strafe blend.")]
        public float moveDamping = 0.15f;

        [Header("Floor safety")]
        [Tooltip("Keeps his feet from sinking into the floor: if an animation pushes his lowest foot below this height above the floor (meters), the model is raised to match.")]
        public float footClearance = 0.04f;
        [Tooltip("How fast (m/s) a raised model settles back down once it no longer needs lifting.")]
        public float settleSpeed = 1.5f;
        [Tooltip("How fast (m/s) the model is raised when a foot dips into the floor. Not instant, so it doesn't pop.")]
        public float liftSpeed = 4f;

        private bool hasMove;
        private Transform[] feet;
        private float floorLift;
        private float lastSinkLog = -10f;
        private Vector3 lastPosition;
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveZId = Animator.StringToHash("MoveZ");

        private TrainingDummy dummy;
        private float lastHit = -10f;

        private static readonly int AliveId = Animator.StringToHash("Alive");
        private static readonly int AirborneId = Animator.StringToHash("Airborne");
        private static readonly int ChargingId = Animator.StringToHash("Charging");
        private static readonly int HitId = Animator.StringToHash("Hit");

        private void Awake()
        {
            dummy = GetComponent<TrainingDummy>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                enabled = false;
                return;
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.transform.localRotation = Quaternion.Euler(0f, modelYawDegrees, 0f);

            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.nameHash == MoveXId) hasMove = true;
            lastPosition = transform.position;

            Transform hand = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            if (animator.isHuman)
            {
                // Toes if the rig has them, else the ankles (which sit higher, so allow more room).
                Transform lt = animator.GetBoneTransform(HumanBodyBones.LeftToes);
                Transform rt = animator.GetBoneTransform(HumanBodyBones.RightToes);
                Transform lf = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rf = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (lt != null && rt != null) feet = new[] { lt, rt };
                else if (lf != null && rf != null)
                {
                    feet = new[] { lf, rf };
                    footClearance = Mathf.Max(footClearance, 0.07f);
                }
            }
            if (hand != null) dummy.muzzleBone = hand;
            else MoveLog.Add("Enemy model: right hand bone not found, bolts fire from the chest");
        }

        private void OnEnable()
        {
            if (dummy != null) dummy.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (dummy != null) dummy.Damaged -= OnDamaged;
        }

        private void OnDamaged()
        {
            if (!dummy.IsAlive || dummy.IsAirborne || Time.time - lastHit < minHitInterval) return;
            lastHit = Time.time;
            animator.SetTrigger(HitId);
        }

        // After the animation has posed him: if his feet ended up below the floor, raise the model so they don't.
        // Logs which animation did it, so the cause can be fixed properly.
        private void LateUpdate()
        {
            if (feet == null) return;
            Transform model = animator.transform;
            bool grounded = dummy.IsAlive && !dummy.IsAirborne;
            if (!grounded)
            {
                floorLift = Mathf.MoveTowards(floorLift, 0f, settleSpeed * Time.deltaTime);
            }
            else
            {
                float lowest = Mathf.Min(feet[0].position.y, feet[1].position.y);
                float error = dummy.FloorHeight + footClearance - lowest;   // > 0 means sunk
                if (error > 0.02f)
                {
                    floorLift += Mathf.Min(error, liftSpeed * Time.deltaTime);
                    if (error > 0.15f && Time.time - lastSinkLog > 2f)
                    {
                        lastSinkLog = Time.time;
                        MoveLog.Add("Enemy sank " + error.ToString("0.00") + " m (" + CurrentStateName() + ") - corrected");
                    }
                }
                else if (floorLift > 0f && error < -0.01f)
                {
                    floorLift = Mathf.Max(0f, floorLift - Mathf.Min(-error, settleSpeed * Time.deltaTime));
                }
            }
            floorLift = Mathf.Clamp(floorLift, 0f, 2f);
            Vector3 p = model.localPosition;
            p.y = floorLift;
            model.localPosition = p;
        }

        private string CurrentStateName()
        {
            AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(0);
            string[] names = { "Locomotion", "Idle", "Charge", "Hit", "Lifted", "KnockedOut", "GetUp" };
            for (int i = 0; i < names.Length; i++)
                if (s.IsName(names[i])) return names[i];
            return "state " + s.shortNameHash;
        }

        private void Update()
        {
            animator.SetBool(AliveId, dummy.IsAlive);
            animator.SetBool(AirborneId, dummy.IsAirborne);
            animator.SetBool(ChargingId, dummy.IsCharging);
            // A flinch asked for while flying or down would otherwise wait and play later.
            if (!dummy.IsAlive || dummy.IsAirborne) animator.ResetTrigger(HitId);

            if (hasMove)
            {
                // His ground velocity, seen from his own point of view (he always faces the player).
                Vector3 delta = transform.position - lastPosition;
                lastPosition = transform.position;
                delta.y = 0f;
                Vector3 local = Time.deltaTime > 0f ? transform.InverseTransformDirection(delta / Time.deltaTime) : Vector3.zero;
                Vector2 move = new Vector2(local.x, local.z) / Mathf.Max(0.1f, fullMoveSpeed);
                if (move.magnitude > 1f) move.Normalize();
                if (!dummy.IsAlive || dummy.IsAirborne) move = Vector2.zero;
                animator.SetFloat(MoveXId, move.x, moveDamping, Time.deltaTime);
                animator.SetFloat(MoveZId, move.y, moveDamping, Time.deltaTime);
            }
        }
    }
}
