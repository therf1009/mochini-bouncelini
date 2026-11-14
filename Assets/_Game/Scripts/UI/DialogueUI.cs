using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Caseta de dialog de jos, pe hartie, cu text care apare litera cu litera.
///   DialogueUI.Show("QA_Tester_04", lines, () => { ... la final ... });
/// Cat e deschisa, jocul sta pe loc (timeScale = 0). E / Enter / Y pe gamepad: termina randul sau trece mai departe.
/// (Nu SPACE: ar ramane apasat si mochi-ul ar sari cand se inchide dialogul.)
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public float charsPerSecond = 45f;

    static DialogueUI instance;
    static int lastClosedFrame = -10;

    GameObject box;
    TextMeshProUGUI speakerText, bodyText, hintText;
    string[] lines;
    int index, visibleChars;
    float charTimer, openedAt, previousTimeScale = 1f;
    bool open;
    System.Action onDone;

    public static bool IsOpen => instance != null && instance.open;
    /// <summary>Adevarat in cadrul in care s-a inchis (ca acelasi E sa nu redeschida dialogul).</summary>
    public static bool JustClosed => Time.frameCount - lastClosedFrame < 2;

    public static void Show(string speaker, string[] lines, System.Action onDone = null)
    {
        var ui = Get();
        if (ui.open || lines == null || lines.Length == 0) return;

        ui.speakerText.text = speaker;
        ui.lines = lines;
        ui.index = 0;
        ui.onDone = onDone;
        ui.previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        ui.open = true;
        ui.openedAt = Time.unscaledTime;
        ui.box.SetActive(true);
        ui.BeginLine();
    }

    static DialogueUI Get()
    {
        if (instance != null) return instance;

        var go = new GameObject("DialogueUI");
        instance = go.AddComponent<DialogueUI>();
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        instance.Build(go.transform);
        return instance;
    }

    void Build(Transform root)
    {
        Color ink = new Color(0.16f, 0.15f, 0.17f);

        // chenar de cerneala + hartie, jos pe mijloc
        var border = Rect(root, "Box", ink, new Vector2(1316f, 246f));
        box = border.gameObject;
        var paper = Rect(border, "Paper", new Color(0.97f, 0.95f, 0.89f), new Vector2(1300f, 230f));
        paper.anchorMin = paper.anchorMax = paper.pivot = new Vector2(0.5f, 0.5f);
        paper.anchoredPosition = Vector2.zero;

        speakerText = Label(paper, "Speaker", 30f, FontStyles.Bold, ink, new Vector2(36f, -22f), new Vector2(1000f, 40f), TextAlignmentOptions.TopLeft);
        bodyText = Label(paper, "Body", 32f, FontStyles.Normal, ink, new Vector2(36f, -70f), new Vector2(1228f, 120f), TextAlignmentOptions.TopLeft);
        hintText = Label(paper, "Hint", 24f, FontStyles.Italic, new Color(0.45f, 0.43f, 0.46f), new Vector2(1060f, -186f), new Vector2(210f, 32f), TextAlignmentOptions.TopRight);

        box.SetActive(false);
    }

    void BeginLine()
    {
        bodyText.text = lines[index];
        bodyText.maxVisibleCharacters = 0;
        visibleChars = 0;
        charTimer = 0f;
    }

    void Update()
    {
        if (!open) return;

        int total = lines[index].Length;
        if (visibleChars < total)
        {
            charTimer += Time.unscaledDeltaTime * charsPerSecond;
            visibleChars = Mathf.Min(total, Mathf.FloorToInt(charTimer));
            bodyText.maxVisibleCharacters = visibleChars;
        }
        hintText.text = visibleChars < total ? "" : (index < lines.Length - 1 ? "E   >" : "E   -   ok");

        if (Time.unscaledTime - openedAt < 0.15f) return;   // apasarea care a deschis dialogul nu-l si sare
        if (!AdvancePressed()) return;

        if (visibleChars < total)   // prima apasare termina randul
        {
            visibleChars = total;
            charTimer = total;
            bodyText.maxVisibleCharacters = total;
            return;
        }

        index++;
        if (index < lines.Length) BeginLine();
        else Close();
    }

    void Close()
    {
        open = false;
        box.SetActive(false);
        Time.timeScale = previousTimeScale;
        lastClosedFrame = Time.frameCount;
        var callback = onDone;
        onDone = null;
        callback?.Invoke();
    }

    void OnDestroy()
    {
        if (open) Time.timeScale = previousTimeScale;   // nu lasa jocul oprit daca se schimba scena
    }

    static bool AdvancePressed()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool key = kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        bool button = pad != null && pad.buttonNorth.wasPressedThisFrame;
        return key || button;
    }

    static RectTransform Rect(Transform parent, string name, Color color, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 40f);
        rt.sizeDelta = size;
        return rt;
    }

    static TextMeshProUGUI Label(RectTransform parent, string name, float size, FontStyles style, Color color,
                                 Vector2 offset, Vector2 area, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = area;
        return t;
    }
}
