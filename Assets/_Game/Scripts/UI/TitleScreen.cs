using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Title screen: titlul mare, "press anything to continue" care pulseaza faded,
/// iar la orice tasta/click -> fade to black. Prima data -> Tutorial, altfel -> OpenWorld.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Header("Referinte")]
    public CanvasGroup fadeOverlay;          // imagine neagra peste tot, alpha 0 -> 1
    public TextMeshProUGUI pressAnything;    // textul mic care pulseaza

    [Header("Scene")]
    public string tutorialScene = "Tutorial";
    public string openWorldScene = "OpenWorld";

    [Header("Reglaje")]
    public float pulseSpeed = 2.2f;
    [Range(0f, 1f)] public float pulseMin = 0.15f;
    [Range(0f, 1f)] public float pulseMax = 0.55f;
    public float fadeDuration = 1.2f;
    public float inputDelay = 0.4f;          // ignora inputul in primele clipe (sa nu sari din greseala)

    const string PlayedKey = "nerfed_played";
    bool transitioning;

    void Update()
    {
        if (transitioning) return;

        if (pressAnything != null)
        {
            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            Color c = pressAnything.color;
            c.a = Mathf.Lerp(pulseMin, pulseMax, t);
            pressAnything.color = c;
        }

        if (Time.timeSinceLevelLoad >= inputDelay && AnyInput())
            StartCoroutine(Continue());
    }

    static bool AnyInput()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        var pad = Gamepad.current;
        if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) return true;
        if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) return true;
        return false;
    }

    IEnumerator Continue()
    {
        transitioning = true;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            if (fadeOverlay != null) fadeOverlay.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.alpha = 1f;

        bool firstTime = PlayerPrefs.GetInt(PlayedKey, 0) == 0;
        string next = firstTime ? tutorialScene : openWorldScene;

        if (!Application.CanStreamedLevelBeLoaded(next))
        {
            Debug.LogWarning("[Mochini] Scena '" + next + "' nu exista inca in Build Settings. " +
                             "Va merge dupa ce construim Tutorial / OpenWorld.");
            yield return new WaitForSecondsRealtime(0.6f);
            if (fadeOverlay != null) fadeOverlay.alpha = 0f;
            transitioning = false;
            yield break;
        }

        if (firstTime)
        {
            PlayerPrefs.SetInt(PlayedKey, 1);
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene(next);
    }

    /// <summary>Pentru testare: reseteaza "prima data" ca sa vezi iar tutorialul.</summary>
    [ContextMenu("Reset first-time flag")]
    void ResetFirstTime()
    {
        PlayerPrefs.DeleteKey(PlayedKey);
        Debug.Log("[Mochini] Flag-ul de prima rulare a fost resetat.");
    }
}
