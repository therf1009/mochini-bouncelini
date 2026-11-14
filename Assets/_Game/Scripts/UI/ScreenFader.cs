using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fade to black intre scene, apelabil de oriunde: ScreenFader.FadeToScene("OpenWorld").
/// Se creeaza singur la primul apel si supravietuieste schimbarii de scena.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    static ScreenFader instance;
    CanvasGroup group;
    bool busy;

    public static bool Busy => instance != null && instance.busy;

    public static bool CanLoad(string scene) => Application.CanStreamedLevelBeLoaded(scene);

    public static void FadeToScene(string scene, float duration = 0.6f)
    {
        var f = Get();
        if (f.busy) return;
        f.StartCoroutine(f.Run(scene, duration));
    }

    static ScreenFader Get()
    {
        if (instance != null) return instance;

        var go = new GameObject("ScreenFader");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ScreenFader>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var black = new GameObject("Black");
        black.transform.SetParent(go.transform, false);
        var img = black.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        instance.group = black.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
        return instance;
    }

    IEnumerator Run(string scene, float duration)
    {
        busy = true;
        yield return Fade(0f, 1f, duration);

        if (CanLoad(scene))
        {
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
        }
        else
        {
            Debug.LogWarning("[NERFED] Scena '" + scene + "' nu exista inca in Build Settings.");
        }

        yield return Fade(1f, 0f, duration);
        busy = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
