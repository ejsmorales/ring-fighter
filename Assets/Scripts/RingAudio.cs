using UnityEngine;

namespace RingFighter
{
    /// <summary>
    /// Sounds for the ring's moves. Only listens to the moves (like the visual effects); no gameplay here.
    ///   - one-shots: bolt zap, blast launch and boom, shield block / reflect;
    ///   - loops that fade in and out with the move: laser hum, blast charge (pitch rises with the charge),
    ///     whirlwind wind (follows the vortex), shield hum (lower and louder when doubled), recharge chord.
    /// All sounds come from SfxSynth for now; swap clips there (or assign real ones later) to change them.
    /// </summary>
    public class RingAudio : MonoBehaviour
    {
        public RingController controller;
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Header("Volumes (0-1)")]
        [Range(0f, 1f)] public float boltVolume = 0.6f;
        [Range(0f, 1f)] public float blastVolume = 0.9f;
        [Range(0f, 1f)] public float laserVolume = 0.45f;
        [Range(0f, 1f)] public float chargeVolume = 0.4f;
        [Range(0f, 1f)] public float windVolume = 0.7f;
        [Range(0f, 1f)] public float shieldVolume = 0.35f;
        [Range(0f, 1f)] public float blockVolume = 0.8f;
        [Range(0f, 1f)] public float rechargeVolume = 0.35f;

        private LaserMove laser;
        private ChargedBlastMove blast;
        private WhirlwindMove whirl;
        private ShieldMove shield;
        private RechargeMove recharge;

        private AudioSource laserSrc, chargeSrc, windSrc, shieldSrc, rechargeSrc;

        private void Start()
        {
            if (controller == null) controller = GetComponent<RingController>();
            if (controller == null)
            {
                enabled = false;
                return;
            }
            laser = controller.GetComponent<LaserMove>();
            blast = controller.GetComponent<ChargedBlastMove>();
            whirl = controller.GetComponent<WhirlwindMove>();
            shield = controller.GetComponent<ShieldMove>();
            recharge = controller.GetComponent<RechargeMove>();

            laserSrc = Loop("Laser Hum", SfxSynth.LaserLoop);
            chargeSrc = Loop("Blast Charge", SfxSynth.ChargeLoop);
            windSrc = Loop("Whirlwind Wind", SfxSynth.WindLoop);
            shieldSrc = Loop("Shield Hum", SfxSynth.ShieldHum);
            rechargeSrc = Loop("Recharge Chord", SfxSynth.RechargeLoop);
            windSrc.maxDistance = 40f;

            Projectile.Spawned += OnShot;
            Projectile.Ended += OnShotEnded;
            if (shield != null) shield.BoltBlocked += OnBlocked;
        }

        private void OnDestroy()
        {
            Projectile.Spawned -= OnShot;
            Projectile.Ended -= OnShotEnded;
            if (shield != null) shield.BoltBlocked -= OnBlocked;
        }

        private AudioSource Loop(string name, AudioClip clip)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.minDistance = 0.6f;
            s.maxDistance = 25f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.dopplerLevel = 0f;
            s.volume = 0f;
            return s;
        }

        // ---- One-shots ----

        private void OnShot(Projectile p)
        {
            if (p == null) return;
            if (p.isBlast) Play(SfxSynth.Launch, p.transform.position, blastVolume * 0.8f);
            else Play(SfxSynth.Zap, p.transform.position, boltVolume);
        }

        private void OnShotEnded(Projectile p, Vector3 point)
        {
            if (p != null && p.isBlast) Play(SfxSynth.Boom, point, blastVolume);
        }

        private void OnBlocked(bool reflected)
        {
            Transform d = shield != null ? shield.Disc : null;
            Vector3 pos = d != null ? d.position : transform.position;
            Play(reflected ? SfxSynth.Reflect : SfxSynth.Block, pos, blockVolume);
        }

        private void Play(AudioClip clip, Vector3 pos, float volume)
        {
            AudioSource.PlayClipAtPoint(clip, pos, Mathf.Clamp01(volume * masterVolume));
        }

        // ---- Loops ----

        private void Update()
        {
            RingContext ctx = controller.Context;
            TrackedNode ring = ctx != null ? ctx.RingHand : null;
            Vector3 handPos = ring != null ? ring.transform.position : transform.position;

            Fade(laserSrc, laser != null && laser.IsActive, handPos, laserVolume, 1f + 0.03f * Mathf.Sin(Time.time * 7f));

            bool charging = blast != null && blast.IsActive;
            float charge = charging ? blast.Charge : 0f;
            Vector3 mid = ctx != null && ctx.Left != null && ctx.Right != null
                ? (ctx.Left.transform.position + ctx.Right.transform.position) * 0.5f : handPos;
            Fade(chargeSrc, charging, mid, chargeVolume * (0.5f + 0.5f * charge), Mathf.Lerp(0.7f, 1.7f, charge));

            Transform w = whirl != null && whirl.IsActive ? whirl.RootTransform : null;
            Fade(windSrc, w != null, w != null ? w.position + Vector3.up : windSrc.transform.position, windVolume, 1f);

            Transform disc = shield != null ? shield.Disc : null;
            bool doubled = shield != null && shield.IsDoubled;
            Fade(shieldSrc, disc != null, disc != null ? disc.position : shieldSrc.transform.position,
                 shieldVolume * (doubled ? 1.4f : 1f), doubled ? 0.8f : 1f);

            bool recharging = recharge != null && recharge.IsActive;
            float full = ctx != null && ctx.energy != null ? ctx.energy.Fraction : 0f;
            Fade(rechargeSrc, recharging, handPos, rechargeVolume, Mathf.Lerp(0.9f, 1.15f, full));
        }

        private void Fade(AudioSource s, bool on, Vector3 pos, float volume, float pitch)
        {
            if (s == null) return;
            s.transform.position = pos;
            s.pitch = pitch;
            float target = on ? Mathf.Clamp01(volume * masterVolume) : 0f;
            s.volume = Mathf.MoveTowards(s.volume, target, Time.unscaledDeltaTime * (on ? 6f : 3f));
            if (s.volume > 0.001f)
            {
                if (!s.isPlaying) s.Play();
            }
            else if (s.isPlaying)
            {
                s.Stop();
            }
        }
    }
}
