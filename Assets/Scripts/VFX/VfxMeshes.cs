using UnityEngine;
using UnityEngine.Rendering;

namespace RingFighter
{
    /// <summary>Meshes Unity doesn't have built in, made once from code.</summary>
    public static class VfxMeshes
    {
        private static Mesh funnel;

        /// <summary>
        /// An open funnel, 1 unit tall (y 0 to 1), narrower at the bottom (radius 0.55) than the top (radius 1).
        /// UV: u = around (0 to 1), v = height (0 bottom to 1 top). Scale it to size it.
        /// </summary>
        public static Mesh Funnel
        {
            get
            {
                if (funnel == null) funnel = BuildFunnel(24, 8, 0.55f, 1f);
                return funnel;
            }
        }

        private static Mesh BuildFunnel(int segments, int rings, float bottomRadius, float topRadius)
        {
            int cols = segments + 1;
            int rows = rings + 1;
            Vector3[] verts = new Vector3[cols * rows];
            Vector2[] uvs = new Vector2[cols * rows];
            int[] tris = new int[segments * rings * 6];

            for (int j = 0; j < rows; j++)
            {
                float v = j / (float)rings;
                float radius = Mathf.Lerp(bottomRadius, topRadius, Mathf.Pow(v, 1.4f));
                for (int i = 0; i < cols; i++)
                {
                    float u = i / (float)segments;
                    float a = u * Mathf.PI * 2f;
                    verts[j * cols + i] = new Vector3(Mathf.Cos(a) * radius, v, Mathf.Sin(a) * radius);
                    uvs[j * cols + i] = new Vector2(u, v);
                }
            }

            int t = 0;
            for (int j = 0; j < rings; j++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int a = j * cols + i;
                    int b = a + 1;
                    int c = a + cols;
                    int d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            }

            Mesh m = new Mesh();
            m.name = "RF Funnel";
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Makes a funnel object (no collider, no shadows) using 'material', under 'parent'.</summary>
        public static GameObject CreateFunnel(string name, Material material, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Funnel;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }
    }
}
