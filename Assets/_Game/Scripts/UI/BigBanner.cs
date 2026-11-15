using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Banner mare pe mijlocul ecranului, pentru momentele importante (o abilitate primita, boss-ul invins):
///     PATCH 1.0.4  -  REVERTED
///        DASH RESTORED
///          [ SHIFT ]
/// Apare cu un "pop", sta putin si se stinge. Merge in timp real (si cand jocul e incetinit).
/// </summary>
public class BigBanner : MonoBehaviour
{
    static BigBanner instance;

    CanvasGroup group;
    TextMeshProUGUI header, title, key;
    RectTransform box, keyBox;
    Coroutine running;

    public static void Show(string headerText, string titleText, string keyText, float hold = 2.5f)
    {
        if (instance == null) instance = Create();
        if (instance.running != null) instance.StopCoroutine(instance.running);
        instance.running = instance.StartCoroutine(instance.Run(headerText, titleText, keyText, hold));
    }

    static BigBanner Create()
    {
        var go = new GameObject("BigBanner");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var b = go.AddComponent<BigBanner>();
        b.group = go.AddComponent<CanvasGroup>();
        b.group.alpha = 0f;
        b.group.interactable = false;
        b.group.blocksRaycasts = false;

        b.box = new GameObject("Box").AddComponent<RectTransform>();
        b.box.SetParent(go.transform, false);
        b.box.anchorMin = b.box.anchorMax = new Vector2(0.5f, 0.56f);
        b.box.sizeDelta = new Vector2(1200f, 330f);
        var bg = b.box.gameObject.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.10f, 0.78f);
        bg.raycastTarget = false;

        b.header = Text(b.box, "Header", new Vector2(0f, 105f), 40f, new Color(1f, 0.78f, 0.35f), FontStyles.Bold);
        b.title = Text(b.box, "Title", new Vector2(0f, 15f), 96f, Color.white, FontStyles.Bold);

        b.keyBox = new GameObject("Key").AddComponent<RectTransform>();
        b.keyBox.SetParent(b.box, false);
        b.keyBox.anchoredPosition = new Vector2(0f, -105f);
        b.keyBox.sizeDelta = new Vector2(260f, 70f);
        var kbg = b.keyBox.gameObject.AddComponent<Image>();
        kbg.color = new Color(1f, 1f, 1f, 0.92f);
        kbg.raycastTarget = false;
        b.key = Text(b.keyBox, "KeyText", Vector2.zero, 44f, new Color(0.10f, 0.09f, 0.12f), FontStyles.Bold);
        DontDestroyOnLoad(go);
        return b;
    }

    static TextMeshProUGUI Text(RectTransform parent, string name, Vector2 pos, float size, Color c, FontStyles style)
    {
        var t = new GameObject(name).AddComponent<TextMeshProUGUI>();
        t.rectTransform.SetParent(parent, false);
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(1180f, size * 1.3f);
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = c;
        t.raycastTarget = false;
        return t;
    }

    IEnumerator Run(string h, string t, string k, float hold)
    {
        header.text = h;
        title.text = t;
        key.text = k;
        keyBox.gameObject.SetActive(!string.IsNullOrEmpty(k));

        for (float x = 0f; x < 0.35f; x += Time.unscaledDeltaTime)
        {
            float p = x / 0.35f;
            group.alpha = p;
            float s = 1f + 0.25f * Mathf.Sin(p * Mathf.PI) * (1f - p) + (1f - p) * 0.4f;   // pop: mare -> normal, cu un mic recul
            box.localScale = Vector3.one * s;
            yield return null;
        }
        group.alpha = 1f;
        box.localScale = Vector3.one;

        for (float x = 0f; x < hold; x += Time.unscaledDeltaTime)
        {
            keyBox.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(x * 8f));   // tasta "respira"
            yield return null;
        }
        for (float x = 0f; x < 0.5f; x += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - x / 0.5f;
            yield return null;
        }
        group.alpha = 0f;
        running = null;
    }
}
