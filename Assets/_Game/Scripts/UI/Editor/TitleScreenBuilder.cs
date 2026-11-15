using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Construieste scena de title screen. Meniu: Mochini Bouncelini > Build Title Screen.
/// O pune PRIMA in Build Settings, ca jocul sa porneasca de aici.
/// Fontul e cel implicit din TMP — inlocuiti-l cu fontul "cool" pe obiectul Title.
/// </summary>
public static class TitleScreenBuilder
{
    const string ScenePath = "Assets/_Game/Scenes/TitleScreen.unity";

    [MenuItem("Mochini Bouncelini/Build Title Screen")]
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

        // numele jocului (fereastra / build-ul)
        PlayerSettings.productName = GameName;

        // logo-ul (fara motto), care pluteste usor
        var logoGo = new GameObject("Logo");
        logoGo.transform.SetParent(canvasGo.transform, false);
        var logo = logoGo.AddComponent<Image>();
        logo.sprite = LogoSprite();
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        Place(logo.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(1500, 520));
        var bob = logoGo.AddComponent<PromptWobble>();
        bob.swayAngle = 1.2f;
        bob.bob = 10f;
        if (logo.sprite == null)
        {
            // fara imagine: macar numele, scris
            Object.DestroyImmediate(logoGo);
            var title = CreateText(canvasGo.transform, "Title", GameName, 150, FontStyles.Bold);
            Place(title.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(1700, 320));
            title.color = new Color(0.98f, 0.62f, 0.18f);
        }

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

        Debug.Log("[Mochini] Title screen construit: " + ScenePath +
                  "   |   Play -> apasa orice tasta. Tutorial/OpenWorld nu exista inca, deci da doar un warning.");
    }

    public const string GameName = "Mochini Bouncelini";
    const string LogoPath = "Assets/_Game/Art/UI/Logo_MochiniBouncelini.png";

    /// <summary>Logo-ul ca Sprite (seteaza singur importul: sprite, transparenta, fara mipmap-uri).</summary>
    static Sprite LogoSprite()
    {
        var importer = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
        if (importer == null) { Debug.LogWarning("[Mochini] Lipseste logo-ul: " + LogoPath); return null; }
        if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
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
