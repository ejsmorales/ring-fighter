using UnityEngine;

namespace RingFighter
{
    /// <summary>Small helpers for building the placeholder visuals out of basic shapes.</summary>
    public static class Fx
    {
        /// <summary>Creates a primitive shape with no collider, using the given material.</summary>
        public static GameObject Shape(PrimitiveType type, Material material, Vector3 scale, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.localScale = scale;
            if (parent != null) go.transform.SetParent(parent, false);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (material != null) r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return go;
        }

        /// <summary>
        /// Gives a TextMesh a font and material so it actually shows up (one added from code has neither),
        /// and sets its size and color. 'size' is roughly the letter height in meters.
        /// </summary>
        public static void SetupText(TextMesh tm, float size, Color color)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = size * 10f / tm.fontSize;   // line height ~ fontSize * characterSize / 10
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            MeshRenderer mr = tm.GetComponent<MeshRenderer>();
            if (mr != null && font != null) mr.sharedMaterial = font.material;
        }
    }
}
