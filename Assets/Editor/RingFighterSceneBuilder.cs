using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RingFighter.EditorTools
{
    /// <summary>
    /// Adds the "Ring Fighter" menu. "Build Training Scene" assembles the whole practice arena
    /// (VR rig, hands, ring moves, training dummy, materials) so nothing has to be wired by hand.
    /// Safe to run again: it rebuilds the scene and reuses the existing materials.
    /// </summary>
    public static class RingFighterSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TrainingArena.unity";
        private const string MaterialFolder = "Assets/Materials";
        private const string VfxFolder = "Assets/VFX";
        private const string VfxShaderFolder = "Assets/VFX/Shaders";
        private const string VfxMaterialFolder = "Assets/VFX/Materials";

        [MenuItem("Ring Fighter/Build Training Scene")]
        public static void BuildTrainingScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Training Scene",
                    "This creates (or replaces) " + ScenePath + " and makes it the only scene in the build.\n\nContinue?",
                    "Build", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            BuildTrainingSceneNoPrompt();
        }

        /// <summary>Same as the menu item, without dialogs. Used by the command-line build.</summary>
        public static void BuildTrainingSceneNoPrompt()
        {
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "VFX");
            EnsureFolder(VfxFolder, "Shaders");
            EnsureFolder(VfxFolder, "Materials");
            EnsureFolder(VfxFolder, "Textures");

            // ---- Materials ----
            Material floorMat = GetMaterial("RF_Floor", new Color(0.13f, 0.15f, 0.2f), false, false);
            Material platformMat = GetMaterial("RF_Platform", new Color(0.1f, 0.5f, 0.3f), false, false);
            Material handMat = GetMaterial("RF_Hand", new Color(0.18f, 0.18f, 0.2f), false, false);
            Material ringMat = GetMaterial("RF_Ring", new Color(0.15f, 1f, 0.4f), true, false);
            Material boltMat = GetMaterial("RF_Bolt", new Color(0.25f, 1f, 0.5f), true, false);
            Material blastMat = GetMaterial("RF_Blast", new Color(0.55f, 1f, 0.8f), true, false);
            Material shieldMat = GetMaterial("RF_Shield", new Color(0.2f, 1f, 0.5f, 0.35f), true, true);
            Material whirlMat = GetMaterial("RF_Whirlwind", new Color(0.5f, 1f, 0.7f, 0.4f), true, true);
            Material dummyMat = GetMaterial("RF_Dummy", new Color(0.78f, 0.78f, 0.82f), false, false);
            Material healthFill = GetMaterial("RF_HealthFill", new Color(0.9f, 0.2f, 0.2f), true, false);
            Material healthBack = GetMaterial("RF_HealthBack", new Color(0.08f, 0.08f, 0.1f), true, false);
            Material energyFill = GetMaterial("RF_EnergyFill", new Color(0.2f, 1f, 0.5f), true, false);
            Material energyBack = GetMaterial("RF_EnergyBack", new Color(0.08f, 0.1f, 0.1f), true, false);
            Material enemyBoltMat = GetMaterial("RF_EnemyBolt", new Color(1f, 0.85f, 0.15f), true, false);
            Material hudHealth = GetMaterial("RF_HudHealth", new Color(0.95f, 0.25f, 0.25f), true, false);
            Material hudEnergy = GetMaterial("RF_HudEnergy", new Color(0.2f, 1f, 0.5f), true, false);
            Material hudBack = GetMaterial("RF_HudBack", new Color(0.05f, 0.07f, 0.07f), true, false);
            Material hudFlash = GetMaterial("RF_HudFlash", new Color(1f, 1f, 1f, 1f), true, true);

            // Effect shader materials (Assets/VFX). The scene's VFX System object references them, which is what
            // keeps the Quest build from stripping the shaders; the effects clone these at runtime.
            Material fxGlow = GetVfxMaterial("RFX_Glow", "RF_Glow");
            Material fxParticle = GetVfxMaterial("RFX_Particle", "RF_Particle");
            Material fxRingDisc = GetVfxMaterial("RFX_RingDisc", "RF_RingDisc");
            Material fxFunnel = GetVfxMaterial("RFX_Funnel", "RF_Funnel");
            Material fxShield = GetVfxMaterial("RFX_Shield", "RF_Shield");
            AssetDatabase.SaveAssets();

            // ---- New scene ----
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.47f, 0.58f);

            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Floor and the spot where the player stands.
            Prim(PrimitiveType.Plane, "Floor", null, Vector3.zero, new Vector3(3f, 1f, 3f), floorMat, true);
            Prim(PrimitiveType.Cylinder, "Player Platform", null, new Vector3(0f, 0.005f, 0f), new Vector3(1.4f, 0.005f, 1.4f), platformMat, false);

            // ---- Player rig ----
            GameObject rigGo = new GameObject("Ring Rig");
            RingRig rig = rigGo.AddComponent<RingRig>();

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rigGo.transform, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.11f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            TrackedNode head = camGo.AddComponent<TrackedNode>();
            head.node = TrackedNode.NodeType.Head;

            TrackedNode left = BuildHand("Left Hand", TrackedNode.NodeType.LeftHand, rigGo.transform, handMat, ringMat);
            TrackedNode right = BuildHand("Right Hand", TrackedNode.NodeType.RightHand, rigGo.transform, handMat, ringMat);
            rig.head = head;
            rig.leftHand = left;
            rig.rightHand = right;

            // ---- Ring brain: energy + moves ----
            GameObject ringGo = new GameObject("Ring Controller");
            RingEnergy energy = ringGo.AddComponent<RingEnergy>();
            RingController controller = ringGo.AddComponent<RingController>();
            controller.rig = rig;
            controller.energy = energy;

            // Priority decides which move wins when one gesture could match several (higher is checked first).
            LaserMove laser = ringGo.AddComponent<LaserMove>();
            laser.beamMaterial = boltMat;
            laser.priority = 0;
            ChargedBlastMove blast = ringGo.AddComponent<ChargedBlastMove>();
            blast.blastMaterial = blastMat;
            blast.priority = 30;
            WhirlwindMove whirl = ringGo.AddComponent<WhirlwindMove>();
            whirl.whirlwindMaterial = whirlMat;
            whirl.priority = 20;
            ShieldMove shield = ringGo.AddComponent<ShieldMove>();
            shield.shieldMaterial = shieldMat;
            shield.priority = 10;

            // Player health, and the HUD that shows health (left) and ring energy (right).
            // (The old floating energy bar above the off hand is gone; EnergyBarVisual is still in the project.)
            PlayerHealth playerHealth = ringGo.AddComponent<PlayerHealth>();
            playerHealth.rig = rig;
            playerHealth.energy = energy;
            playerHealth.controller = controller;
            playerHealth.shield = shield;

            PlayerMovement mover = ringGo.AddComponent<PlayerMovement>();
            mover.rig = rig;
            mover.controller = controller;
            mover.health = playerHealth;

            PauseControl pause = ringGo.AddComponent<PauseControl>();
            pause.rig = rig;

            GameObject hudGo = new GameObject("Player HUD");
            PlayerHud hud = hudGo.AddComponent<PlayerHud>();
            hud.head = camGo.transform;
            hud.health = playerHealth;
            hud.energy = energy;
            hud.healthMaterial = hudHealth;
            hud.energyMaterial = hudEnergy;
            hud.backMaterial = hudBack;
            hud.flashMaterial = hudFlash;

            // ---- Training dummy ----
            GameObject dummyGo = new GameObject("Training Dummy");
            dummyGo.transform.position = new Vector3(0f, 0f, 5.5f);
            TrainingDummy dummy = dummyGo.AddComponent<TrainingDummy>();
            dummy.bodyMaterial = dummyMat;
            dummy.healthBarMaterial = healthFill;
            dummy.healthBarBackMaterial = healthBack;
            dummy.lookTarget = camGo.transform;
            dummy.maxHealth = 200f;
            dummy.attackMaterial = enemyBoltMat;
            // Saved into the scene (not only added at runtime) so the build can't strip them out.
            Rigidbody dummyBody = dummyGo.AddComponent<Rigidbody>();
            dummyBody.isKinematic = true;
            dummyBody.useGravity = false;
            CapsuleCollider dummyHit = dummyGo.AddComponent<CapsuleCollider>();
            dummyHit.center = new Vector3(0f, 1.1f, 0f);
            dummyHit.height = 2.2f;
            dummyHit.radius = 0.35f;

            // ---- Visual effects (shaders, particles; reads the moves, changes no gameplay) ----
            GameObject vfxGo = new GameObject("VFX System");
            VfxLibrary vfxLibrary = vfxGo.AddComponent<VfxLibrary>();
            vfxLibrary.glow = fxGlow;
            vfxLibrary.particle = fxParticle;
            vfxLibrary.ringDisc = fxRingDisc;
            vfxLibrary.funnel = fxFunnel;
            vfxLibrary.shield = fxShield;
            vfxGo.AddComponent<VfxSystem>();

            // ---- Enemy character (if the art is in Assets/Art/Enemy on this PC) ----
            AddEnemyModel(dummyGo, dummy);

            // ---- Save and register ----
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = rigGo;
            AssetDatabase.SaveAssets();

            Debug.Log("Ring Fighter: training scene built at " + ScenePath + ". Press Play with the Quest linked, or build to the headset.");
        }

        // ------------------------------------------------------------------

        // Puts the Enemy01 character inside the dummy instead of the capsule shapes, with its material, animations
        // and the driver that plays them. Without the art files this does nothing and the capsule dummy stays.
        private static void AddEnemyModel(GameObject dummyGo, TrainingDummy dummy)
        {
            EnemyArtSetup.Result art = EnemyArtSetup.Prepare();
            if (art == null || art.model == null) return;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(art.model);
            model.name = "Enemy01";
            model.transform.SetParent(dummyGo.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = art.material;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            // The art should already be about 1.9 m tall; correct it if the import came out at a different size.
            Bounds b = new Bounds();
            bool any = false;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            if (any && b.size.y > 0.3f && Mathf.Abs(b.size.y - 1.9f) > 0.25f)
            {
                Debug.Log("RingFighter enemy: model was " + b.size.y.ToString("0.00") + " m tall, scaling to 1.9 m.");
                model.transform.localScale *= 1.9f / b.size.y;
            }

            Animator anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.avatar = art.avatar;
            anim.runtimeAnimatorController = art.controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            dummy.visualsRoot = model;
            dummy.animatedModel = true;
            dummy.normalTint = Color.white;
            dummy.headMinHeight = 1.6f;    // head of a 1.9 m character
            dummy.hitHeight = 1.95f;
            dummy.getUpTime = art.getUpSeconds;

            EnemyAnimationDriver driver = dummyGo.AddComponent<EnemyAnimationDriver>();
            driver.animator = anim;
        }

        private static TrackedNode BuildHand(string name, TrackedNode.NodeType type, Transform parent, Material handMat, Material ringMat)
        {
            GameObject hand = new GameObject(name);
            hand.transform.SetParent(parent, false);
            TrackedNode tracked = hand.AddComponent<TrackedNode>();
            tracked.node = type;

            // Aim: a child whose forward is the pointing direction. Rotate this if shots feel off-target.
            GameObject aim = new GameObject("Aim");
            aim.transform.SetParent(hand.transform, false);
            tracked.aim = aim.transform;

            // Placeholder controller shape and the glowing ring.
            Prim(PrimitiveType.Capsule, "Controller Body", hand.transform, new Vector3(0f, 0f, -0.02f), new Vector3(0.045f, 0.06f, 0.045f), handMat, false);
            GameObject ring = Prim(PrimitiveType.Cylinder, "Ring", hand.transform, new Vector3(0f, 0f, 0.05f), new Vector3(0.08f, 0.004f, 0.08f), ringMat, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim(PrimitiveType.Sphere, "Ring Gem", hand.transform, new Vector3(0f, 0.04f, 0.05f), Vector3.one * 0.022f, ringMat, false);

            // No aim line: aiming is skill-based (decided 2026-10-18).
            return tracked;
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            return go;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static Material GetMaterial(string name, Color color, bool unlit, bool transparent)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (transparent) MakeTransparent(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        // An effect material: created once in Assets/VFX/Materials with its shader assigned straight from the
        // shader asset (not Shader.Find), and re-assigned on every rebuild so it can't be lost.
        private static Material GetVfxMaterial(string materialName, string shaderFile)
        {
            string shaderPath = VfxShaderFolder + "/" + shaderFile + ".shader";
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
            {
                Debug.LogError("Ring Fighter: effect shader not found at " + shaderPath + ". Effects will be off until it is restored.");
                return null;
            }

            string path = VfxMaterialFolder + "/" + materialName + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void MakeTransparent(Material m)
        {
            SetFloat(m, "_Surface", 1f);
            SetFloat(m, "_Blend", 0f);
            SetFloat(m, "_ZWrite", 0f);
            SetFloat(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster", false);
        }

        private static void SetFloat(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
    }
}
