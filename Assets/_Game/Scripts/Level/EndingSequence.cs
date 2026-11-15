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
        "MOCHINI BOUNCELINI",
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
        var mochi = FindAnyObjectByType<MochiController>();
        var respawn = mochi != null ? mochi.GetComponent<Respawn2D>() : null;
        if (respawn != null) respawn.enabled = false;   // de aici nu mai exista checkpoint-uri

        yield return new WaitForSeconds(1.2f);
        PatchNoteUI.Show("PATCH 2.0.0", "Removed: the Director.", "Balance can no longer be guaranteed.");
        yield return new WaitForSeconds(4f);
        PatchNoteUI.Show("PATCH 2.0.1", "Removed: unbalanced content.", "Removed: the world.");
        yield return new WaitForSeconds(1.5f);

        if (crumbleRoot != null)
        {
            // fiecare cub in parte (nu piesele intregi), de la cel mai departat spre mochi, in ~5 secunde
            Vector3 center = mochi != null && mochi.visual != null ? mochi.visual.position : crumbleRoot.position;
            var blocks = new List<Transform>();
            foreach (var r in crumbleRoot.GetComponentsInChildren<Renderer>()) blocks.Add(r.transform);
            blocks.Sort((a, b) => Vector3.Distance(b.position, center).CompareTo(Vector3.Distance(a.position, center)));
            int perStep = Mathf.Max(1, Mathf.CeilToInt(blocks.Count * blockInterval / 5f));
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                foreach (var c in b.GetComponentsInChildren<Collider2D>()) c.enabled = false;
                StartCoroutine(Shrink(b));
                if (i % perStep == perStep - 1) yield return new WaitForSeconds(blockInterval);
            }
        }

        if (FezRoom.Active != null) FezRoom.Active.SetPhysicsEnabled(false);   // si podeaua (coliziunea) dispare
        PatchNoteUI.Show("PATCH 2.0.2", "Removed: mochi.", "Thank you for playing.");
        if (mochi != null)
        {
            mochi.enabled = false;
            var body = mochi.GetComponent<Rigidbody2D>();
            if (body != null) body.gravityScale = 4.6f;   // cade, nu pluteste (daca era in dash sau lipit de perete)
        }
        yield return new WaitForSeconds(2f);
        yield return WhiteOut();
        // melodia de final (Audio/Music/Ending, daca ati pus-o): ecranul alb sta cat canta, sau pana apesi o tasta
        while (GameAudio.EndingPlaying && !AnyKey()) yield return null;
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

    static bool AnyKey()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var pad = UnityEngine.InputSystem.Gamepad.current;
        return (kb != null && kb.anyKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
    }

    IEnumerator WhiteOut()
    {
        GameAudio.PlayEnding();
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
