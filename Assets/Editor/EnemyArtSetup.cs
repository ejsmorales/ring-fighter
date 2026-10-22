using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RingFighter.EditorTools
{
    /// <summary>
    /// Prepares the enemy character in Assets/Art/Enemy for the scene, with no clicking in Unity:
    ///   - import settings: Humanoid rig on every FBX, no imported materials, looping idle and charge_shoot,
    ///     clips kept in place (except lifted_falling, whose height the game drives from code),
    ///     normal map and linear metallic/smoothness textures;
    ///   - a URP Lit material (RF_Enemy01.mat) from the three textures;
    ///   - an Animator controller (Enemy01.controller) driven by EnemyAnimationDriver.
    /// Each step is skipped if it was already done, so running it every build is cheap.
    /// Called by RingFighterSceneBuilder. If the art isn't on this PC, Prepare returns null and the scene keeps
    /// the placeholder capsule dummy.
    /// </summary>
    public static class EnemyArtSetup
    {
        public const string Folder = "Assets/Art/Enemy";
        public const string ModelPath = Folder + "/Enemy01.fbx";
        public const string ControllerPath = Folder + "/Enemy01.controller";
        public const string MaterialPath = Folder + "/RF_Enemy01.mat";
        private const string TextureFolder = Folder + "/Textures";
        private const string Marker = "RingFighter enemy import v1";

        private static readonly string[] ClipNames = { "idle", "charge_shoot", "hit_react", "lifted_falling", "knocked_out", "get_up",
                                                       "strafe_left", "strafe_right", "walk_forward", "walk_back" };

        // Optional in-place movement clips. If any exist, idle becomes a "Locomotion" blend between them, driven by
        // the MoveX (sideways) and MoveZ (toward/away from the player) parameters, so he walks instead of sliding.
        private static readonly string[] MoveClips = { "strafe_left", "strafe_right", "walk_forward", "walk_back" };
        private static readonly Vector2[] MoveDirections = { new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f) };

        private static bool Loops(string clip)
        {
            return clip == "idle" || clip == "charge_shoot" || System.Array.IndexOf(MoveClips, clip) >= 0;
        }

        /// <summary>Playback speed of the get-up animation (the clip is 8.3 s long).</summary>
        public const float GetUpSpeed = 2f;

        public class Result
        {
            public GameObject model;
            public Avatar avatar;
            public RuntimeAnimatorController controller;
            public Material material;
            public float getUpSeconds;
        }

        private static string ClipPath(string name)
        {
            return Folder + "/Animations/Enemy01@" + name + ".fbx";
        }

        public static Result Prepare()
        {
            if (AssetImporter.GetAtPath(ModelPath) == null)
            {
                Debug.Log("RingFighter enemy: no " + ModelPath + " on this PC, keeping the placeholder dummy.");
                return null;
            }

            ConfigureModel(ModelPath, false, false, false);
            foreach (string n in ClipNames)
                if (AssetImporter.GetAtPath(ClipPath(n)) != null)
                    ConfigureModel(ClipPath(n), true, Loops(n), n == "lifted_falling");

            ConfigureTexture(TextureFolder + "/Enemy01_BaseColor.png", false, false);
            ConfigureTexture(TextureFolder + "/Enemy01_Normal.png", true, true);
            ConfigureTexture(TextureFolder + "/Enemy01_MetallicSmoothness.png", false, true);

            Result r = new Result();
            r.model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            r.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (r.avatar == null || !r.avatar.isValid || !r.avatar.isHuman)
                Debug.LogWarning("RingFighter enemy: the humanoid avatar for Enemy01 is not valid (bone auto-mapping failed?). The model will show but may not animate.");

            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
            foreach (string n in ClipNames)
            {
                AnimationClip c = AssetDatabase.LoadAllAssetsAtPath(ClipPath(n)).OfType<AnimationClip>()
                    .FirstOrDefault(x => !x.name.StartsWith("__preview__"));
                if (c != null) clips[n] = c;
                else if (System.Array.IndexOf(MoveClips, n) < 0) Debug.LogWarning("RingFighter enemy: no animation clip found in " + ClipPath(n));
            }

            r.material = BuildMaterial();
            r.controller = BuildController(clips);
            AnimationClip getUp;
            r.getUpSeconds = clips.TryGetValue("get_up", out getUp) ? Mathf.Max(0f, getUp.length / GetUpSpeed - 0.2f) : 0f;

            AssetDatabase.SaveAssets();
            Debug.Log("RingFighter enemy: prepared (" + clips.Count + " clips, avatar " + (r.avatar != null && r.avatar.isHuman ? "humanoid OK" : "NOT humanoid") + ").");
            return r;
        }

        // ---- Import settings ----

        private static void ConfigureModel(string path, bool isAnimation, bool loop, bool keepHeightFree)
        {
            ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null || mi.userData == Marker) return;

            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;   // same bone names in every file, so clips retarget
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = isAnimation;
            mi.SaveAndReimport();

            if (isAnimation)
            {
                mi = (ModelImporter)AssetImporter.GetAtPath(path);
                ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = loop;
                    // Keep the animation in place: the game moves the dummy itself.
                    clips[i].lockRootRotation = true;
                    clips[i].lockRootPositionXZ = true;
                    clips[i].lockRootHeightY = !keepHeightFree;   // lifted_falling: the code drives the height
                    clips[i].keepOriginalOrientation = true;
                    clips[i].keepOriginalPositionXZ = true;
                    clips[i].keepOriginalPositionY = true;
                }
                mi.clipAnimations = clips;
            }
            mi.userData = Marker;
            mi.SaveAndReimport();
        }

        private static void ConfigureTexture(string path, bool normalMap, bool linear)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null || ti.userData == Marker) return;
            ti.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !linear;
            ti.maxTextureSize = 2048;
            ti.mipmapEnabled = true;
            ti.userData = Marker;
            ti.SaveAndReimport();
        }

        // ---- Material ----

        private static Material BuildMaterial()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (m == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(lit);
                AssetDatabase.CreateAsset(m, MaterialPath);
            }

            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Enemy01_BaseColor.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Enemy01_Normal.png");
            Texture2D metal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Enemy01_MetallicSmoothness.png");

            m.SetColor("_BaseColor", Color.white);
            if (baseMap != null) m.SetTexture("_BaseMap", baseMap);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            if (metal != null)
            {
                m.SetTexture("_MetallicGlossMap", metal);
                m.SetFloat("_SmoothnessTextureChannel", 0f);   // smoothness from the metallic map's alpha
                m.SetFloat("_Smoothness", 1f);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---- Animator controller ----

        private static RuntimeAnimatorController BuildController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            bool wantMove = MoveClips.Any(c => clips.ContainsKey(c));
            bool hasMove = existing != null && existing.parameters.Any(p => p.name == "MoveX");
            if (existing != null && existing.layers.Length > 0 && existing.layers[0].stateMachine.states.Length >= 6 && hasMove == wantMove)
                return existing;   // already built for this set of clips: keep it (and its GUID) as is
            if (existing != null) AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Alive", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Airborne", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Charging", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            if (wantMove)
            {
                ac.AddParameter("MoveX", AnimatorControllerParameterType.Float);
                ac.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
            }
            AnimatorControllerParameter[] ps = ac.parameters;
            for (int i = 0; i < ps.Length; i++)
                if (ps[i].name == "Alive") ps[i].defaultBool = true;
            ac.parameters = ps;

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            AnimatorState idle = wantMove ? Locomotion(ac, sm, clips) : State(sm, "Idle", clips, "idle", 1f);
            AnimatorState charge = State(sm, "Charge", clips, "charge_shoot", 1f);
            AnimatorState hit = State(sm, "Hit", clips, "hit_react", 1f);
            AnimatorState lifted = State(sm, "Lifted", clips, "lifted_falling", 1f);
            AnimatorState down = State(sm, "KnockedOut", clips, "knocked_out", 1f);
            AnimatorState getUp = State(sm, "GetUp", clips, "get_up", GetUpSpeed);
            sm.defaultState = idle;

            // Charging glow: aim and shoot.
            AnimatorStateTransition t = idle.AddTransition(charge);
            Instant(t, 0.1f);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Charging");
            t = charge.AddTransition(idle);
            Instant(t, 0.15f);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Charging");

            // Flinch when hit (not while flying or knocked out), then back to idle.
            t = sm.AddAnyStateTransition(hit);
            Instant(t, 0.05f);
            t.canTransitionToSelf = true;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            t.AddCondition(AnimatorConditionMode.If, 0f, "Alive");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Airborne");
            t = hit.AddTransition(idle);
            t.hasExitTime = true;
            t.exitTime = 0.85f;
            t.duration = 0.15f;

            // Lifted by the whirlwind.
            t = sm.AddAnyStateTransition(lifted);
            Instant(t, 0.1f);
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Airborne");
            t.AddCondition(AnimatorConditionMode.If, 0f, "Alive");
            t = lifted.AddTransition(idle);
            Instant(t, 0.2f);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Airborne");

            // Knocked out, then get up when it respawns.
            t = sm.AddAnyStateTransition(down);
            Instant(t, 0.15f);
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Alive");
            t = down.AddTransition(getUp);
            Instant(t, 0.2f);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Alive");
            t = getUp.AddTransition(idle);
            t.hasExitTime = true;
            t.exitTime = 0.95f;
            t.duration = 0.2f;

            EditorUtility.SetDirty(ac);
            return ac;
        }

        // Idle in the middle, a movement clip in each direction; Unity blends between them by MoveX / MoveZ.
        private static AnimatorState Locomotion(AnimatorController ac, AnimatorStateMachine sm, Dictionary<string, AnimationClip> clips)
        {
            BlendTree tree = new BlendTree();
            tree.name = "Locomotion";
            tree.blendType = BlendTreeType.SimpleDirectional2D;
            tree.blendParameter = "MoveX";
            tree.blendParameterY = "MoveZ";
            tree.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(tree, ac);

            AnimationClip c;
            if (clips.TryGetValue("idle", out c)) tree.AddChild(c, Vector2.zero);
            for (int i = 0; i < MoveClips.Length; i++)
                if (clips.TryGetValue(MoveClips[i], out c)) tree.AddChild(c, MoveDirections[i]);

            AnimatorState s = sm.AddState("Locomotion");
            s.motion = tree;
            return s;
        }

        private static AnimatorState State(AnimatorStateMachine sm, string name, Dictionary<string, AnimationClip> clips, string clip, float speed)
        {
            AnimatorState s = sm.AddState(name);
            AnimationClip c;
            if (clips.TryGetValue(clip, out c)) s.motion = c;
            s.speed = speed;
            return s;
        }

        private static void Instant(AnimatorStateTransition t, float blend)
        {
            t.hasExitTime = false;
            t.duration = blend;
        }
    }
}
