using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skin-urile lui mochi: culori (lista de mai jos) + imagini. Imaginile se pun in
/// Assets/_Game/Resources/Skins/ (PNG sau JPG, orice marime). Poza se intinde pe toata mingea (efect "stretched"),
/// cu mijlocul pozei in fata lui mochi.
/// Numele fisierului devine numele skin-ului (ex. "Pepene.png" -> "Pepene"). Nu trebuie schimbat cod ca sa adaugi unul.
/// Alegerea se salveaza (PlayerPrefs) si se aplica peste tot: open world, tutorial, niveluri.
/// </summary>
public static class MochiSkins
{
    public struct Skin
    {
        public string id, name;
        public Color color;
        public Texture2D texture;   // null = skin de culoare
    }

    const string Key = "nerfed_skin";
    public const string DefaultId = "color:Classic";

    static readonly (string name, Color color)[] Colors =
    {
        ("Classic", new Color(0.98f, 0.62f, 0.25f)),
        ("Strawberry", new Color(1f, 0.55f, 0.70f)),
        ("Matcha", new Color(0.55f, 0.82f, 0.45f)),
        ("Blueberry", new Color(0.45f, 0.60f, 1f)),
        ("Sesame", new Color(0.28f, 0.25f, 0.27f)),
    };

    static List<Skin> cache;

    public static event System.Action Changed;

    public static List<Skin> All
    {
        get
        {
            if (cache != null) return cache;
            cache = new List<Skin>();
            foreach (var (name, color) in Colors)
                cache.Add(new Skin { id = "color:" + name, name = name, color = color });
            var images = Resources.LoadAll<Texture2D>("Skins");
            System.Array.Sort(images, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var tex in images)
                cache.Add(new Skin { id = "img:" + tex.name, name = tex.name, color = Color.white, texture = tex });
            return cache;
        }
    }

    public static string CurrentId
    {
        get => PlayerPrefs.GetString(Key, DefaultId);
        set { PlayerPrefs.SetString(Key, value); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static Skin Current
    {
        get
        {
            string id = CurrentId;
            foreach (var s in All) if (s.id == id) return s;
            return All[0];
        }
    }

    public static bool CurrentIsImage => Current.texture != null;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

    /// <summary>Pune skin-ul pe un renderer (copie proprie a materialului, ca sa nu schimbe si alte obiecte).
    /// frontU = unde e "fata" pe sfera (sfera Unity: 0.25 = spre -z, adica spre camera in niveluri; 0.75 = spre +z).
    /// Mijlocul pozei ajunge acolo.</summary>
    public static void Apply(Renderer r, Skin skin, float frontU = 0.25f)
    {
        if (r == null) return;
        var m = r.material;   // instanta proprie
        if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, skin.color);
        m.color = skin.color;
        if (m.HasProperty(BaseMap)) m.SetTexture(BaseMap, skin.texture);
        m.mainTexture = skin.texture;
        var offset = new Vector2(skin.texture != null ? 0.5f - frontU : 0f, 0f);
        if (m.HasProperty(BaseMap)) m.SetTextureOffset(BaseMap, offset);
        m.mainTextureOffset = offset;
    }
}
