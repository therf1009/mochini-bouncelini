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
/// La unghiul 0, u = x din lume, deci camera se leaga direct de un coridor 2D obisnuit.
/// Cuburile vizibile (3D) sunt separate si nu au collidere.
/// </summary>
public class FezRoom : MonoBehaviour
{
    [Header("Grila (celule de 1 x 1 x 1)")]
    public int originX;          // prima coloana pe X; la unghiul 0 este si u-ul din stanga camerei
    public int originZ;          // primul rand pe Z
    public int size = 8;         // camera e size x size in plan (par, ca pivotul sa cada pe grila)
    public float baseY;          // inaltimea de jos a randului j = 0

    [Tooltip("Celule solide (i, j, k): i pe X, j pe inaltime (de la baseY), k pe Z. Coordonate absolute de grila.")]
    public List<Vector3Int> solidCells = new List<Vector3Int>();

    [Header("Margini invizibile (doar fizica)")]
    public float boundaryHeight = 24f;

    readonly GameObject[] angleSets = new GameObject[4];
    GameObject leftBoundary, rightBoundary;
    int angle;

    public int Angle => angle;
    public Vector3 Pivot => new Vector3(originX + size * 0.5f, 0f, originZ + size * 0.5f);
    public float CenterU => originX + size * 0.5f;
    public float MinU => originX;
    public float MaxU => originX + size;

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

    public bool ContainsU(float u, float margin) => u > MinU + margin && u < MaxU - margin;

    void Awake()
    {
        BuildColliders();
        SetAngle(0);
    }

    /// <summary>Activeaza setul de collidere al unghiului. Peretele din stanga apare doar cand nu esti la 0,
    /// ca la unghiul 0 camera sa ramana legata de coridor.</summary>
    public void SetAngle(int a)
    {
        angle = ((a % 4) + 4) % 4;
        for (int i = 0; i < 4; i++)
            if (angleSets[i] != null) angleSets[i].SetActive(i == angle);
        if (leftBoundary != null) leftBoundary.SetActive(angle != 0);
    }

    void BuildColliders()
    {
        // fizica traieste in planul XY al lumii, deci radacina e la origine, fara parinte
        var root = new GameObject("FezPhysics_" + name);

        for (int a = 0; a < 4; a++)
        {
            var set = new GameObject("Angle" + a);
            set.transform.SetParent(root.transform, false);

            var body = set.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = set.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            foreach (Vector2Int cell in ProjectCells(a))
            {
                var box = set.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
                box.offset = new Vector2(cell.x + 0.5f, baseY + cell.y + 0.5f);
                box.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
            composite.GenerateGeometry();
            angleSets[a] = set;
        }

        leftBoundary = Wall(root.transform, "BoundaryLeft", MinU - 0.5f);
        rightBoundary = Wall(root.transform, "BoundaryRight", MaxU + 0.5f);
    }

    /// <summary>Celulele ocupate pe ecran la unghiul dat: (coloana u, rand j).</summary>
    HashSet<Vector2Int> ProjectCells(int a)
    {
        var result = new HashSet<Vector2Int>();
        Vector3 right = Right(a * 90f);
        foreach (Vector3Int c in solidCells)
        {
            Vector3 center = new Vector3(c.x + 0.5f, 0f, c.z + 0.5f);
            float u = CenterU + Vector3.Dot(center - Pivot, right);
            result.Add(new Vector2Int(Mathf.FloorToInt(u), c.y));
        }
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
