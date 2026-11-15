using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trusa pentru nivelurile "Fez" (o camera rotativa mare): blocuri cub cu cub, teme (vara / toamna / digital),
/// capace peste suprafete (iarba, frunze, neon) cu bucati care atarna peste margini, decor (tevi, iedera, ferestre,
/// copaci, felinare), cer cu patrate in parallax, particule (polen, frunze, biti), checkpoint-uri 3D, cristale,
/// platforme pe sina, standuri, REVERT, steag. Builderele nivelurilor descriu doar piesele si ce e pe ele.
///
/// Grila: celula (i, j, k) = cub de 1 x 1 x 1: i pe X, j = randul (inaltimea, de la baseY = 0), k pe Z.
/// Piesele "Solid" au coliziune (prin proiectie, FezRoom); "Glitch" sunt solide, dar dash-ul trece prin ele.
/// Tot ce e decor (corpul turnului, tevi, copaci) e doar vizual: mochi trece prin fata lui, ca Gomez in Fez.
/// </summary>
public static class FezLevelKit
{
    public enum Kind { Solid, Glitch }
    public enum Cap { None, Grass, Leaves, Neon }
    public enum Particles { None, Pollen, Leaves, Data }
    public enum Tex { Block, Plank, Stone, Brick, Wood, Circuit, Metal }

    public struct Piece
    {
        public string name, skin;
        public int i0, i1, k0, k1, j0, j1;
        public Kind kind;
        public Piece(string name, int i0, int i1, int k0, int k1, int j0, int j1, Kind kind, string skin)
        {
            this.name = name; this.i0 = i0; this.i1 = i1; this.k0 = k0; this.k1 = k1; this.j0 = j0; this.j1 = j1;
            this.kind = kind; this.skin = skin;
        }
    }

    public static Piece S(string name, int i0, int i1, int k0, int k1, int j0, int j1, string skin = "Plank")
        => new Piece(name, i0, i1, k0, k1, j0, j1, Kind.Solid, skin);
    public static Piece G(string name, int i0, int i1, int k0, int k1, int j0, int j1)
        => new Piece(name, i0, i1, k0, k1, j0, j1, Kind.Glitch, "Glitch");

    public class Skin
    {
        public Color color; public Tex tex; public bool emissiveEdges; public bool noCap; public bool hung;
        public Skin(Color c, Tex t, bool noCap = false, bool hung = false, bool emissiveEdges = false)
        { color = c; tex = t; this.noCap = noCap; this.hung = hung; this.emissiveEdges = emissiveEdges; }
    }

    public class Theme
    {
        public string prefix = "L1";
        public Color skyTop, skyMid, skyLow, haze, ambient, sun = Color.white, ink = new Color(0.16f, 0.15f, 0.17f);
        public Color squares = new Color(1f, 1f, 1f, 0.18f);
        public float sunIntensity = 1.2f, hazeStrength = 0.5f;
        public Vector3 sunEuler = new Vector3(42f, -35f, 0f);
        public readonly Dictionary<string, Skin> skins = new Dictionary<string, Skin>();
        public Cap cap = Cap.Grass;
        public Color capA, capB, capHang;
        public Color[] flowers = { };
        public Color bodyColor; public Tex bodyTex = Tex.Brick;
        public Particles particles = Particles.None;
        public Color particleA = Color.white, particleB = Color.white;
        public Color neon = new Color(0.3f, 1f, 1f);
    }

    public class Ctx
    {
        public Theme theme;
        public FezRoom room;
        public FezRotator rotator;
        public GameObject player;
        public Transform visuals, decor, gameplay;
        public HashSet<Vector3Int> solid = new HashSet<Vector3Int>();
        public HashSet<Vector3Int> glitch = new HashSet<Vector3Int>();
        public List<Piece> pieces = new List<Piece>();
        public System.Random rng = new System.Random(1004);
        public Scene scene;
        public float killY;
    }

    // =====================================================================================================
    // inceput / sfarsit
    // =====================================================================================================

    /// <summary>Scena noua, lumina, camera rotativa cu piesele, vizualul lor. playerWorld = centrul lui mochi (3D).</summary>
    public static Ctx Begin(Theme theme, int roomX, int roomZ, int size, IEnumerable<Piece> pieces,
                            Vector3 playerWorld, float killY, int boundaryHeight = 60)
    {
        var c = new Ctx { theme = theme, killY = killY };
        c.scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureFxMaterial();

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = theme.sunIntensity;
        sun.color = theme.sun;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.7f;
        sun.transform.rotation = Quaternion.Euler(theme.sunEuler);
        var follow = sun.gameObject.AddComponent<FezSun>();
        follow.pitch = theme.sunEuler.x;
        follow.yawOffset = theme.sunEuler.y;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = theme.ambient;
        RenderSettings.fog = false;

        var roomGo = new GameObject("Room");
        c.room = roomGo.AddComponent<FezRoom>();
        c.room.originX = roomX;
        c.room.originZ = roomZ;
        c.room.size = size;
        c.room.baseY = 0f;
        c.room.boundaryHeight = boundaryHeight;
        c.room.leftBoundaryAlways = true;

        c.visuals = new GameObject("Blocks").transform; c.visuals.SetParent(roomGo.transform, false);
        c.decor = new GameObject("Decor").transform; c.decor.SetParent(roomGo.transform, false);
        c.gameplay = new GameObject("Gameplay").transform;

        foreach (var p in pieces)
        {
            c.pieces.Add(p);
            foreach (var cell in Cells(p))
            {
                if (p.kind == Kind.Glitch) c.glitch.Add(cell); else c.solid.Add(cell);
            }
        }
        c.room.solidCells = new List<Vector3Int>(c.solid);
        c.room.glitchCells = new List<Vector3Int>(c.glitch);

        BuildBlocks(c);
        c.player = Mochi(c, playerWorld);
        return c;
    }

