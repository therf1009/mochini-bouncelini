using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O camera rotativa in stil Fez: cuburi pe o grila 3D, vazute dintr-unul din 4 unghiuri.
///
/// Fizica ramane 2D, in planul ecranului: (u, v) = (orizontala pe ecran, inaltime).
/// Pentru fiecare unghi, toate celulele solide se proiecteaza pe ecran, iar reuniunea lor
/// devine un CompositeCollider2D. Ce se suprapune pe ecran devine o singura suprafata solida.
/// Cele 4 seturi se genereaza o singura data, la incarcare; rotirea doar comuta intre ele.
///
/// Celulele glitch (peretii "deprecated") au setul lor: sunt solide, dar dash-ul trece prin ele (DashPhase).
/// La unghiul 0, u = x din lume, deci camera se leaga direct de un coridor 2D obisnuit.
/// Cuburile vizibile (3D) sunt separate si nu au collidere.
/// </summary>
public class FezRoom : MonoBehaviour
{
    /// <summary>Camera rotativa din scena (una pe scena). Null in nivelurile 2D clasice.</summary>
    public static FezRoom Active { get; private set; }

    // fara reincarcarea domeniului la Play (setarea proiectului), staticele raman de la rularea trecuta: le golim
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active = null; }

    [Header("Grila (celule de 1 x 1 x 1)")]
    public int originX;          // prima coloana pe X; la unghiul 0 este si u-ul din stanga camerei
    public int originZ;          // primul rand pe Z
    public int size = 8;         // camera e size x size in plan (par, ca pivotul sa cada pe grila)
    public float baseY;          // inaltimea de jos a randului j = 0

    [Tooltip("Celule solide (i, j, k): i pe X, j pe inaltime (de la baseY), k pe Z. Coordonate absolute de grila.")]
    public List<Vector3Int> solidCells = new List<Vector3Int>();
    [Tooltip("Celule glitch: solide, dar dash-ul trece prin ele.")]
    public List<Vector3Int> glitchCells = new List<Vector3Int>();

    [Header("Margini invizibile (doar fizica)")]
    public float boundaryHeight = 24f;
    [Tooltip("Pentru niveluri care sunt in intregime camera rotativa (ex. turnul): peretele din stanga ramane si la unghiul 0.")]
    public bool leftBoundaryAlways = false;

    readonly GameObject[] angleSets = new GameObject[4];
    readonly GameObject[] glitchSets = new GameObject[4];
    // pe fiecare unghi: (coloana, rand) de pe ecran -> celulele 3D care cad acolo (la adancimi diferite)
    readonly Dictionary<Vector2Int, List<Vector3Int>>[] screenCells = new Dictionary<Vector2Int, List<Vector3Int>>[4];
    readonly HashSet<Vector2Int>[] glitchScreen = new HashSet<Vector2Int>[4];
    GameObject leftBoundary, rightBoundary;
    int angle;

    public int Angle => angle;
    public Vector3 Pivot => new Vector3(originX + size * 0.5f, 0f, originZ + size * 0.5f);
    public float CenterU => originX + size * 0.5f;
    public float MinU => originX;
    public float MaxU => originX + size;
    /// <summary>Adancimea "in fata camerei": aici stau lucrurile care trebuie sa se vada mereu (patch-uri, efecte).</summary>
    public float FrontDepth => -(size * 0.5f + 1f);

    /// <summary>Axa "dreapta pe ecran" pentru o camera rotita cu yaw grade in jurul verticalei.</summary>
    public static Vector3 Right(float yawDeg)
    {
        float r = yawDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(r), 0f, -Mathf.Sin(r));
    }

    /// <summary>Directia in care priveste camera (in adancime).</summary>
    public static Vector3 Forward(float yawDeg)
    {
        float r = yawDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
    }

    /// <summary>Punct 3D -> coordonate de fizica (u, v) la unghiul dat.</summary>
    public Vector2 ToPhysics(Vector3 world, int atAngle)
    {
        return new Vector2(CenterU + Vector3.Dot(world - Pivot, Right(atAngle * 90f)), world.y);
    }

    /// <summary>(u, v) + adancime -> punct 3D. Merge si cu yaw continuu, in timpul rotirii.</summary>
    public Vector3 ToWorld(float u, float v, float depth, float yawDeg)
    {
        return Pivot + Right(yawDeg) * (u - CenterU) + Vector3.up * v + Forward(yawDeg) * depth;
    }

    // ---- ajutoare statice: merg si fara camera rotativa (nivelurile 2D clasice: ecranul = lumea) ----

    /// <summary>Unde apare pe ecran (in planul fizicii) un punct 3D, la unghiul curent.</summary>
    public static Vector2 Screen(Vector3 world) => Active != null ? Active.ToPhysics(world, Active.angle) : (Vector2)world;

    /// <summary>Punct de pe ecran (u, v) adus in lume, la o adancime data (implicit: in fata camerei).</summary>
    public static Vector3 World(Vector2 screen, float? depth = null)
    {
        if (Active == null) return new Vector3(screen.x, screen.y, depth ?? -1f);
        float yaw = FezRotator.Active != null ? FezRotator.Active.Yaw : Active.angle * 90f;
        return Active.ToWorld(screen.x, screen.y, depth ?? Active.FrontDepth, yaw);
    }

    public bool ContainsU(float u, float margin) => u > MinU + margin && u < MaxU - margin;

    /// <summary>Adancimea unui punct 3D la unghiul dat (negativ = spre camera).</summary>
    public float DepthOf(Vector3 world, int atAngle) => Vector3.Dot(world - Pivot, Forward(atAngle * 90f));

    /// <summary>Centrul celulei (i, j, k) in lume.</summary>
    public Vector3 CellCenter(Vector3Int c) => new Vector3(c.x + 0.5f, baseY + c.y + 0.5f, c.z + 0.5f);

    /// <summary>Ar atinge un cerc (u, v) vreun bloc solid (sau glitch) la unghiul dat? Merge si pentru unghiuri inactive
    /// (folosit inainte de rotire: nu rotesti daca ai intra intr-un bloc).</summary>
    public bool Overlaps(int atAngle, Vector2 p, float radius)
    {
        int a = ((atAngle % 4) + 4) % 4;
        var map = screenCells[a];
        var glitch = glitchScreen[a];
        int c0 = Mathf.FloorToInt(p.x - radius), c1 = Mathf.FloorToInt(p.x + radius);
        int r0 = Mathf.FloorToInt(p.y - radius - baseY), r1 = Mathf.FloorToInt(p.y + radius - baseY);
        for (int c = c0; c <= c1; c++)
            for (int r = r0; r <= r1; r++)
            {
                var key = new Vector2Int(c, r);
                bool blocked = (map != null && map.ContainsKey(key)) || (glitch != null && glitch.Contains(key));
                if (!blocked) continue;
                // cel mai apropiat punct al celulei de centrul cercului
                float nx = Mathf.Clamp(p.x, c, c + 1f), ny = Mathf.Clamp(p.y, baseY + r, baseY + r + 1f);
                if ((new Vector2(nx, ny) - p).sqrMagnitude < radius * radius) return true;
            }
        return false;
    }

    /// <summary>
    /// Pe ce bloc 3D stai? (u = centrul mochi-ului pe ecran, feetY = talpa lui.)
    /// Daca exista un bloc de sprijin la adancimea curenta, ramai pe el; altfel iei blocul cel mai apropiat
    /// de camera (ce vezi e ce calci, ca in Fez). Intoarce false daca sub tine nu e nimic din camera asta.
    /// </summary>
    public bool TryGetSupport(int atAngle, float u, float feetY, float currentDepth, out Vector3Int cell)
    {
        cell = default;
        var map = screenCells[((atAngle % 4) + 4) % 4];
        if (map == null) return false;
        int row = Mathf.FloorToInt(feetY - baseY - 0.05f);
        int center = Mathf.FloorToInt(u);
        bool found = false;
        float best = float.MaxValue;
        // intai coloana de sub centru, apoi marginile (cand stai pe colt)
        int[] cols = { center, Mathf.FloorToInt(u - 0.45f), Mathf.FloorToInt(u + 0.45f) };
        foreach (int c in cols)
        {
            if (!map.TryGetValue(new Vector2Int(c, row), out var list)) continue;
            foreach (var cand in list)
            {
                float d = DepthOf(CellCenter(cand), atAngle);
                // aceeasi adancime castiga clar; altfel, cel mai din fata
                float score = Mathf.Abs(d - currentDepth) < 0.51f ? -1000f + Mathf.Abs(d - currentDepth) : d;
                if (score < best) { best = score; cell = cand; found = true; }
            }
            if (found) return true;
        }
        return false;
    }

    void Awake()
    {
        Active = this;
        BuildColliders();
        SetAngle(0);
    }

    void OnDestroy()
    {
        if (Active == this) Active = null;
        for (int a = 0; a < 4; a++)
            if (glitchSets[a] != null) DashPhase.Unregister(glitchSets[a].GetComponent<Collider2D>());
    }

    /// <summary>Activeaza setul de collidere al unghiului. Peretele din stanga apare doar cand nu esti la 0,
    /// ca la unghiul 0 camera sa ramana legata de coridor (sau mereu, cu leftBoundaryAlways).</summary>
    public void SetAngle(int a)
    {
        angle = ((a % 4) + 4) % 4;
        for (int i = 0; i < 4; i++)
        {
            if (angleSets[i] != null) angleSets[i].SetActive(i == angle);
            if (glitchSets[i] != null) glitchSets[i].SetActive(i == angle);
        }
        if (leftBoundary != null) leftBoundary.SetActive(angle != 0 || leftBoundaryAlways);
    }

    HashSet<Vector3Int> solidSet;

    /// <summary>E celula (i, j, k) un bloc solid al camerei?</summary>
    public bool IsSolidCell(Vector3Int c)
    {
        if (solidSet == null) solidSet = new HashSet<Vector3Int>(solidCells);
        return solidSet.Contains(c);
    }

    /// <summary>Platforma din care face parte o celula: blocurile solide lipite de ea (pe i si k), la aceeasi inaltime,
    /// cu loc liber deasupra. Prima e celula de start.</summary>
    public List<Vector3Int> PlatformCells(Vector3Int start, int max)
    {
        var result = new List<Vector3Int>();
        var seen = new HashSet<Vector3Int> { start };
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(start);
        while (queue.Count > 0 && result.Count < max)
        {
            var c = queue.Dequeue();
            if (!IsSolidCell(c)) continue;
            if (!IsSolidCell(c + Vector3Int.up)) result.Add(c);
            foreach (var d in new[] { Vector3Int.left, Vector3Int.right, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) })
                if (seen.Add(c + d)) queue.Enqueue(c + d);
        }
        return result;
    }

    /// <summary>Opreste / porneste toata fizica camerei (finalul jocului: lumea dispare de sub mochi).</summary>
    public void SetPhysicsEnabled(bool on)
    {
        for (int i = 0; i < 4; i++)
        {
            if (angleSets[i] != null) angleSets[i].SetActive(on && i == angle);
            if (glitchSets[i] != null) glitchSets[i].SetActive(on && i == angle);
        }
        if (leftBoundary != null) leftBoundary.SetActive(on && (angle != 0 || leftBoundaryAlways));
        if (rightBoundary != null) rightBoundary.SetActive(on);
    }

    void BuildColliders()
    {
        // fizica traieste in planul XY al lumii, deci radacina e la origine, fara parinte
        var root = new GameObject("FezPhysics_" + name);

        for (int a = 0; a < 4; a++)
        {
            angleSets[a] = CompositeSet(root.transform, "Angle" + a, ProjectCells(a, solidCells, true));

            var g = ProjectCells(a, glitchCells, false);
            glitchScreen[a] = g;
            if (g.Count > 0)
            {
                glitchSets[a] = CompositeSet(root.transform, "Glitch" + a, g);
                DashPhase.Register(glitchSets[a].GetComponent<Collider2D>());
            }
        }

        leftBoundary = Wall(root.transform, "BoundaryLeft", MinU - 0.5f);
        rightBoundary = Wall(root.transform, "BoundaryRight", MaxU + 0.5f);
    }

    GameObject CompositeSet(Transform parent, string setName, HashSet<Vector2Int> cells)
    {
        var set = new GameObject(setName);
        set.transform.SetParent(parent, false);
        var body = set.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        var composite = set.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        foreach (Vector2Int cell in cells)
        {
            var box = set.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.offset = new Vector2(cell.x + 0.5f, baseY + cell.y + 0.5f);
            box.compositeOperation = Collider2D.CompositeOperation.Merge;
        }
        composite.GenerateGeometry();
        return set;
    }

    /// <summary>Celulele ocupate pe ecran la unghiul dat: (coloana u, rand j).</summary>
    HashSet<Vector2Int> ProjectCells(int a, List<Vector3Int> cells, bool remember)
    {
        var result = new HashSet<Vector2Int>();
        var map = remember ? new Dictionary<Vector2Int, List<Vector3Int>>() : null;
        Vector3 right = Right(a * 90f);
        foreach (Vector3Int c in cells)
        {
            Vector3 center = new Vector3(c.x + 0.5f, 0f, c.z + 0.5f);
            float u = CenterU + Vector3.Dot(center - Pivot, right);
            var key = new Vector2Int(Mathf.FloorToInt(u), c.y);
            result.Add(key);
            if (map == null) continue;
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<Vector3Int>();
            list.Add(c);
        }
        if (remember) screenCells[a] = map;
        return result;
    }

    GameObject Wall(Transform parent, string wallName, float u)
    {
        var go = new GameObject(wallName);
        go.transform.SetParent(parent, false);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, boundaryHeight);
        box.offset = new Vector2(u, baseY + boundaryHeight * 0.5f);
        return go;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireCube(Pivot + Vector3.up * (baseY + 4f), new Vector3(size, 8f, size));
    }
}
