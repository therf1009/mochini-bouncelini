using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Notificari in stil "patch notes" (intra din dreapta-sus, stau 3 secunde, ies) si linia de obiectiv (stanga-sus).
///   PatchNoteUI.Show("PATCH 1.0.4  -  REVERTED", "Dash restored.", "Rollback due to player backlash.");
///   PatchNoteUI.SetGoal("find the REVERT token");   // "" o ascunde
/// Se creeaza singur, cate unul pe scena. Merge si cu timpul oprit.
/// </summary>
public class PatchNoteUI : MonoBehaviour
{
    const float HiddenX = 700f;
    const float ShownX = -40f;
    const float Top = -40f;

    static PatchNoteUI instance;

    readonly Queue<KeyValuePair<string, string[]>> queue = new Queue<KeyValuePair<string, string[]>>();
    RectTransform panel;
    TextMeshProUGUI headerText, bodyText, goalText;
    GameObject goalPanel;
    bool showing;

    public static void Show(string header, params string[] lines)
    {
        var ui = Get();
        ui.queue.Enqueue(new KeyValuePair<string, string[]>(header, lines));
        if (!ui.showing) ui.StartCoroutine(ui.Run());
    }

    public static void SetGoal(string goal)
    {
        var ui = Get();
        bool has = !string.IsNullOrEmpty(goal);
        ui.goalPanel.SetActive(has);
        ui.goalText.text = has ? "GOAL   >   " + goal : "";
    }

    static PatchNoteUI Get()
    {
        if (instance != null) return instance;

        var go = new GameObject("PatchNoteUI");
        instance = go.AddComponent<PatchNoteUI>();
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        instance.Build(go.transform);
        return instance;
    }

    void Build(Transform root)
    {
        // patch notes: gri inchis, corporatist, ancorat sus-dreapta; sta ascuns in afara ecranului
        panel = Panel(root, "PatchPanel", new Color(0.16f, 0.17f, 0.20f, 0.95f), new Vector2(1f, 1f));
        panel.sizeDelta = new Vector2(640f, 200f);
        panel.anchoredPosition = new Vector2(HiddenX, Top);
        headerText = Label(panel, "Header", 30f, FontStyles.Bold, new Color(0.98f, 0.72f, 0.30f), new Vector2(24f, -18f), new Vector2(592f, 44f));
        bodyText = Label(panel, "Body", 24f, FontStyles.Normal, new Color(0.86f, 0.88f, 0.92f), new Vector2(24f, -66f), new Vector2(592f, 160f));

        // obiectivul: sus-stanga, ramane pe ecran
        var goalRect = Panel(root, "GoalPanel", new Color(0.10f, 0.10f, 0.12f, 0.6f), new Vector2(0f, 1f));
        goalRect.sizeDelta = new Vector2(860f, 60f);
        goalRect.anchoredPosition = new Vector2(40f, Top);
        goalText = Label(goalRect, "Goal", 28f, FontStyles.Bold, Color.white, new Vector2(20f, -12f), new Vector2(820f, 40f));
        goalPanel = goalRect.gameObject;
        goalPanel.SetActive(false);
    }

    IEnumerator Run()
    {
        showing = true;
        while (queue.Count > 0)
        {
            var note = queue.Dequeue();
            string[] lines = note.Value ?? new string[0];
            headerText.text = note.Key;
            bodyText.text = lines.Length > 0 ? "- " + string.Join("\n- ", lines) : "";
            panel.sizeDelta = new Vector2(640f, 86f + 34f * Mathf.Max(1, lines.Length));

            yield return Slide(HiddenX, ShownX, 0.35f);
            yield return new WaitForSecondsRealtime(3.2f);
            yield return Slide(ShownX, HiddenX, 0.3f);
        }
        showing = false;
    }

    IEnumerator Slide(float fromX, float toX, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            panel.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, Mathf.SmoothStep(0f, 1f, t / time)), Top);
            yield return null;
        }
        panel.anchoredPosition = new Vector2(toX, Top);
    }

    static RectTransform Panel(Transform parent, string name, Color color, Vector2 corner)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = corner;
        rt.anchorMax = corner;
        rt.pivot = corner;
        return rt;
    }

    static TextMeshProUGUI Label(RectTransform parent, string name, float size, FontStyles style, Color color, Vector2 offset, Vector2 box)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = box;
        return t;
    }
}