    /// <summary>Camera, cerul, particulele, ceata de adancime; salveaza scena si o pune in build.</summary>
    /// <param name="minU">optional: camera urmareste si pe orizontala intre minU si maxU (ex. tutorialul, care e lat);
    /// implicit sta centrata pe camera rotativa (turnurile din niveluri).</param>
    public static void Finish(Ctx c, string scenePath, float minV, float maxV, float orthoSize = 6.5f, float minU = float.NaN, float maxU = float.NaN)
    {
        var body = c.player.GetComponent<Rigidbody2D>();

        var rotator = new GameObject("FezRotator").AddComponent<FezRotator>();
        rotator.room = c.room;
        rotator.player = body;
        rotator.hideWhenRotated = new Transform[0];
        c.rotator = rotator;

        var view = c.player.GetComponent<FezMochiView>();
        view.room = c.room;
        view.rotator = rotator;

        var tint = c.room.gameObject.AddComponent<FezDepthTint>();
        tint.room = c.room;
        tint.rotator = rotator;
        tint.roots = new[] { c.visuals, c.decor };
        tint.haze = c.theme.haze;
        tint.strength = c.theme.hazeStrength;
        tint.nearDepth = -c.room.size * 0.3f;
        tint.farDepth = c.room.size * 0.5f;

        var rig = new GameObject("CameraRig");
        var fezCam = rig.AddComponent<FezCamera>();
        fezCam.target = body;
        fezCam.room = c.room;
        fezCam.rotator = rotator;
        fezCam.minU = float.IsNaN(minU) ? c.room.CenterU : minU;
        fezCam.maxU = float.IsNaN(maxU) ? c.room.CenterU : maxU;
        fezCam.minV = minV;
        fezCam.maxV = maxV;
        fezCam.offset = new Vector2(0f, 1.5f);

        var shake = new GameObject("ShakeRig");
        shake.transform.SetParent(rig.transform, false);
        shake.AddComponent<CameraShake>();

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(shake.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0f, -40f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = c.theme.skyMid;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 160f;
        camGo.AddComponent<AudioListener>();

        Sky(c, camGo.transform);
        AmbientParticles(c, rig.transform);
        fezCam.Snap();

        System.IO.Directory.CreateDirectory(NerfedEditorUtil.ScenesDir);
        EditorSceneManager.SaveScene(c.scene, scenePath);
        NerfedEditorUtil.AddSceneToBuild(scenePath);
        AssetDatabase.SaveAssets();
    }

    public static IEnumerable<Vector3Int> Cells(Piece p)
    {
        for (int i = p.i0; i <= p.i1; i++)
            for (int k = p.k0; k <= p.k1; k++)
                for (int j = p.j0; j <= p.j1; j++)
                    yield return new Vector3Int(i, j, k);
    }

    public static Vector3 Center(Vector3Int c) => new Vector3(c.x + 0.5f, c.y + 0.5f, c.z + 0.5f);
    /// <summary>Punctul de pe fata de sus a celulei (i, j, k): unde sta talpa.</summary>
    public static Vector3 Top(int i, int j, int k) => new Vector3(i + 0.5f, j + 1f, k + 0.5f);

    // =====================================================================================================
    // blocuri si capace
    // =====================================================================================================

    static void BuildBlocks(Ctx c)
    {
        var t = c.theme;
        foreach (var p in c.pieces)
        {
            var parent = new GameObject(p.name).transform;
            parent.SetParent(c.visuals, false);

            if (p.kind == Kind.Glitch)
            {
                Material gm = GlitchMat();
                foreach (var cell in Cells(p))
                    NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Glitch", parent, Center(cell), new Vector3(0.86f, 0.98f, 0.86f), gm, keepCollider: false)
                        .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                parent.gameObject.AddComponent<GlitchVisual>();
                continue;
            }

            Skin skin = t.skins.TryGetValue(p.skin, out var sk) ? sk : new Skin(new Color(0.8f, 0.6f, 0.4f), Tex.Block);
            Material m = SkinMat(c, p.skin, skin);
            foreach (var cell in Cells(p))
            {
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Cell", parent, Center(cell), Vector3.one, m, keepCollider: false);
                bool top = !skin.noCap && !c.solid.Contains(cell + Vector3Int.up) && !c.glitch.Contains(cell + Vector3Int.up);
                if (top) CapCell(c, parent, cell);
            }
            if (skin.hung) HangRods(c, parent, p);
        }
    }

    static void CapCell(Ctx c, Transform parent, Vector3Int cell)
    {
        var t = c.theme;
        Vector3 top = Center(cell) + Vector3.up * 0.5f;
        var rng = c.rng;
        switch (t.cap)
        {
            case Cap.None: return;
            case Cap.Neon:
            {
                // dunga de neon pe marginile libere de sus
                Material neon = NerfedEditorUtil.Mat("M_" + t.prefix + "_Neon", t.neon, unlit: true);
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonTop", parent, top + Vector3.up * 0.03f, new Vector3(1.0f, 0.06f, 1.0f),
                    NerfedEditorUtil.Mat("M_" + t.prefix + "_NeonDim", Color.Lerp(t.neon, Color.black, 0.75f), unlit: true), keepCollider: false);
                foreach (var s in Sides())
                {
                    if (c.solid.Contains(cell + s)) continue;
                    Vector3 o = new Vector3(s.x, 0f, s.z) * 0.5f;
                    Vector3 size = s.x != 0 ? new Vector3(0.06f, 0.08f, 1.02f) : new Vector3(1.02f, 0.08f, 0.06f);
                    NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonEdge", parent, top + o + Vector3.up * 0.04f, size, neon, keepCollider: false);
                }
                return;
            }
        }

        // iarba / frunze: capac putin mai lat decat blocul + fire / frunze care atarna peste margini (regula fixa, cu seed)
        Material a = NerfedEditorUtil.Mat("M_" + t.prefix + "_CapA", t.capA);
        Material b = NerfedEditorUtil.Mat("M_" + t.prefix + "_CapB", t.capB);
        Material hang = NerfedEditorUtil.Mat("M_" + t.prefix + "_CapHang", t.capHang);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Cap", parent, top + Vector3.up * 0.07f, new Vector3(1.07f, 0.2f, 1.07f),
            rng.NextDouble() < 0.5 ? a : b, keepCollider: false);
        if (t.cap == Cap.Leaves && rng.NextDouble() < 0.35)
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Leaf", parent, top + new Vector3((float)rng.NextDouble() * 0.6f - 0.3f, 0.19f, (float)rng.NextDouble() * 0.6f - 0.3f),
                new Vector3(0.3f, 0.05f, 0.22f), rng.NextDouble() < 0.5 ? a : hang, keepCollider: false).transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 0f);

        foreach (var s in Sides())
        {
            if (c.solid.Contains(cell + s)) continue;
            int n = rng.Next(0, 3);
            for (int q = 0; q < n; q++)
            {
                float along = (float)rng.NextDouble() * 0.8f - 0.4f;
                float len = 0.22f + (float)rng.NextDouble() * (t.cap == Cap.Leaves ? 0.25f : 0.38f);
                Vector3 outward = new Vector3(s.x, 0f, s.z) * 0.54f;
                Vector3 alongV = s.x != 0 ? new Vector3(0f, 0f, along) : new Vector3(along, 0f, 0f);
                Vector3 size = t.cap == Cap.Leaves
                    ? (s.x != 0 ? new Vector3(0.05f, len * 0.6f, 0.2f) : new Vector3(0.2f, len * 0.6f, 0.05f))
                    : (s.x != 0 ? new Vector3(0.06f, len, 0.13f) : new Vector3(0.13f, len, 0.06f));
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Hang", parent,
                    top + outward + alongV + Vector3.up * (0.13f - size.y * 0.5f), size, rng.NextDouble() < 0.6 ? hang : b, keepCollider: false);
            }
        }

        // flori mici
        if (t.flowers.Length > 0 && rng.NextDouble() < 0.22)
        {
            Color fc = t.flowers[rng.Next(t.flowers.Length)];
            Vector3 at = top + new Vector3((float)rng.NextDouble() * 0.7f - 0.35f, 0.17f, (float)rng.NextDouble() * 0.7f - 0.35f);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Stem", parent, at + Vector3.up * 0.08f, new Vector3(0.04f, 0.18f, 0.04f), hang, keepCollider: false);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Flower", parent, at + Vector3.up * 0.2f, Vector3.one * 0.13f,
                NerfedEditorUtil.Mat("M_Flower_" + ColorUtility.ToHtmlStringRGB(fc), fc), keepCollider: false);
        }
    }

    static Vector3Int[] Sides() => new[] { Vector3Int.left, Vector3Int.right, new Vector3Int(0, 0, -1), new Vector3Int(0, 0, 1) };

    /// <summary>Scandurile "agatate" (ca in video): o tija subtire de la mijlocul lor in sus, cu un bolt la capat.</summary>
    static void HangRods(Ctx c, Transform parent, Piece p)
    {
        Material rod = NerfedEditorUtil.Mat("M_Rod", new Color(0.22f, 0.18f, 0.20f));
        Material pin = NerfedEditorUtil.Mat("M_RodPin", new Color(0.70f, 0.55f, 0.32f));
        float x = (p.i0 + p.i1 + 1) * 0.5f, z = (p.k0 + p.k1 + 1) * 0.5f;
        float y0 = p.j1 + 1.2f, len = 2.6f;
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Rod", parent, new Vector3(x, y0 + len * 0.5f, z), new Vector3(0.07f, len, 0.07f), rod, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Pin", parent, new Vector3(x, y0 + len, z), Vector3.one * 0.22f, pin, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Pin", parent, new Vector3(x, p.j1 + 1.05f, z), new Vector3(0.24f, 0.12f, 0.24f), pin, keepCollider: false);
    }

    // =====================================================================================================
    // decor (doar vizual)
    // =====================================================================================================

    /// <summary>Un bloc de decor (ex. corpul turnului), cu textura repetata.</summary>
    public static GameObject DecorBox(Ctx c, string name, Vector3 min, Vector3 max, Color color, Tex tex)
    {
        Vector3 size = max - min;
        Material m = TexMat("M_" + c.theme.prefix + "_" + name, color, Texture(tex), new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y)));
        return NerfedEditorUtil.Primitive(PrimitiveType.Cube, name, c.decor, (min + max) * 0.5f, size, m, keepCollider: false);
    }

    /// <summary>Teava din segmente drepte (puncte consecutive), cu coturi si coliere.</summary>
    public static void Pipe(Ctx c, Color color, float radius, params Vector3[] pts)
    {
        var root = new GameObject("Pipe").transform; root.SetParent(c.decor, false);
        Material m = NerfedEditorUtil.Mat("M_Pipe_" + ColorUtility.ToHtmlStringRGB(color), color);
        Material ring = NerfedEditorUtil.Mat("M_PipeRing_" + ColorUtility.ToHtmlStringRGB(color), Color.Lerp(color, Color.black, 0.3f));
        for (int n = 0; n + 1 < pts.Length; n++)
        {
            Vector3 a = pts[n], b = pts[n + 1];
            Vector3 d = b - a;
            var seg = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Segment", root, (a + b) * 0.5f,
                new Vector3(Mathf.Abs(d.x) + radius * 2f, Mathf.Abs(d.y) + radius * 2f, Mathf.Abs(d.z) + radius * 2f), m, keepCollider: false);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Joint", root, a, Vector3.one * radius * 2.7f, ring, keepCollider: false);
            if (d.magnitude > 2.5f)
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Collar", root, (a + b) * 0.5f, Vector3.one * radius * 2.5f + new Vector3(Mathf.Abs(d.normalized.x), Mathf.Abs(d.normalized.y), Mathf.Abs(d.normalized.z)) * -radius, ring, keepCollider: false);
        }
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Joint", root, pts[pts.Length - 1], Vector3.one * radius * 2.7f, ring, keepCollider: false);
    }

    /// <summary>Iedera / liane care atarna de la un punct in jos.</summary>
    public static void Vine(Ctx c, Vector3 top, int length, Color color)
    {
        var root = new GameObject("Vine").transform; root.SetParent(c.decor, false);
        Material m = NerfedEditorUtil.Mat("M_Vine_" + ColorUtility.ToHtmlStringRGB(color), color);
        Material leaf = NerfedEditorUtil.Mat("M_VineLeaf_" + ColorUtility.ToHtmlStringRGB(color), Color.Lerp(color, Color.white, 0.15f));
        for (int n = 0; n < length; n++)
        {
            float wob = Mathf.Sin(n * 1.7f) * 0.06f;
            Vector3 p = top + new Vector3(wob, -n * 0.32f - 0.16f, 0f);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Strand", root, p, new Vector3(0.07f, 0.34f, 0.07f), m, keepCollider: false);
            if (n % 2 == 1)
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Leaf", root, p + new Vector3(n % 4 == 1 ? 0.1f : -0.1f, 0f, -0.02f), new Vector3(0.16f, 0.1f, 0.04f), leaf, keepCollider: false);
        }
    }

    /// <summary>Fereastra pe o fata de decor: geam inchis + rama. normal = directia spre afara a fetei.</summary>
    public static void Window(Ctx c, Vector3 center, Vector3 normal, Color frame, Color glass, float w = 0.9f, float h = 1.2f)
    {
        var root = new GameObject("Window").transform; root.SetParent(c.decor, false);
        root.position = center;
        root.rotation = Quaternion.LookRotation(-normal, Vector3.up);
        Material fm = NerfedEditorUtil.Mat("M_Frame_" + ColorUtility.ToHtmlStringRGB(frame), frame);
        Material gm = NerfedEditorUtil.Mat("M_Glass_" + ColorUtility.ToHtmlStringRGB(glass), glass, unlit: true);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Glass", root, new Vector3(0f, 0f, -0.01f), new Vector3(w, h, 0.04f), gm, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "FrameTop", root, new Vector3(0f, h * 0.5f + 0.06f, -0.04f), new Vector3(w + 0.24f, 0.12f, 0.1f), fm, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Sill", root, new Vector3(0f, -h * 0.5f - 0.06f, -0.08f), new Vector3(w + 0.3f, 0.12f, 0.2f), fm, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "FrameL", root, new Vector3(-w * 0.5f - 0.06f, 0f, -0.04f), new Vector3(0.12f, h, 0.1f), fm, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "FrameR", root, new Vector3(w * 0.5f + 0.06f, 0f, -0.04f), new Vector3(0.12f, h, 0.1f), fm, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Mullion", root, new Vector3(0f, 0f, -0.03f), new Vector3(0.06f, h, 0.06f), fm, keepCollider: false);
    }

    /// <summary>Copac din cuburi: trunchi + coroana din cuburi de culori amestecate.</summary>
    public static void Tree(Ctx c, Vector3 basePos, float height, Color trunk, params Color[] leaves)
    {
        var root = new GameObject("Tree").transform; root.SetParent(c.decor, false);
        Material tm = NerfedEditorUtil.Mat("M_Trunk_" + ColorUtility.ToHtmlStringRGB(trunk), trunk);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Trunk", root, basePos + Vector3.up * height * 0.5f, new Vector3(0.35f, height, 0.35f), tm, keepCollider: false);
        var rng = new System.Random((int)(basePos.x * 73 + basePos.z * 31 + basePos.y * 7));
        for (int n = 0; n < 9; n++)
        {
            Vector3 o = new Vector3((float)rng.NextDouble() * 1.6f - 0.8f, (float)rng.NextDouble() * 1.1f, (float)rng.NextDouble() * 1.6f - 0.8f);
            Color lc = leaves[rng.Next(leaves.Length)];
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Leaves", root, basePos + Vector3.up * (height - 0.2f) + o,
                Vector3.one * (0.7f + (float)rng.NextDouble() * 0.5f), NerfedEditorUtil.Mat("M_Leaves_" + ColorUtility.ToHtmlStringRGB(lc), lc), keepCollider: false);
        }
    }

    /// <summary>Felinar pe un stalp scurt (decor).</summary>
    public static void Lantern(Ctx c, Vector3 basePos, Color light)
    {
        var root = new GameObject("Lantern").transform; root.SetParent(c.decor, false);
        Material pole = NerfedEditorUtil.Mat("M_LanternPole", new Color(0.22f, 0.18f, 0.2f));
        Material glow = NerfedEditorUtil.Mat("M_LanternGlow_" + ColorUtility.ToHtmlStringRGB(light), light, unlit: true);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Pole", root, basePos + Vector3.up * 0.6f, new Vector3(0.08f, 1.2f, 0.08f), pole, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Lamp", root, basePos + Vector3.up * 1.3f, new Vector3(0.28f, 0.34f, 0.28f), glow, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Cap", root, basePos + Vector3.up * 1.52f, new Vector3(0.36f, 0.08f, 0.36f), pole, keepCollider: false);
    }

    public static GameObject Cube(Ctx c, string name, Vector3 center, Vector3 size, Color color, bool unlit = false)
        => NerfedEditorUtil.Primitive(PrimitiveType.Cube, name, c.decor, center, size,
               NerfedEditorUtil.Mat("M_" + c.theme.prefix + "_" + name + "_" + ColorUtility.ToHtmlStringRGB(color), color, unlit), keepCollider: false);

    // =====================================================================================================
    // gameplay
    // =====================================================================================================

    public static TextMeshPro Prompt(Ctx c, string text, Vector3 world, float size = 5f, float tilt = 0f)
    {
        var t = NerfedEditorUtil.WorldText(c.gameplay, "Prompt", text, world, size, c.theme.ink, FontStyles.Italic, new Vector2(12f, 3f));
        var holder = t.transform.parent;
        holder.localRotation = Quaternion.Euler(0f, 0f, tilt);
        holder.gameObject.AddComponent<PromptWobble>().faceCamera = true;
        return t;
    }

    /// <summary>Checkpoint 3D: un felinar mic pus pe un bloc (pozitia = unde sta talpa).</summary>
    public static void Checkpoint(Ctx c, Vector3 feet)
    {
        var root = new GameObject("Checkpoint").transform; root.SetParent(c.gameplay, false);
        root.position = feet;
        Material pole = NerfedEditorUtil.Mat("M_CheckpointPole", new Color(0.25f, 0.22f, 0.22f));
        Material lamp = NerfedEditorUtil.Mat("M_CheckpointLamp", new Color(0.45f, 0.42f, 0.40f), unlit: true);
        // felinarul sta putin in spate, ca sa nu acopere drumul
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Pole", root, new Vector3(0.35f, 0.45f, 0.3f), new Vector3(0.07f, 0.9f, 0.07f), pole, keepCollider: false);
        var l = NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Lamp", root, new Vector3(0.35f, 0.98f, 0.3f), Vector3.one * 0.35f, lamp, keepCollider: false);
        var cp = root.gameObject.AddComponent<FezCheckpoint>();
        cp.lamp = l.GetComponent<Renderer>();
    }

    public static void Orb(Ctx c, Vector3 world)
    {
        var root = new GameObject("DashOrb").transform; root.SetParent(c.gameplay, false);
        root.position = world;
        Material m = NerfedEditorUtil.Mat("M_DashOrb", new Color(0.45f, 0.95f, 1f), unlit: true);
        var crystal = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Crystal", root, Vector3.zero, Vector3.one * 0.55f, m, keepCollider: false);
        crystal.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var orb = root.gameObject.AddComponent<DashOrb>();
        orb.crystal = crystal.transform;
    }

    /// <summary>Tokenul REVERT, la o pozitie 3D (centrul lui), cu momentul mare.</summary>
    public static AbilityPickup Revert(Ctx c, Vector3 world, Ability ability, string header, string[] lines,
                                       string goalAfter, string bannerTitle, string bannerKey, Color color)
    {
        Material glow = NerfedEditorUtil.Mat("M_Revert_" + ColorUtility.ToHtmlStringRGB(color), color, unlit: true);
        var root = new GameObject("Revert_" + ability).transform; root.SetParent(c.gameplay, false);
        root.position = world;
        var token = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Token", root, Vector3.zero, Vector3.one * 0.55f, glow, keepCollider: false);
        var label = NerfedEditorUtil.WorldText(root, "Label", "REVERT", new Vector3(0f, 0.95f, 0f), 3f, c.theme.ink, FontStyles.Bold, new Vector2(3f, 0.6f));
        label.transform.parent.gameObject.AddComponent<PromptWobble>().faceCamera = true;

        var pickup = root.gameObject.AddComponent<AbilityPickup>();
        pickup.ability = ability;
        pickup.patchHeader = header;
        pickup.patchLines = lines;
        pickup.goalAfter = goalAfter;
        pickup.visual = token.transform;
        pickup.bannerTitle = bannerTitle;
        pickup.bannerKey = bannerKey;
        pickup.burstColor = color;
        pickup.radius = 1.1f;
        return pickup;
    }

    /// <summary>Steagul de final, la o pozitie 3D (pe fata de sus a unui bloc).</summary>
    public static LevelGoal Flag(Ctx c, Vector3 feet, int levelIndex, Color cloth)
    {
        Material pole = NerfedEditorUtil.Mat("M_FlagPole", new Color(0.25f, 0.24f, 0.27f));
        Material flag = NerfedEditorUtil.Mat("M_Flag_" + ColorUtility.ToHtmlStringRGB(cloth), cloth);
        var root = new GameObject("Goal_Level" + levelIndex).transform; root.SetParent(c.gameplay, false);
        root.position = feet + Vector3.up * 0.5f;
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Pole", root, new Vector3(0f, 1.1f, 0f), new Vector3(0.15f, 3.2f, 0.15f), pole, keepCollider: false);
        var c2 = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Flag", root, new Vector3(0.6f, 2.25f, 0f), new Vector3(1.1f, 0.7f, 0.08f), flag, keepCollider: false);
        c2.AddComponent<PromptWobble>().swayAngle = 4f;
        var goal = root.gameObject.AddComponent<LevelGoal>();
        goal.levelIndex = levelIndex;
        goal.radius = 1.4f;
        return goal;
    }

    /// <summary>Standul cu personaj, la o pozitie 3D (talpa), cu fata spre fata turnului (unghiul 0).</summary>
    public static NpcStand Stand(Ctx c, Vector3 feet, string speaker, string[] lines, string goal, Color npcColor)
    {
        Material npc = NerfedEditorUtil.Mat("M_Npc_" + ColorUtility.ToHtmlStringRGB(npcColor), npcColor);
        var root = new GameObject("StandAnchor").transform; root.SetParent(c.gameplay, false);
        root.position = feet + new Vector3(0f, 0f, 0.8f);   // tejgheaua sta putin in spatele drumului
        var stand = LevelBuilderKit.Stand(root, 0f, 0f, speaker, lines, goal, npc, c.theme.ink);
        stand.transform.localPosition = new Vector3(0f, 0.5f, -1.2f);
        if (stand.talkPrompt != null)
        {
            stand.talkPrompt.text = "S   -   talk";
            stand.talkPrompt.transform.parent.GetComponent<PromptWobble>().faceCamera = true;
        }
        return stand;
    }

    /// <summary>Platforma pe sina intre doua puncte 3D (centrul platformei), cu tija vizibila si bolturi la capete.</summary>
    public static RailPlatform Rail(Ctx c, string name, Vector3 a, Vector3 b, Vector3 size, float speed, float phase, string skin = "Plank")
    {
        var root = new GameObject(name).transform; root.SetParent(c.gameplay, false);
        Skin sk = c.theme.skins.TryGetValue(skin, out var s) ? s : new Skin(new Color(0.8f, 0.6f, 0.4f), Tex.Plank);
        Material pm = SkinMat(c, skin, sk);
        var plank = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Plank", root, (a + b) * 0.5f, size, pm, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Bolt", plank.transform, Vector3.zero, new Vector3(0.3f / size.x, 1.2f, 0.3f / size.z),
            NerfedEditorUtil.Mat("M_RodPin", new Color(0.70f, 0.55f, 0.32f)), keepCollider: false);

        // sina: o tija subtire de la A la B (prelungita putin), cu bolturi la capete
        Material rod = NerfedEditorUtil.Mat("M_Rod", new Color(0.22f, 0.18f, 0.20f));
        Material pin = NerfedEditorUtil.Mat("M_RodPin", new Color(0.70f, 0.55f, 0.32f));
        Vector3 d = b - a;
        Vector3 ext = d.normalized * 0.4f;
        Vector3 railA = a - ext, railB = b + ext;
        var r = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Rail", root, (railA + railB) * 0.5f, new Vector3(0.07f, 0.07f, (railB - railA).magnitude), rod, keepCollider: false);
        r.transform.rotation = Quaternion.LookRotation(d.normalized, Mathf.Abs(d.normalized.y) > 0.9f ? Vector3.forward : Vector3.up);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "PinA", root, railA, Vector3.one * 0.24f, pin, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "PinB", root, railB, Vector3.one * 0.24f, pin, keepCollider: false);

        var rail = root.gameObject.AddComponent<RailPlatform>();
        rail.pointA = a;
        rail.pointB = b;
        rail.size = size;
        rail.speed = speed;
        rail.startPhase = phase;
        rail.visual = plank.transform;
        return rail;
    }

    // =====================================================================================================
    // mochi
    // =====================================================================================================

    static GameObject Mochi(Ctx c, Vector3 world)
    {
        Vector2 screen = c.room.ToPhysics(world, 0);
        var go = LevelBuilderKit.Mochi(screen, c.killY);
        var view = go.AddComponent<FezMochiView>();
        view.visual = go.GetComponent<MochiController>().visual;
        view.depth = c.room.DepthOf(world, 0);
        go.AddComponent<DashFx>();

        Shader silhouette = Shader.Find("Mochini/Silhouette");
        if (silhouette != null)
        {
            const string path = NerfedEditorUtil.MaterialsDir + "/M_MochiSilhouette.mat";
            var sm = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sm == null) { sm = new Material(silhouette) { name = "M_MochiSilhouette" }; AssetDatabase.CreateAsset(sm, path); }
            var s = NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Silhouette", view.visual, Vector3.zero, Vector3.one, sm, keepCollider: false);
            s.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return go;
    }

    // =====================================================================================================
    // cer si particule
    // =====================================================================================================

    static void Sky(Ctx c, Transform cam)
    {
        var t = c.theme;
        string name = "T_" + t.prefix + "_Sky";
        var tex = LoadTex(name);
        if (tex == null)
        {
            tex = new Texture2D(2, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
            for (int y = 0; y < 64; y++)
            {
                float k = y / 63f;
                Color col = k < 0.5f ? Color.Lerp(t.skyLow, t.skyMid, k * 2f) : Color.Lerp(t.skyMid, t.skyTop, (k - 0.5f) * 2f);
                tex.SetPixel(0, y, col); tex.SetPixel(1, y, col);
            }
            tex.Apply();
            SaveTex(tex, name);
        }
        Material m = TexMat("M_" + t.prefix + "_Sky", Color.white, tex, Vector2.one, unlit: true);
        var quad = NerfedEditorUtil.Primitive(PrimitiveType.Quad, "SkyGradient", cam, new Vector3(0f, 0f, 140f), new Vector3(120f, 34f, 1f), m, keepCollider: false);
        quad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // patratele mari, decolorate, in parallax
        var holder = new GameObject("SkySquares").transform;
        holder.SetParent(cam, false);
        holder.localPosition = new Vector3(0f, 0f, 120f);
        Material sq = FxMaterial("M_" + t.prefix + "_SkySquare", t.squares);
        var rng = new System.Random(77);
        for (int n = 0; n < 14; n++)
        {
            float s = 2.5f + (float)rng.NextDouble() * 6f;
            var q = NerfedEditorUtil.Primitive(PrimitiveType.Quad, "Square", holder,
                new Vector3((float)rng.NextDouble() * 40f - 20f, (float)rng.NextDouble() * 24f - 12f, n * 0.1f), new Vector3(s, s * (0.8f + (float)rng.NextDouble() * 0.5f), 1f), sq, keepCollider: false);
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        holder.gameObject.AddComponent<SkyParallax>();
    }

    static void AmbientParticles(Ctx c, Transform rig)
    {
        var t = c.theme;
        if (t.particles == Particles.None) return;
        var go = new GameObject("Ambient_" + t.particles);
        go.transform.SetParent(rig, false);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 220;
        main.startColor = new ParticleSystem.MinMaxGradient(t.particleA, t.particleB);
        var em = ps.emission;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 18f, 12f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.2f;
        var rot = ps.rotationOverLifetime;

        switch (t.particles)
        {
            case Particles.Pollen:
                main.startLifetime = 10f; main.startSpeed = 0.15f; main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                main.gravityModifier = -0.004f; em.rateOverTime = 14f;
                break;
            case Particles.Leaves:
                main.startLifetime = 9f; main.startSpeed = 0.3f; main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
                main.gravityModifier = 0.025f; em.rateOverTime = 10f;
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
                noise.strength = 0.9f;
                break;
            case Particles.Data:
                main.startLifetime = 6f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
                main.gravityModifier = -0.05f; em.rateOverTime = 22f; noise.strength = 0.1f;
                break;
        }
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = ParticleMaterial(t.particles == Particles.Pollen);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // =====================================================================================================
    // materiale si texturi generate (fara fisiere externe)
    // =====================================================================================================

    const string TexDir = "Assets/_Game/Art/Textures";

    public static Texture2D LoadTex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + name + ".asset");

    public static void SaveTex(Texture2D tex, string name)
    {
        System.IO.Directory.CreateDirectory(TexDir);
        AssetDatabase.CreateAsset(tex, TexDir + "/" + name + ".asset");
    }

    static Material SkinMat(Ctx c, string skinName, Skin skin)
    {
        Material m = TexMat("M_" + c.theme.prefix + "_" + skinName, skin.color, Texture(skin.tex), Vector2.one);
        if (skin.emissiveEdges && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", EdgeTexture());
            m.SetColor("_EmissionColor", c.theme.neon * 1.6f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }
        return m;
    }

    public static Material TexMat(string name, Color color, Texture2D tex, Vector2 tiling, bool unlit = false)
    {
        Material m = NerfedEditorUtil.Mat(name, color, unlit);
        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); }
        m.mainTexture = tex;
        m.mainTextureScale = tiling;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Material transparent (shader-ul Mochini/UnlitAlpha) cu culoare fixa.</summary>
    public static Material FxMaterial(string name, Color color)
    {
        string path = NerfedEditorUtil.MaterialsDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var sh = Shader.Find("Mochini/UnlitAlpha");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            m = new Material(sh) { name = name };
            System.IO.Directory.CreateDirectory(NerfedEditorUtil.MaterialsDir);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material GlitchMat() => FxMaterial("M_Glitch", new Color(1f, 0.2f, 0.4f, 0.7f));

    public static Material ParticleMaterial(bool soft)
    {
        Material m = FxMaterial(soft ? "M_ParticleSoft" : "M_ParticleSquare", Color.white);
        if (soft && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", DotTexture());
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Resources/MochiniFx.mat: materialul efectelor create din cod (Fx), inclus mereu in build.</summary>
    public static void EnsureFxMaterial()
    {
        const string dir = "Assets/_Game/Resources";
        const string path = dir + "/MochiniFx.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var sh = Shader.Find("Mochini/UnlitAlpha");
        if (sh == null) { Debug.LogWarning("[Mochini] Shader-ul Mochini/UnlitAlpha lipseste (inca neimportat?)."); return; }
        System.IO.Directory.CreateDirectory(dir);
        AssetDatabase.CreateAsset(new Material(sh) { name = "MochiniFx" }, path);
    }

    public static Texture2D Texture(Tex kind)
    {
        string name = "T_" + kind;
        var tex = LoadTex(name);
        if (tex != null) return tex;
        tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = name };
        var rng = new System.Random((int)kind * 13 + 5);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, y, Grey(PixelValue(kind, x, y, rng)));
        tex.Apply();
        SaveTex(tex, name);
        return tex;
    }

    static Color Grey(float v) => new Color(v, v, v, 1f);

    static float PixelValue(Tex kind, int x, int y, System.Random rng)
    {
        float noise = (float)rng.NextDouble();
        switch (kind)
        {
            case Tex.Plank:
            {
                // scanduri orizontale de cate 4 pixeli, cu rost inchis si cuie
                if (y % 4 == 0) return 0.62f;
                int off = (y / 4) % 2 == 0 ? 0 : 8;
                if ((x + off) % 16 == 0) return 0.66f;
                if (y % 4 == 2 && ((x + off) % 16 == 2 || (x + off) % 16 == 13)) return 0.55f;
                return 0.88f + noise * 0.1f + (y % 4 == 3 ? 0.04f : 0f);
            }
            case Tex.Stone:
            {
                bool seam = (y % 8 == 0) || ((x + (y / 8 % 2) * 5) % 8 == 0);
                return seam ? 0.66f : 0.86f + noise * 0.12f;
            }
            case Tex.Brick:
            {
                int row = y / 4;
                int xs = (x + (row % 2 == 1 ? 4 : 0)) % 8;
                bool mortar = y % 4 == 0 || xs == 0;
                return mortar ? 0.68f : (y % 4 == 3 ? 1f : 0.88f + noise * 0.1f);
            }
            case Tex.Wood:
            {
                bool seam = x % 5 == 0;
                return seam ? 0.66f : 0.84f + 0.08f * Mathf.Sin(y * 0.9f + x) + noise * 0.06f;
            }
            case Tex.Circuit:
            {
                if (x == 0 || y == 0) return 0.5f;
                bool trace = (y == 5 && x > 2 && x < 12) || (x == 11 && y > 5 && y < 13) || (y == 12 && x > 4 && x <= 11) || (x == 4 && y > 8 && y <= 12);
                bool pad = (x == 3 && y == 5) || (x == 4 && y == 9);
                return pad ? 1f : trace ? 0.8f : 0.6f + noise * 0.08f;
            }
            case Tex.Metal:
            {
                if (x == 0 || y == 0 || x == 15 || y == 15) return 0.6f;
                if ((x == 2 || x == 13) && (y == 2 || y == 13)) return 0.5f;
                return 0.85f + noise * 0.05f;
            }
            default:
            {
                // bloc: margine inchisa, lumina sus-stanga, umbra jos-dreapta
                if (x == 0 || y == 0 || x == 15 || y == 15) return 0.62f;
                if (x == 1 || y == 14) return 1.0f;
                if (x == 14 || y == 1) return 0.78f;
                return 0.92f + noise * 0.08f;
            }
        }
    }

    static Texture2D EdgeTexture()
    {
        const string name = "T_NeonEdge";
        var tex = LoadTex(name);
        if (tex != null) return tex;
        tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = name };
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool edge = x == 0 || y == 0 || x == 15 || y == 15;
                bool trace = (y == 5 && x > 2 && x < 12) || (x == 11 && y > 5 && y < 13);
                tex.SetPixel(x, y, edge ? Color.white : trace ? new Color(0.35f, 0.35f, 0.35f) : Color.black);
            }
        tex.Apply();
        SaveTex(tex, name);
        return tex;
    }

    static Texture2D DotTexture()
    {
        const string name = "T_SoftDot";
        var tex = LoadTex(name);
        if (tex != null) return tex;
        tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f)) / 8f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * d)));
            }
        tex.Apply();
        SaveTex(tex, name);
        return tex;
    }
}
