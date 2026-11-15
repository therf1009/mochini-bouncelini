using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ceata de adancime pentru camera rotativa: ce e mai departe de camera se amesteca usor cu culoarea cerului,
/// ce e in fata ramane plin. Asa se citeste adancimea si dintr-o camera ortografica, din fata (ca in Fez).
/// Se recalculeaza cand se roteste lumea (si lin in timpul rotirii). Culorile se pun prin MaterialPropertyBlock,
/// deci materialele raman neatinse.
/// </summary>
public class FezDepthTint : MonoBehaviour
{
    public FezRoom room;
    public FezRotator rotator;
    [Tooltip("Ce se coloreaza: toti rendererii de sub obiectele astea.")]
    public Transform[] roots;
    public Color haze = new Color(1f, 0.8f, 0.6f);
    [Range(0f, 1f)] public float strength = 0.5f;
    [Tooltip("Adancimea (fata de centrul camerei) de la care incepe ceata si cea la care e maxima.")]
    public float nearDepth = -4f, farDepth = 7f;

    struct Entry { public Renderer r; public Color baseColor; public Vector3 center; }
    readonly List<Entry> entries = new List<Entry>();
    MaterialPropertyBlock block;
    float lastYaw = float.NaN;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Start() => Refresh(rotator != null ? rotator.Yaw : 0f);

    /// <summary>Recalculeaza ceata pentru un unghi (si in editor, pentru capturi).</summary>
    public void Refresh(float yaw)
    {
        if (entries.Count == 0) Collect();
        Apply(yaw);
    }

    void Collect()
    {
        if (block == null) block = new MaterialPropertyBlock();
        if (roots == null) return;
        foreach (var root in roots)
        {
            if (root == null) continue;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null || !m.HasProperty(BaseColorId)) continue;
                entries.Add(new Entry { r = r, baseColor = m.GetColor(BaseColorId), center = r.bounds.center });
            }
        }
    }

    void LateUpdate()
    {
        float yaw = rotator != null ? rotator.Yaw : 0f;
        if (!Mathf.Approximately(yaw, lastYaw)) Apply(yaw);
    }

    void Apply(float yaw)
    {
        lastYaw = yaw;
        if (room == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        Vector3 forward = FezRoom.Forward(yaw);
        foreach (var e in entries)
        {
            if (e.r == null) continue;
            float d = Vector3.Dot(e.center - room.Pivot, forward);
            float t = Mathf.Clamp01(Mathf.InverseLerp(nearDepth, farDepth, d)) * strength;
            e.r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(e.baseColor, haze, t));
            e.r.SetPropertyBlock(block);
        }
    }
}
