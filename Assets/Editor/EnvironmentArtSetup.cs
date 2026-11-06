using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RingFighter.EditorTools
{
    /// <summary>
    /// Finds and prepares the Meshy environment props in Assets/Art/Environment/&lt;Name&gt;/ for the arena builder:
    /// import settings (static mesh, no rig, no animation, no imported materials), texture settings (normal map,
    /// linear metallic/smoothness), and a URP Lit material per prop (&lt;Name&gt;_Mat.mat, plus tinted copies on request).
    /// Expected files: &lt;Name&gt;.fbx, &lt;Name&gt;_BaseColor.png, &lt;Name&gt;_Normal.png, &lt;Name&gt;_MetallicSmoothness.png.
    /// Get returns null when a prop isn't on this PC yet; the arena then uses a placeholder shape.
    /// </summary>
    public static class EnvironmentArtSetup
    {
        public const string Root = "Assets/Art/Environment";
        private const string Marker = "RingFighter environment import v1";

        public class Prop
        {
            public string name;
            public string folder;
            public GameObject model;
            public Material material;
        }

        private static readonly Dictionary<string, Prop> cache = new Dictionary<string, Prop>();

        public static void ClearCache()
        {
            cache.Clear();
        }

        public static Prop Get(string name)
        {
            Prop p;
            if (cache.TryGetValue(name, out p)) return p;

            string folder = Root + "/" + name;
            string fbx = folder + "/" + name + ".fbx";
            ModelImporter mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (mi == null)
            {
                cache[name] = null;
                return null;
            }

            if (mi.userData != Marker)
            {
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importCameras = false;
                mi.importLights = false;
                mi.isReadable = false;
                mi.userData = Marker;
                mi.SaveAndReimport();
            }

            ConfigureTexture(folder + "/" + name + "_BaseColor.png", false, false);
            ConfigureTexture(folder + "/" + name + "_Normal.png", true, true);
            ConfigureTexture(folder + "/" + name + "_MetallicSmoothness.png", false, true);

            p = new Prop();
            p.name = name;
            p.folder = folder;
            p.model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            p.material = BuildMaterial(folder, name);
            cache[name] = p;
            Debug.Log("RingFighter arena: using prop " + name);
            return p;
        }

        /// <summary>A copy of the prop's material with its base color multiplied by 'tint' (e.g. car paint colors).</summary>
        public static Material Tinted(Prop p, string tintName, Color tint)
        {
            string path = p.folder + "/" + p.name + "_Mat_" + tintName + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(p.material);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.CopyPropertiesFromMaterial(p.material);
            }
            m.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(m);
            return m;
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

        private static Material BuildMaterial(string folder, string name)
        {
            string path = folder + "/" + name + "_Mat.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name + "_BaseColor.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name + "_Normal.png");
            Texture2D metal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name + "_MetallicSmoothness.png");

            m.SetColor("_BaseColor", Color.white);
            if (baseMap != null) m.SetTexture("_BaseMap", baseMap);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            if (metal != null)
            {
                m.SetTexture("_MetallicGlossMap", metal);
                m.SetFloat("_SmoothnessTextureChannel", 0f);
                m.SetFloat("_Smoothness", 1f);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                m.SetFloat("_Smoothness", 0.25f);
            }
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
