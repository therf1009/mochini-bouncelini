using UnityEngine;

/// <summary>
/// Pune skin-ul ales (MochiSkins) pe corpul lui mochi. Pus de buildere pe mochi peste tot (open world, tutorial, niveluri).
/// `body` = sfera corpului; `extras` = bucati care urmeaza culoarea (ex. petele din open world, mai deschise),
/// ascunse cand skin-ul e o imagine. Tot cu o imagine se ascund si ochii (orice copil "EyeL"/"EyeR"): poza are deja fata ei.
/// </summary>
public class MochiSkin : MonoBehaviour
{
    public Renderer body;
    public Renderer[] extras;
    [Tooltip("Unde e fata pe sfera: 0.25 = spre camera in niveluri (-z), 0.75 = spre +z (mochi din open world).")]
    public float frontU = 0.25f;

    Transform[] eyes;

    void Start()
    {
        if (body == null)
        {
            var t = transform.Find("Visual") ?? transform.Find("VisualRoot/Body");
            if (t != null) body = t.GetComponent<Renderer>();
        }
        var found = new System.Collections.Generic.List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == "EyeL" || t.name == "EyeR") found.Add(t);
        eyes = found.ToArray();
        Refresh();
        MochiSkins.Changed += Refresh;
    }

    void OnDestroy() => MochiSkins.Changed -= Refresh;

    public void Refresh()
    {
        var skin = MochiSkins.Current;
        MochiSkins.Apply(body, skin, frontU);
        if (eyes != null) foreach (var e in eyes) if (e != null) e.gameObject.SetActive(skin.texture == null);
        if (extras == null) return;
        foreach (var e in extras)
        {
            if (e == null) continue;
            e.gameObject.SetActive(skin.texture == null);
            if (skin.texture == null)
                MochiSkins.Apply(e, new MochiSkins.Skin { color = Color.Lerp(skin.color, Color.white, 0.35f) });
        }
    }
}
