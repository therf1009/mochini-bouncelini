using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Garderoba din open world: la pornire, pune cate un piedestal pentru fiecare skin (culori + imaginile din
/// Resources/Skins), in arc in jurul obiectului. Pe fiecare piedestal pluteste o mingiuta cu skin-ul.
/// Mergi langa unul si apesi E (sau Enter / Y pe gamepad) -> mochi il poarta. Cel purtat acum straluceste.
/// Skin-uri noi (imagini) apar singure, fara rebuild.
/// </summary>
public class SkinWardrobe : MonoBehaviour
{
    public float arcRadius = 3.5f;
    public float arcDegrees = 280f;
    public float interactRadius = 1.1f;
    public Material pedestalMat;
    public Material previewMat;
    public Color labelColor = new Color(0.16f, 0.15f, 0.17f);

    class Slot { public MochiSkins.Skin skin; public Transform root, ball; public Renderer ballRenderer, ring; public TextMeshPro label; }

    readonly List<Slot> slots = new List<Slot>();
    Transform player;
    Slot near;

    void Start()
    {
        var ow = FindAnyObjectByType<OverworldController>();
        if (ow != null) player = ow.transform;

        var skins = MochiSkins.All;
        int n = skins.Count;
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0.5f : i / (float)(n - 1);
            float ang = (-arcDegrees * 0.5f + arcDegrees * t + 180f) * Mathf.Deg2Rad;   // arcul se deschide spre camera
            var pos = transform.position + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * arcRadius;
            slots.Add(MakeSlot(skins[i], pos, i));
        }
    }

    Slot MakeSlot(MochiSkins.Skin skin, Vector3 pos, int index)
    {
        var s = new Slot { skin = skin };
        s.root = new GameObject("Skin_" + skin.name).transform;
        s.root.SetParent(transform, true);
        s.root.position = pos;

        var stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(stand.GetComponent<Collider>());
        stand.transform.SetParent(s.root, false);
        stand.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        stand.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);
        if (pedestalMat != null) stand.GetComponent<Renderer>().sharedMaterial = pedestalMat;

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(ring.GetComponent<Collider>());
        ring.transform.SetParent(s.root, false);
        ring.transform.localPosition = new Vector3(0f, 0.52f, 0f);
        ring.transform.localScale = new Vector3(0.95f, 0.02f, 0.95f);
        s.ring = ring.GetComponent<Renderer>();
        s.ring.sharedMaterial = Fx.Material;

        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(ball.GetComponent<Collider>());
        ball.transform.SetParent(s.root, false);
        ball.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        ball.transform.localScale = Vector3.one * 0.6f;
        s.ball = ball.transform;
        s.ballRenderer = ball.GetComponent<Renderer>();
        if (previewMat != null) s.ballRenderer.sharedMaterial = previewMat;
        MochiSkins.Apply(s.ballRenderer, skin);

        var holder = new GameObject("Label").transform;
        holder.SetParent(s.root, false);
        holder.localPosition = new Vector3(0f, index % 2 == 0 ? 1.85f : 2.3f, 0f);   // etichetele alterneaza pe inaltime, sa nu se calce
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(holder, false);
        s.label = textGo.AddComponent<TextMeshPro>();
        var tr = textGo.GetComponent<MeshRenderer>();
        if (tr != null) tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        s.label.text = skin.name;
        s.label.fontSize = 2.6f;
        s.label.alignment = TextAlignmentOptions.Center;
        s.label.color = labelColor;
        s.label.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
        s.label.enableAutoSizing = true;
        s.label.fontSizeMin = 1.4f;
        s.label.fontSizeMax = 2.6f;
        return s;
    }

    void Update()
    {
        float time = Time.time;
        string current = MochiSkins.CurrentId;
        var cam = Camera.main != null ? Camera.main.transform : null;
        near = null;
        float best = interactRadius;

        foreach (var s in slots)
        {
            bool worn = s.skin.id == current;
            s.ball.localPosition = new Vector3(0f, 1.15f + Mathf.Sin(time * 2f + s.root.position.x) * 0.08f, 0f);
            s.ball.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
            Fx.SetColor(s.ring, worn ? new Color(1f, 0.85f, 0.35f, 0.9f) : new Color(1f, 1f, 1f, 0.15f));
            if (cam != null) s.label.transform.parent.rotation = cam.rotation;
            if (player == null) continue;
            Vector3 d = player.position - s.root.position;
            float dist = new Vector2(d.x, d.z).magnitude;
            if (Mathf.Abs(d.y) < 1.5f && dist < best) { best = dist; near = s; }
        }

        foreach (var s in slots)
            s.label.text = s == near ? s.skin.name + "\n<size=70%>E  -  wear</size>" : s.skin.name;

        if (near != null && Pressed() && !ScreenFader.Busy)
        {
            MochiSkins.CurrentId = near.skin.id;
            GameAudio.Sfx("ability", 0.35f, 1.6f, 0f);
            GameAudio.VoiceNow();   // skin-ul cu poza "se prezinta" (daca are voci)
            Fx.Burst(near.ball.position, near.skin.texture == null ? near.skin.color : Color.white, 18, 4f, 0.12f, 3f);
            CameraShake.Shake(0.08f);
        }
    }

    static bool Pressed()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        return (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
               || (pad != null && pad.buttonNorth.wasPressedThisFrame);
    }
}
