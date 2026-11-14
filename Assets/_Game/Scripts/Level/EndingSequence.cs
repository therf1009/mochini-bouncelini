using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Finalul tragic. Cand Directorul cade, nu mai e nimeni care sa "balanseze" lumea:
/// vin ultimele patch notes, arena dispare bloc cu bloc (de la cel mai departat spre mochi),
/// mochi cade, ecranul se albeste, "REVERTING TO v1.0.0..." - iar in v1.0.0 mochi nu exista.
/// Apoi creditele si inapoi la title screen.
/// </summary>
public class EndingSequence : MonoBehaviour
{
    public DirectorBoss boss;
    public Transform crumbleRoot;          // copiii lui dispar pe rand (arena, construita din blocuri de 1x1)
    public string titleScene = "TitleScreen";
    public float blockInterval = 0.12f;

    [TextArea]
    public string[] credits =
    {
        "REVERTING TO v1.0.0 ...",
        "",
        "NERFED",
        "made in 24 hours at HAVEN Brasov",
        "theme: BOUNCE BACK",
        "",
        "thank you for playing"
    };

    void Start()
    {
        if (boss != null) boss.Defeated += () => StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        var mochi = FindFirstObjectByType<MochiController>();
        var respawn = mochi != null ? mochi.GetComponent<Respawn2D>() : null;
        if (respawn != null) respawn.enabled = false;   // de aici nu mai exista checkpoint-uri

        yield return new WaitForSeconds(1.2f);
        PatchNoteUI.Show("PATCH 2.0.0", "Removed: the Director.", "Balance can no longer be guaranteed.");
        yield return new WaitForSeconds(4f);
        PatchNoteUI.Show("PATCH 2.0.1", "Removed: unbalanced content.", "Removed: the world.");
        yield return new WaitForSeconds(1.5f);

        if (crumbleRoot != null)
        {
            Vector3 center = mochi != null ? mochi.transform.position : crumbleRoot.position;
            var blocks = new List<Transform>();
            foreach (Transform child in crumbleRoot) blocks.Add(child);
            blocks.Sort((a, b) => Vector2.Distance(b.position, center).CompareTo(Vector2.Distance(a.position, center)));

            foreach (var b in blocks)
            {
                foreach (var c in b.GetComponentsInChildren<Collider2D>()) c.enabled = false;
                StartCoroutine(Shrink(b));
                yield return new WaitForSeconds(blockInterval);
            }
        }

        PatchNoteUI.Show("PATCH 2.0.2", "Removed: mochi.", "Thank you for playing.");
        if (mochi != null) mochi.enabled = false;
        yield return new WaitForSeconds(2f);
        yield return WhiteOut();
        ScreenFader.FadeToScene(titleScene, 1.2f);
    }

    IEnumerator Shrink(Transform t)
    {
        Vector3 start = t.localScale;
        for (float k = 0f; k < 0.3f; k += Time.deltaTime)
        {
            t.localScale = Vector3.Lerp(start, Vector3.zero, k / 0.3f);
            yield return null;
        }
        t.gameObject.SetActive(false);
    }

    IEnumerator WhiteOut()
    {
        var go = new GameObject("Ending");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var white = new GameObject("White");
        white.transform.SetParent(go.transform, false);
        var img = white.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = Vector2.zero;
        img.rectTransform.offsetMax = Vector2.zero;

        for (float t = 0f; t < 2.5f; t += Time.unscaledDeltaTime)
        {
            img.color = new Color(1f, 1f, 1f, t / 2.5f);
            yield return null;
        }
        img.color = Color.white;

        var textGo = new GameObject("Credits");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 44f;
        text.color = new Color(0.16f, 0.15f, 0.17f);
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        string shown = "";
        foreach (string line in credits)
        {
            shown += line + "\n";
            text.text = shown;
            yield return new WaitForSecondsRealtime(line.Length == 0 ? 0.4f : 1.4f);
        }
        yield return new WaitForSecondsRealtime(3f);
    }
}
