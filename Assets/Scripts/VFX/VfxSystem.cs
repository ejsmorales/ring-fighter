using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RingFighter
{
    /// <summary>
    /// Sits on the "VFX System" object that RingFighterSceneBuilder puts in the scene, next to VfxLibrary (which holds
    /// the shader materials the scene references). It does three jobs:
    ///   1. Attaches a visual component to each ring move (laser, charged blast, shield, whirlwind). The moves keep
    ///      all the gameplay; these components only read their state and draw the glow.
    ///   2. Owns the pooled effect objects (BurstFx).
    ///   3. Switches real post-processing bloom on and off (pause menu, B button). Off by default: the effects fake
    ///      their glow with additive shaders, which costs far less on Quest 3.
    /// If the library is missing (an old scene) nothing is attached and the old placeholder shapes stay.
    /// </summary>
    [RequireComponent(typeof(VfxLibrary))]
    public class VfxSystem : MonoBehaviour
    {
        public static VfxSystem Instance { get; private set; }

        private Volume bloomVolume;
        private bool bloomApplied;
        private bool attached;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            VfxLibrary library = GetComponent<VfxLibrary>();
            if (library != null) library.Register();
            AttachEffects();
        }

        private void OnEnable()
        {
            VfxSettings.Changed += OnSettingsChanged;
        }

        private void OnDisable()
        {
            VfxSettings.Changed -= OnSettingsChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void AttachEffects()
        {
            if (!VfxMaterials.Ready)
            {
                MoveLog.Add("VFX unavailable (rebuild scene)");
                return;
            }

            LaserMove laser = FindAnyObjectByType<LaserMove>();
            if (laser != null && laser.GetComponent<LaserVfx>() == null)
                laser.gameObject.AddComponent<LaserVfx>().Init(laser);

            ChargedBlastMove blast = FindAnyObjectByType<ChargedBlastMove>();
            if (blast != null && blast.GetComponent<BlastChargeVfx>() == null)
                blast.gameObject.AddComponent<BlastChargeVfx>().Init(blast);

            ShieldMove shield = FindAnyObjectByType<ShieldMove>();
            if (shield != null && shield.GetComponent<ShieldVfx>() == null)
                shield.gameObject.AddComponent<ShieldVfx>().Init(shield);

            WhirlwindMove whirl = FindAnyObjectByType<WhirlwindMove>();
            if (whirl != null && whirl.GetComponent<WhirlwindVfx>() == null)
                whirl.gameObject.AddComponent<WhirlwindVfx>().Init(whirl);

            gameObject.AddComponent<VfxPauseMenu>();
            attached = true;
            MoveLog.Add("VFX ready (" + VfxSettings.QualityLabel + ")");
        }

        // ---- Bloom ----

        private void OnSettingsChanged()
        {
            bloomApplied = false;   // re-apply on the next Update
        }

        private void Update()
        {
            if (!attached) return;
            if (!bloomApplied) ApplyBloom();
        }

        private void ApplyBloom()
        {
            bool want = VfxSettings.Bloom;
            Camera cam = Camera.main;
            if (want && cam == null) return;   // try again next frame

            if (cam != null)
            {
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = want;
            }

            if (want && bloomVolume == null) BuildBloom();
            if (bloomVolume != null) bloomVolume.enabled = want;
            bloomApplied = true;
            MoveLog.Add("Bloom " + (want ? "ON" : "off"));
        }

        private void BuildBloom()
        {
            GameObject go = new GameObject("VFX Bloom Volume");
            go.transform.SetParent(transform, false);
            bloomVolume = go.AddComponent<Volume>();
            bloomVolume.isGlobal = true;
            bloomVolume.priority = 10f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.6f);
            bloomVolume.sharedProfile = profile;
        }
    }
}
