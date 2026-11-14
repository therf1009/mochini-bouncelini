using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Construieste scena de title screen. Meniu: NERFED > Build Title Screen.
/// O pune PRIMA in Build Settings, ca jocul sa porneasca de aici.
/// Fontul e cel implicit din TMP — inlocuiti-l cu fontul "cool" pe obiectul Title.
/// </summary>
public static class TitleScreenBuilder
{
    const string ScenePath = "Assets/_Game/Scenes/TitleScreen.unity";

    [MenuItem("NERFED/Build Title Screen")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // camera simpla, fundal inchis
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.06f, 0.09f);
        camGo.AddComponent<AudioListener>();

        // event system pentru noul Input System
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // titlul mare
        var title = CreateText(canvasGo.transform, "Title", "NERFED", 200, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(1600, 320));
        title.color = new Color(0.98f, 0.62f, 0.18f);   // portocaliu cald, din paleta jocului
        title.characterSpacing = 8f;

        // subtitlul / tema, mic sub titlu
        var sub = CreateText(canvasGo.transform, "Subtitle", "a game that patches you back", 38, FontStyles.Italic);
        Place(sub.rectTransform, new Vector2(0.5f, 0.45f), new Vector2(1200, 70));
        sub.color = new Color(0.45f, 0.85f, 0.9f, 0.85f); // accent cyan

        // "press anything to continue", mic, faded, pulseaza din script
        var press = CreateText(canvasGo.transform, "PressAnything", "press anything to continue", 36, FontStyles.Normal);
        Place(press.rectTransform, new Vector2(0.5f, 0.16f), new Vector2(1000, 70));
        press.color = new Color(1f, 1f, 1f, 0.35f);

        // overlay negru pentru fade, ultimul = deasupra tuturor
        var fadeGo = new GameObject("FadeOverlay");
        fadeGo.transform.SetParent(canvasGo.transform, false);
        var img = fadeGo.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var frt = img.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        var cg = fadeGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // controllerul
        var ctrlGo = new GameObject("TitleScreenController");
        var ts = ctrlGo.AddComponent<TitleScreen>();
        ts.fadeOverlay = cg;
        ts.pressAnything = press;

        System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        // title screen = prima scena din build; restul raman dupa ea
        var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != ScenePath) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[NERFED] Title screen construit: " + ScenePath +
                  "   |   Play -> apasa orice tasta. Tutorial/OpenWorld nu exista inca, deci da doar un warning.");
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }
}
