using UnityEngine;

/// <summary>
/// Efecte mici, fara asset-uri externe: explozii de particule (cuburi mici), stralucire, pufaituri, umbre-fantoma.
/// Materialul vine din Resources/MochiniFx (shader-ul Mochini/UnlitAlpha), creat de buildere.
/// </summary>
public static class Fx
{
    static Material mat;
    static MaterialPropertyBlock block;
    static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    public static Material Material
    {
        get
        {
            if (mat != null) return mat;
            mat = Resources.Load<Material>("MochiniFx");
            if (mat == null)
            {
                var sh = Shader.Find("Mochini/UnlitAlpha");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(sh);
            }
            return mat;
        }
    }

    public static void SetColor(Renderer r, Color c)
    {
        if (block == null) block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor(ColorId, c);
        r.SetPropertyBlock(block);
    }

    static GameObject Piece(PrimitiveType type, Vector3 pos, float size, Color c)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.Destroy(go.GetComponent<Collider>());
        go.name = "Fx";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = Material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetColor(r, c);
        return go;
    }

    /// <summary>Explozie de cuburi mici care zboara si se sting.</summary>
    public static void Burst(Vector3 world, Color c, int count, float speed, float size = 0.18f, float gravity = 6f)
    {
        for (int i = 0; i < count; i++)
        {
            var go = Piece(PrimitiveType.Cube, world, size * Random.Range(0.6f, 1.3f), c);
            var p = go.AddComponent<FxParticle>();
            p.velocity = Random.onUnitSphere * speed * Random.Range(0.4f, 1f);
            p.gravity = gravity;
            p.life = Random.Range(0.5f, 1.0f);
            p.color = c;
            p.spin = Random.Range(-540f, 540f);
        }
    }

    /// <summary>Pufaitura la aterizare: cateva bucatele care fug pe sol, in stanga si in dreapta.</summary>
    public static void Puff(Vector3 world, Vector3 right, Color c, int count = 8)
    {
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            var go = Piece(PrimitiveType.Sphere, world, Random.Range(0.12f, 0.22f), c);
            var p = go.AddComponent<FxParticle>();
            p.velocity = right * side * Random.Range(1.5f, 3.5f) + Vector3.up * Random.Range(0.3f, 1.2f);
            p.gravity = 2f;
            p.life = Random.Range(0.3f, 0.5f);
            p.color = c;
            p.grow = 1.5f;
        }
    }

    /// <summary>Un nimb care pulseaza si se stinge in jurul unui obiect (ex. mochi cand primeste o abilitate).</summary>
    public static void Glow(Transform target, Color c, float duration)
    {
        if (target == null) return;
        var go = Piece(PrimitiveType.Sphere, target.position, 1.6f, c);
        go.transform.SetParent(target, true);
        var p = go.AddComponent<FxParticle>();
        p.life = duration;
        p.color = new Color(c.r, c.g, c.b, 0.55f);
        p.pulse = true;
        p.unscaled = true;
    }

    /// <summary>Umbra-fantoma a vizualului (urma dash-ului).</summary>
    public static void Afterimage(Transform visual, Color c, float life = 0.25f)
    {
        if (visual == null) return;
        var go = Piece(PrimitiveType.Sphere, visual.position, 1f, c);
        go.transform.rotation = visual.rotation;
        go.transform.localScale = visual.lossyScale;
        var p = go.AddComponent<FxParticle>();
        p.life = life;
        p.color = c;
        p.grow = -0.6f;
    }

    /// <summary>Linie de viteza: un cub foarte subtire, intins pe directia de miscare.</summary>
    public static void SpeedLine(Vector3 world, Vector3 dir, Color c)
    {
        var go = Piece(PrimitiveType.Cube, world, 1f, c);
        go.transform.rotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up);
        go.transform.localScale = new Vector3(Random.Range(0.8f, 1.6f), 0.04f, 0.04f);
        go.transform.right = dir;
        var p = go.AddComponent<FxParticle>();
        p.life = 0.18f;
        p.color = c;
        p.velocity = -dir * 3f;
    }
}

/// <summary>Particula simpla: zboara, cade, se roteste, se stinge si se distruge singura.</summary>
public class FxParticle : MonoBehaviour
{
    public Vector3 velocity;
    public float gravity;
    public float life = 0.6f;
    public Color color = Color.white;
    public float spin;
    public float grow;          // >0 creste, <0 se micsoreaza
    public bool pulse;
    public bool unscaled;

    Renderer rend;
    float age;
    Vector3 baseScale;

    void Start()
    {
        rend = GetComponent<Renderer>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        age += dt;
        float t = age / Mathf.Max(0.01f, life);
        if (t >= 1f) { Destroy(gameObject); return; }

        velocity += Vector3.down * gravity * dt;
        transform.position += velocity * dt;
        if (spin != 0f) transform.Rotate(spin * dt, spin * 0.7f * dt, 0f, Space.Self);

        float scale = 1f + grow * t;
        if (pulse) scale *= 1f + 0.15f * Mathf.Sin(age * 14f);
        transform.localScale = baseScale * Mathf.Max(0.01f, scale);

        Color c = color;
        c.a *= 1f - t * t;
        Fx.SetColor(rend, c);
    }
}
