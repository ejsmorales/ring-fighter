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
    }
}
