using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RingFighter.EditorTools
{
    /// <summary>
    /// Builds the "mall parking lot" arena into the scene (called by RingFighterSceneBuilder):
    ///   - overcast sky, soft light, haze;
    ///   - a big asphalt floor with painted parking lines, yellow curbs, grass islands, a sidewalk and parking stops;
    ///   - distant green hills with small houses;
    ///   - props (cars, light poles, palms, shrubs, store front...) laid out around the edges, keeping the middle
    ///     (about 11 m around you) open for fighting.
    /// Each prop uses the Meshy model from Assets/Art/Environment/&lt;Name&gt;/ if it exists, otherwise a simple placeholder.
    /// Nothing here has colliders except the floor, so props never block shots or movement.
    /// The player starts at the origin facing +Z; the store front is ahead, like the reference photo.
    /// </summary>
    public static class ArenaBuilder
    {
        private const string GenFolder = "Assets/Art/Generated";

        // Layout (meters). The fight happens inside FightRadius.
        private const float FightRadius = 11f;
        private const float RowNear = 20f;     // first parking row starts here (north and south)
        private const float RowDepth = 5f;     // stall depth
        private const float RowHalfWidth = 33f;
        private const float SideRowNear = 22f; // east/west rows
        private const float StoreFrontZ = 44f;
        private static readonly Vector3 WreckPos = new Vector3(-12.5f, 0f, -22.5f);   // crashed into the south row

        private static System.Random rng;
        private static Transform root;
        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        private static readonly string[] CarTintNames = { "White", "Silver", "Red", "Blue", "Black", "Grey" };
        private static readonly Color[] CarTints =
        {
            new Color(0.95f, 0.95f, 0.95f), new Color(0.72f, 0.74f, 0.77f), new Color(0.7f, 0.08f, 0.08f),
            new Color(0.12f, 0.2f, 0.42f), new Color(0.08f, 0.08f, 0.09f), new Color(0.4f, 0.42f, 0.44f)
        };

        public static void Build(Camera cam)
        {
            rng = new System.Random(20261106);
            mats.Clear();
            EnvironmentArtSetup.ClearCache();
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Generated");

            root = new GameObject("Arena").transform;

            BuildSkyAndLight(cam);
            BuildGround();
            BuildHills();
            PlaceProps();

            MarkStatic(root.gameObject);
            AssetDatabase.SaveAssets();
            Debug.Log("RingFighter arena: parking lot built.");
        }

        // ---------------------------------------------------------------- sky, light, haze

        private static void BuildSkyAndLight(Camera cam)
        {
            Material sky = GetMaterial("RF_Sky_Overcast", "Skybox/Procedural", Color.white);
            if (sky.HasProperty("_SunDisk")) sky.SetFloat("_SunDisk", 0f);
            SetF(sky, "_SunSize", 0f);
            SetF(sky, "_AtmosphereThickness", 1.6f);
            SetF(sky, "_Exposure", 1.15f);
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.62f, 0.64f, 0.68f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.45f, 0.47f, 0.48f));
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.56f, 0.58f, 0.62f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.7f, 0.72f, 0.75f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 340f;

            GameObject lightGo = new GameObject("Overcast Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.95f;
            light.color = new Color(0.96f, 0.97f, 1f);
            light.shadows = LightShadows.None;   // soft overcast look, and cheaper on Quest
            lightGo.transform.rotation = Quaternion.Euler(58f, -25f, 0f);

            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.farClipPlane = 450f;
            }
        }

        // ---------------------------------------------------------------- ground

        private static void BuildGround()
        {
            // Asphalt: one big plane (with the only collider in the arena: shots hit the floor).
            Material asphalt = AsphaltMaterial();
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root, false);
            floor.transform.localScale = new Vector3(40f, 1f, 40f);   // 400 x 400 m
            floor.GetComponent<Renderer>().sharedMaterial = asphalt;

            MeshBuilder lines = new MeshBuilder();     // painted yellow lines
            MeshBuilder curbs = new MeshBuilder();     // yellow curbs and parking stops
            MeshBuilder grass = new MeshBuilder();     // island lawns
            MeshBuilder concrete = new MeshBuilder();  // sidewalk

            // North and south double rows, split by a middle island.
            foreach (float sign in new[] { 1f, -1f })
            {
                float z0 = sign * RowNear;
                float z1 = sign * (RowNear + RowDepth);
                float z2 = sign * (RowNear + RowDepth * 2f);
                StallLinesAlongZ(lines, curbs, -RowHalfWidth, -2f, z0, z1, 11, sign);
                StallLinesAlongZ(lines, curbs, 2f, RowHalfWidth, z0, z1, 11, sign);
                StallLinesAlongZ(lines, curbs, -RowHalfWidth, -2f, z1, z2, 11, -sign);
                StallLinesAlongZ(lines, curbs, 2f, RowHalfWidth, z1, z2, 11, -sign);
                lines.FlatQuad(new Vector3(0f, 0.012f, z1), new Vector2(RowHalfWidth * 2f, 0.12f));   // row divider

                // End islands: a light pole toward the outer row, a tree and shrubs. Middle island: a pole.
                Island(curbs, grass, -RowHalfWidth - 3f, -RowHalfWidth, Mathf.Min(z0, z2), Mathf.Max(z0, z2), sign * 3f);
                Island(curbs, grass, RowHalfWidth, RowHalfWidth + 3f, Mathf.Min(z0, z2), Mathf.Max(z0, z2), sign * 3f);
                Island(curbs, grass, -2f, 2f, Mathf.Min(z0, z2), Mathf.Max(z0, z2), 0f);
            }

            // East and west single rows.
            foreach (float sign in new[] { 1f, -1f })
            {
                float x0 = sign * SideRowNear;
                float x1 = sign * (SideRowNear + RowDepth);
                StallLinesAlongX(lines, curbs, x0, x1, -16f, 16f, 10, sign);
                Island(curbs, grass, Mathf.Min(x1, x1 + sign * 2.5f), Mathf.Max(x1, x1 + sign * 2.5f), -16f, 16f, -10f, 10f);
            }

            // Sidewalk with a yellow curb in front of the stores.
            concrete.Box(new Vector3(0f, 0.075f, StoreFrontZ - 2f), new Vector3(170f, 0.15f, 4f));
            curbs.Box(new Vector3(0f, 0.08f, StoreFrontZ - 4.05f), new Vector3(170f, 0.16f, 0.15f));
            // Crosswalk stripes toward the entrance.
            for (int i = 0; i < 8; i++)
                lines.FlatQuad(new Vector3(-3.5f + i, 0.012f, StoreFrontZ - 7f), new Vector2(0.45f, 5f), "White");

            AddMesh("Parking Lines", lines, Paint(new Color(0.86f, 0.72f, 0.2f)), Paint(new Color(0.9f, 0.9f, 0.88f)));
            AddMesh("Curbs", curbs, Lit("RF_Env_Curb", new Color(0.88f, 0.74f, 0.22f), 0.15f));
            AddMesh("Island Grass", grass, Lit("RF_Env_Grass", new Color(0.36f, 0.5f, 0.2f), 0.05f));
            AddMesh("Sidewalk", concrete, Lit("RF_Env_Concrete", new Color(0.7f, 0.69f, 0.66f), 0.1f));
        }

        // A row of stalls whose lines run along Z (cars face north/south). 'facing' = +1 or -1: which way noses point.
        private static void StallLinesAlongZ(MeshBuilder lines, MeshBuilder curbs, float xa, float xb, float za, float zb, int stalls, float facing)
        {
            float w = (xb - xa) / stalls;
            float zMid = (za + zb) * 0.5f;
            float len = Mathf.Abs(zb - za);
            for (int i = 0; i <= stalls; i++)
                lines.FlatQuad(new Vector3(xa + i * w, 0.012f, zMid), new Vector2(0.12f, len));
            for (int i = 0; i < stalls; i++)
            {
                float x = xa + (i + 0.5f) * w;
                // Parking stop near the far end of the stall.
                float zStop = zb - Mathf.Sign(zb - za) * 0.6f;
                curbs.Box(new Vector3(x, 0.07f, zStop), new Vector3(1.7f, 0.14f, 0.2f));
                ParkedCar(new Vector3(x, 0f, zMid), Mathf.Sign(zb - za) > 0f ? 0f : 180f);
            }
        }

        // A row of stalls whose lines run along X (cars face east/west).
        private static void StallLinesAlongX(MeshBuilder lines, MeshBuilder curbs, float xa, float xb, float za, float zb, int stalls, float facing)
        {
            float w = (zb - za) / stalls;
            float xMid = (xa + xb) * 0.5f;
            float len = Mathf.Abs(xb - xa);
            for (int i = 0; i <= stalls; i++)
                lines.FlatQuad(new Vector3(xMid, 0.012f, za + i * w), new Vector2(len, 0.12f));
            for (int i = 0; i < stalls; i++)
            {
                float z = za + (i + 0.5f) * w;
                float xStop = xb - Mathf.Sign(xb - xa) * 0.6f;
                curbs.Box(new Vector3(xStop, 0.07f, z), new Vector3(0.2f, 0.14f, 1.7f));
                ParkedCar(new Vector3(xMid, 0f, z), Mathf.Sign(xb - xa) > 0f ? 90f : -90f);
            }
        }

        // Raised grass island with a yellow curb. 'poles' = light pole spots (meters from the island's center along
        // its length); a palm or tree goes in the middle unless a pole is there, and shrubs fill the rest.
        private static void Island(MeshBuilder curbs, MeshBuilder grass, float xa, float xb, float za, float zb, params float[] poles)
        {
            Vector3 c = new Vector3((xa + xb) * 0.5f, 0f, (za + zb) * 0.5f);
            Vector3 size = new Vector3(Mathf.Abs(xb - xa), 0.16f, Mathf.Abs(zb - za));
            curbs.Box(c + Vector3.up * 0.08f, size);
            grass.FlatQuad(c + Vector3.up * 0.172f, new Vector2(size.x - 0.3f, size.z - 0.3f));

            bool alongZ = size.z > size.x;
            float len = alongZ ? size.z : size.x;
            Vector3 axis = alongZ ? Vector3.forward : Vector3.right;
            Vector3 top = Vector3.up * 0.16f;
            List<float> taken = new List<float>();
            foreach (float p in poles)
            {
                Spawn("Env_LightPole", c + axis * p + top, alongZ ? 0f : 90f, null);
                taken.Add(p);
            }
            if (!Near(taken, 0f, 1.5f))
            {
                Spawn(rng.NextDouble() < 0.6 ? "Env_PalmTree" : "Env_StreetTree", c + top, Rand(0f, 360f), null);
                taken.Add(0f);
            }
            for (float d = -len * 0.5f + 1.2f; d <= len * 0.5f - 1.2f; d += 2.4f)
                if (!Near(taken, d, 1.3f)) Spawn("Env_Shrub", c + axis * d + top, Rand(0f, 360f), null);
        }

        private static bool Near(List<float> list, float value, float range)
        {
            foreach (float x in list)
                if (Mathf.Abs(x - value) < range) return true;
            return false;
        }

        // ---------------------------------------------------------------- distant hills with houses

        private static void BuildHills()
        {
            MeshBuilder hills = new MeshBuilder();
            float[] radii = { 150f, 185f, 220f, 270f };
            int segments = 120;
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * 360f / segments, a1 = (s + 1) * 360f / segments;
                for (int r = 0; r < radii.Length - 1; r++)
                {
                    Vector3 p00 = HillPoint(a0, radii[r]), p01 = HillPoint(a0, radii[r + 1]);
                    Vector3 p10 = HillPoint(a1, radii[r]), p11 = HillPoint(a1, radii[r + 1]);
                    Vector3 inwardUp = (-((p00 + p11) * 0.5f).normalized + Vector3.up * 2f);
                    hills.Quad(p00, p01, p11, p10, inwardUp);
                }
            }
            AddMesh("Hills", hills, Lit("RF_Env_Hills", new Color(0.3f, 0.42f, 0.22f), 0f));

            // Little houses on the slopes all around, except straight ahead when the Meshy hillside town is there.
            bool backdrop = EnvironmentArtSetup.Get("Env_HillsideTown") != null;
            MeshBuilder[] houses = { new MeshBuilder(), new MeshBuilder(), new MeshBuilder(), new MeshBuilder() };
            for (int i = 0; i < 420; i++)
            {
                float a = Rand(0f, 360f);
                float dist = Rand(170f, 235f);
                if (backdrop && Mathf.DeltaAngle(a, 0f) > -50f && Mathf.DeltaAngle(a, 0f) < 50f) continue;
                Vector3 p = HillPoint(a, dist);
                Vector3 size = new Vector3(Rand(4f, 8f), Rand(3f, 5.5f), Rand(4f, 7f));
                MeshBuilder mb = houses[rng.Next(houses.Length)];
                mb.Box(p + Vector3.up * (size.y * 0.5f - 0.5f), size, a);
            }
            AddMesh("Houses White", houses[0], Lit("RF_Env_HouseWhite", new Color(0.88f, 0.86f, 0.82f), 0.1f));
            AddMesh("Houses Peach", houses[1], Lit("RF_Env_HousePeach", new Color(0.9f, 0.72f, 0.58f), 0.1f));
            AddMesh("Houses Blue", houses[2], Lit("RF_Env_HouseBlue", new Color(0.62f, 0.72f, 0.82f), 0.1f));
            AddMesh("Houses Yellow", houses[3], Lit("RF_Env_HouseYellow", new Color(0.9f, 0.82f, 0.5f), 0.1f));

            // Meshy backdrop if available: straight ahead behind the stores, facing the player.
            if (backdrop)
                Spawn("Env_HillsideTown", new Vector3(0f, 0f, 205f), 180f, null);   // 260 m wide, ~70 m deep
        }

        private static Vector3 HillPoint(float angleDeg, float radius)
        {
            float t = Mathf.InverseLerp(150f, 270f, radius);
            float n = Mathf.PerlinNoise(angleDeg * 0.035f, 1.7f) * 0.7f + Mathf.PerlinNoise(angleDeg * 0.12f, 5.3f) * 0.3f;
            float h = -2f + t * (18f + 55f * n);
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * radius, h, Mathf.Cos(rad) * radius);
        }

        // ---------------------------------------------------------------- props

        private static void PlaceProps()
        {
            PlaceStores();

            // (Light poles are placed by the islands.)

            // Drive-lane details like the photo.
            Spawn("Env_WatchTower", new Vector3(-7f, 0f, 35f), 160f, null);
            Spawn("Env_StopSign", new Vector3(37f, 0f, 34f), -90f, null);
            Spawn("Env_CartCorral", new Vector3(13f, 0f, 33.5f), 0f, null);
            Spawn("Env_ShoppingCart", new Vector3(14.2f, 0f, 31.5f), 15f, null);
            Spawn("Env_ShoppingCart", new Vector3(-24.5f, 0f, 17.5f), 120f, null);

            // Signs of the fight: a wrecked car knocked into the south row.
            Spawn("Env_WreckedCar", WreckPos, 35f, null);
        }

        // Store fronts in a strip ahead, side by side by their real widths. Uses every variant that exists
        // (Env_StoreFront, Env_StoreFront_B ... _F); if there are too few to fill the strip, repeats them tinted.
        private static void PlaceStores()
        {
            List<string> kinds = new List<string> { "Env_StoreFront" };
            foreach (string v in new[] { "B", "C", "D", "E", "F" })
                if (EnvironmentArtSetup.Get("Env_StoreFront_" + v) != null) kinds.Add("Env_StoreFront_" + v);

            Color[] shades = { new Color(0.92f, 0.86f, 0.8f), new Color(0.85f, 0.88f, 0.92f), new Color(0.9f, 0.9f, 0.84f) };
            const float stripWidth = 150f, gap = 1.5f;
            List<GameObject> stores = new List<GameObject>();
            List<float> widths = new List<float>();
            float total = 0f;
            for (int i = 0; total < stripWidth && i < 12; i++)
            {
                string kind = kinds[i % kinds.Count];
                int round = i / kinds.Count;
                Color? shade = round == 0 ? (Color?)null : shades[(round - 1) % shades.Length];
                GameObject go = Spawn(kind, Vector3.zero, 180f, shade, false, shade.HasValue ? "Shade" + round : null);
                Bounds b;
                float w = TryBounds(go, out b) ? b.size.x : 40f;
                stores.Add(go);
                widths.Add(w);
                total += w + gap;
            }
            if (kinds.Count == 1) Debug.Log("RingFighter arena: only one store front design, repeated with tints. Add Env_StoreFront_B, _C... for variety.");

            float x = -total * 0.5f;
            for (int i = 0; i < stores.Count; i++)
            {
                GameObject go = stores[i];
                go.transform.position = new Vector3(x + widths[i] * 0.5f, 0f, StoreFrontZ);
                Bounds b;
                if (TryBounds(go, out b)) go.transform.position += Vector3.forward * (StoreFrontZ - b.min.z);
                x += widths[i] + gap;
            }
        }

        private static void ParkedCar(Vector3 pos, float yaw)
        {
            if ((pos - WreckPos).magnitude < 3.5f) return;   // the wreck took this spot
            if (rng.NextDouble() > 0.62) return;   // empty stall
            double roll = rng.NextDouble();
            string type = roll < 0.55 ? "Env_Sedan" : (roll < 0.9 ? "Env_SUV" : "Env_Van");
            int tint = rng.Next(CarTints.Length);
            if (rng.NextDouble() < 0.2) yaw += 180f;   // some reversed in
            pos += new Vector3(Rand(-0.15f, 0.15f), 0f, Rand(-0.15f, 0.15f));
            Spawn(type, pos, yaw + Rand(-4f, 4f), CarTints[tint], false, CarTintNames[tint]);
        }

        // Places the Meshy prop if present, else a placeholder. 'alignFront' moves a store front so its face sits at pos.z.
        private static GameObject Spawn(string name, Vector3 pos, float yaw, Color? tint, bool alignFront = false, string tintName = null)
        {
            EnvironmentArtSetup.Prop prop = EnvironmentArtSetup.Get(name);
            GameObject go;
            if (prop != null && prop.model != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prop.model);
                Material m = tint.HasValue ? EnvironmentArtSetup.Tinted(prop, tintName ?? ColorKey(tint.Value), tint.Value) : prop.material;
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] list = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                    for (int i = 0; i < list.Length; i++) list[i] = m;
                    r.sharedMaterials = list;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
                FixScale(go, name);
            }
            else
            {
                go = Placeholder(name, tint);
            }
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            if (alignFront)
            {
                Bounds b;
                if (TryBounds(go, out b)) go.transform.position += Vector3.forward * (pos.z - b.min.z);
            }
            return go;
        }

        // Meshy sizes can come out in odd units; scale anything far from its expected height.
        private static void FixScale(GameObject go, string name)
        {
            float expected;
            switch (name)
            {
                case "Env_Sedan": expected = 1.45f; break;
                case "Env_SUV": expected = 1.75f; break;
                case "Env_Van": expected = 2.6f; break;
                case "Env_WreckedCar": expected = 1.2f; break;
                case "Env_LightPole": expected = 10f; break;
                case "Env_PalmTree": expected = 6f; break;
                case "Env_Shrub": expected = 1f; break;
                case "Env_StreetTree": expected = 4f; break;
                case "Env_WatchTower": expected = 6f; break;
                case "Env_StopSign": expected = 2.5f; break;
                case "Env_ShoppingCart": expected = 1f; break;
                case "Env_CartCorral": expected = 2.15f; break;   // includes its CART RETURN sign
                case "Env_StoreFront": expected = 10f; break;
                default: return;
            }
            Bounds b;
            if (!TryBounds(go, out b) || b.size.y < 0.01f) return;
            float ratio = b.size.y / expected;
            if (ratio > 1.4f || ratio < 0.7f)
            {
                go.transform.localScale *= expected / b.size.y;
                Debug.Log("RingFighter arena: " + name + " was " + b.size.y.ToString("0.00") + " m tall, scaled to " + expected + " m.");
            }
        }

        private static bool TryBounds(GameObject go, out Bounds b)
        {
            b = new Bounds();
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return any;
        }

        // ---------------------------------------------------------------- placeholders (until the Meshy props arrive)

        private static GameObject Placeholder(string name, Color? tint)
        {
            GameObject g = new GameObject(name);
            Color paint = tint ?? new Color(0.75f, 0.75f, 0.78f);
            Material glass = Lit("RF_Env_Glass", new Color(0.1f, 0.12f, 0.15f), 0.8f);
            Material metal = Lit("RF_Env_Metal", new Color(0.45f, 0.47f, 0.5f), 0.5f);
            Material leaf = Lit("RF_Env_Leaf", new Color(0.22f, 0.45f, 0.18f), 0.05f);
            Material trunk = Lit("RF_Env_Trunk", new Color(0.45f, 0.36f, 0.26f), 0.05f);
            switch (name)
            {
                case "Env_Sedan":
                case "Env_SUV":
                case "Env_Van":
                case "Env_WreckedCar":
                {
                    bool van = name == "Env_Van", suv = name == "Env_SUV", wreck = name == "Env_WreckedCar";
                    Material body = wreck ? Lit("RF_Env_Scorched", new Color(0.12f, 0.1f, 0.09f), 0.1f) : Lit("RF_Env_Car" + ColorKey(paint), paint, 0.6f);
                    float h = van ? 1.3f : (suv ? 0.95f : 0.75f);
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 0.35f + h * 0.5f, 0f), new Vector3(1.85f, h, van ? 5f : 4.5f), body);
                    if (!van) Part(g, PrimitiveType.Cube, new Vector3(0f, 0.35f + h + 0.3f, -0.2f), new Vector3(1.6f, 0.6f, suv ? 2.8f : 2.2f), glass);
                    else Part(g, PrimitiveType.Cube, new Vector3(0f, 1.95f, 1.7f), new Vector3(1.7f, 0.5f, 1.2f), glass);
                    if (wreck) g.transform.GetChild(0).localRotation = Quaternion.Euler(6f, 0f, 14f);
                    break;
                }
                case "Env_LightPole":
                    Part(g, PrimitiveType.Cylinder, new Vector3(0f, 5f, 0f), new Vector3(0.22f, 5f, 0.22f), metal);
                    Part(g, PrimitiveType.Cube, new Vector3(0.9f, 9.9f, 0f), new Vector3(1.9f, 0.15f, 0.4f), metal);
                    break;
                case "Env_PalmTree":
                    Part(g, PrimitiveType.Cylinder, new Vector3(0f, 3f, 0f), new Vector3(0.3f, 3f, 0.3f), trunk);
                    Part(g, PrimitiveType.Sphere, new Vector3(0f, 6.1f, 0f), new Vector3(3.2f, 0.9f, 3.2f), leaf);
                    break;
                case "Env_StreetTree":
                    Part(g, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(0.2f, 1.2f, 0.2f), trunk);
                    Part(g, PrimitiveType.Sphere, new Vector3(0f, 3.1f, 0f), new Vector3(2.6f, 2.2f, 2.6f), leaf);
                    break;
                case "Env_Shrub":
                    Part(g, PrimitiveType.Sphere, new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 0.9f, 1.1f), leaf);
                    break;
                case "Env_WatchTower":
                    Part(g, PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.3f, 2.2f, 0.3f), metal);
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 5.2f, 0f), new Vector3(2f, 1.8f, 2f), glass);
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 6.2f, 0f), new Vector3(2.3f, 0.2f, 2.3f), metal);
                    break;
                case "Env_StopSign":
                    Part(g, PrimitiveType.Cylinder, new Vector3(0f, 1.1f, 0f), new Vector3(0.06f, 1.1f, 0.06f), metal);
                    Part(g, PrimitiveType.Cylinder, new Vector3(0f, 2.3f, 0.05f), new Vector3(0.75f, 0.02f, 0.75f), Lit("RF_Env_SignRed", new Color(0.75f, 0.08f, 0.08f), 0.3f))
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
                case "Env_ShoppingCart":
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 0.65f, 0f), new Vector3(0.55f, 0.5f, 0.9f), metal);
                    break;
                case "Env_CartCorral":
                    Part(g, PrimitiveType.Cube, new Vector3(-0.7f, 0.55f, 0f), new Vector3(0.06f, 1.1f, 4f), metal);
                    Part(g, PrimitiveType.Cube, new Vector3(0.7f, 0.55f, 0f), new Vector3(0.06f, 1.1f, 4f), metal);
                    break;
                case "Env_StoreFront":
                {
                    Color wall = tint ?? new Color(0.86f, 0.8f, 0.68f);
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 5f, 7.5f), new Vector3(40f, 10f, 15f), Lit("RF_Env_Wall" + ColorKey(wall), wall, 0.1f));
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 8f, -0.05f), new Vector3(16f, 2.6f, 0.2f), Lit("RF_Env_SignPanel", new Color(0.92f, 0.92f, 0.95f), 0.3f));
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 1.6f, -0.05f), new Vector3(9f, 3.2f, 0.2f), glass);
                    break;
                }
                default:
                    Part(g, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), Vector3.one, metal);
                    break;
            }
            // Placeholders are built facing -Z for store fronts (built to face the player when turned 180).
            if (name == "Env_StoreFront")
                foreach (Transform c in g.transform) c.localPosition = new Vector3(c.localPosition.x, c.localPosition.y, -c.localPosition.z);
            return g;
        }

        private static GameObject Part(GameObject parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material m)
        {
            GameObject p = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.transform.SetParent(parent.transform, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = scale;
            Renderer r = p.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return p;
        }

        // ---------------------------------------------------------------- materials and helpers

        private static Material AsphaltMaterial()
        {
            string texPath = GenFolder + "/RF_Asphalt.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
            {
                const int size = 256;
                Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, false);
                System.Random r = new System.Random(7);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float n = 0.24f + 0.05f * Mathf.PerlinNoise(x * 0.05f, y * 0.05f) + (float)r.NextDouble() * 0.06f;
                        if (r.NextDouble() < 0.02) n += 0.12f;   // light speckles
                        t.SetPixel(x, y, new Color(n, n, n * 1.02f));
                    }
                File.WriteAllBytes(texPath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }
            Material m = Lit("RF_Env_Asphalt", Color.white, 0.12f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetTextureScale("_BaseMap", new Vector2(120f, 120f));   // about 3.3 m per tile on the 400 m floor
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Paint(Color c)
        {
            return Lit("RF_Env_Paint" + ColorKey(c), c, 0.2f);
        }

        private static Material Lit(string name, Color color, float smoothness)
        {
            Material m = GetMaterial(name, "Universal Render Pipeline/Lit", color);
            SetF(m, "_Smoothness", smoothness);
            return m;
        }

        private static Material GetMaterial(string name, string shaderName, Color color)
        {
            Material m;
            if (mats.TryGetValue(name, out m)) return m;
            string path = GenFolder + "/" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader s = Shader.Find(shaderName);
                if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(s);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }

        private static void SetF(Material m, string prop, float v)
        {
            if (m.HasProperty(prop)) m.SetFloat(prop, v);
        }

        private static string ColorKey(Color c)
        {
            return "_" + Mathf.RoundToInt(c.r * 99f) + "_" + Mathf.RoundToInt(c.g * 99f) + "_" + Mathf.RoundToInt(c.b * 99f);
        }

        private static float Rand(float a, float b)
        {
            return a + (float)rng.NextDouble() * (b - a);
        }

        private static void AddMesh(string name, MeshBuilder mb, params Material[] materials)
        {
            if (mb.IsEmpty) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(root, false);
            Mesh mesh = mb.ToMesh(name, materials.Length);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = materials;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void MarkStatic(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        // ---------------------------------------------------------------- tiny mesh builder

        /// <summary>Collects flat quads and boxes into one mesh (one draw call per material). Submesh 1 = "White" quads.</summary>
        private class MeshBuilder
        {
            private readonly List<Vector3> verts = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<int> tris0 = new List<int>();
            private readonly List<int> tris1 = new List<int>();

            public bool IsEmpty { get { return verts.Count == 0; } }

            // Quad lying flat, facing up.
            public void FlatQuad(Vector3 c, Vector2 size, string group = null)
            {
                float hx = size.x * 0.5f, hz = size.y * 0.5f;
                Quad(c + new Vector3(-hx, 0f, -hz), c + new Vector3(-hx, 0f, hz), c + new Vector3(hx, 0f, hz), c + new Vector3(hx, 0f, -hz), Vector3.up, group);
            }

            // Four corners in order around the quad; 'facing' says which way the visible side should point.
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing, string group = null)
            {
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, facing) < 0f)
                {
                    Vector3 tmp = b; b = d; d = tmp;   // flip winding
                }
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1f, 0f));
                List<int> t = group == "White" ? tris1 : tris0;
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }

            // Box with no bottom, rotated 'yaw' degrees around its center.
            public void Box(Vector3 c, Vector3 size, float yaw = 0f)
            {
                Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                Vector3 h = size * 0.5f;
                Vector3 P(float x, float y, float z) { return c + q * new Vector3(x * h.x, y * h.y, z * h.z); }
                Vector3 up = Vector3.up, fwd = q * Vector3.forward, right = q * Vector3.right;
                Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), up);
                Quad(P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), P(1, -1, 1), fwd);
                Quad(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), -fwd);
                Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), right);
                Quad(P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1), -right);
            }

            public Mesh ToMesh(string name, int materialCount)
            {
                Mesh m = new Mesh();
                m.name = name;
                if (verts.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(verts);
                m.SetUVs(0, uvs);
                bool two = materialCount > 1 && tris1.Count > 0;
                m.subMeshCount = two ? 2 : 1;
                if (two)
                {
                    m.SetTriangles(tris0, 0);
                    m.SetTriangles(tris1, 1);
                }
                else
                {
                    List<int> all = new List<int>(tris0);
                    all.AddRange(tris1);
                    m.SetTriangles(all, 0);
                }
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }
    }
}
