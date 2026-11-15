using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Tot sunetul jocului, intr-un singur loc. Se creeaza singur la pornire (nu trebuie pus in nicio scena) si traieste
/// peste incarcarile de scena, deci muzica nu se taie cand treci dintr-o scena in alta cu aceeasi piesa.
///
/// Fisierele (in Assets/_Game/Resources/Audio/):
///   Music/OpenWorld, Tutorial, Level1, Level2, Level3, Boss  -> muzica pe scena (title screen-ul are muzica open world-ului)
///   Music/Ending                                             -> OPTIONAL: melodia voastra pe ecranul alb "thank you for playing"
///   Sfx/jump, launch, dash, ability, parry, rotate, boss_defeat, step_a, step_b
///   Voices/[numele skin-ului]/...                            -> replici spuse la intamplare cat porti skin-ul acela
///                                                               (ex. Voices/Trump/ pentru Skins/Trump.jpg)
/// M = sunet oprit / pornit (se tine minte).
/// </summary>
public class GameAudio : MonoBehaviour
{
    const string MuteKey = "nerfed_mute";
    const float MusicVolume = 0.5f;
    const float FadeTime = 1.2f;

    static GameAudio instance;

    AudioSource musicA, musicB, sfx, voice;
    AudioSource currentMusic;
    string currentTrack = "";
    float duck = 1f;            // muzica se lasa mai incet cat vorbeste o voce
    float voiceTimer = 20f;
    AudioClip lastVoice;
    bool ending;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, AudioClip[]> voices = new Dictionary<string, AudioClip[]>();

    static readonly Dictionary<string, string> SceneMusic = new Dictionary<string, string>
    {
        { "TitleScreen", "OpenWorld" }, { "OpenWorld", "OpenWorld" }, { "Tutorial", "Tutorial" }, { "TestRoom", "Tutorial" },
        { "Level1", "Level1" }, { "Level2", "Level2" }, { "Level3", "Level3" },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameAudio>();
    }

    void Awake()
    {
        musicA = MakeSource(true);
        musicB = MakeSource(true);
        sfx = MakeSource(false);
        voice = MakeSource(false);
        AudioListener.volume = PlayerPrefs.GetInt(MuteKey, 0) == 1 ? 0f : 1f;
        SceneManager.sceneLoaded += OnScene;

    }

    // prima scena (ex. Play direct dintr-o scena in editor): sceneLoaded poate veni inainte sa existam
    void Start() => OnScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);

    void OnDestroy() => SceneManager.sceneLoaded -= OnScene;

    AudioSource MakeSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        return s;
    }

    void OnScene(Scene scene, LoadSceneMode mode)
    {
        ending = false;
        if (SceneMusic.TryGetValue(scene.name, out var track)) PlayMusic(track);
    }

    // =================================================================================== API

    /// <summary>Schimba muzica (cu trecere lina). Aceeasi piesa deja pornita = nu se reia de la capat.</summary>
    public static void PlayMusic(string track)
    {
        if (instance == null) return;
        instance.SwitchTo(track, loop: true);
    }

    /// <summary>Opreste muzica treptat.</summary>
    public static void StopMusic()
    {
        if (instance == null) return;
        instance.currentTrack = "";
        instance.currentMusic = null;
    }

    /// <summary>Ecranul alb de final: pune Music/Ending daca exista (o data), altfel liniste. true = are melodie.</summary>
    public static bool PlayEnding()
    {
        if (instance == null) return false;
        instance.ending = true;
        if (instance.Clip("Music/Ending") == null) { StopMusic(); return false; }
        instance.SwitchTo("Ending", loop: false);
        return true;
    }

    /// <summary>Cat timp mai canta melodia de final.</summary>
    public static bool EndingPlaying => instance != null && instance.ending && instance.currentMusic != null && instance.currentMusic.isPlaying;

    /// <summary>Piesa care canta acum (numele fisierului) si daca vorbeste o voce - pentru teste si debug.</summary>
    public static string NowPlaying => instance != null && instance.currentMusic != null && instance.currentMusic.clip != null ? instance.currentMusic.clip.name : "";
    public static string VoiceClip => instance != null && instance.voice.clip != null ? instance.voice.clip.name : "";

    /// <summary>Un efect scurt din Sfx/ (ex. "jump"). pitch variaza putin, ca sa nu sune robotic.</summary>
    public static void Sfx(string name, float volume = 1f, float pitch = 1f, float variation = 0.05f)
    {
        if (instance == null) return;
        var clip = instance.Clip("Sfx/" + name);
        if (clip == null) return;
        instance.sfx.pitch = pitch * (1f + Random.Range(-variation, variation));
        instance.sfx.PlayOneShot(clip, volume);
    }

    /// <summary>O replica a skin-ului purtat acum (daca are voci), chiar acum.</summary>
    public static void VoiceNow()
    {
        if (instance != null) instance.SayRandomLine();
    }

    // =================================================================================== intern

    AudioClip Clip(string path)
    {
        if (!clips.TryGetValue(path, out var c))
        {
            c = Resources.Load<AudioClip>("Audio/" + path);
            clips[path] = c;
        }
        return c;
    }

    void SwitchTo(string track, bool loop)
    {
        if (track == currentTrack && currentMusic != null && currentMusic.isPlaying) return;
        var clip = Clip("Music/" + track);
        currentTrack = track;
        if (clip == null) { currentMusic = null; return; }
        var next = currentMusic == musicA ? musicB : musicA;
        next.clip = clip;
        next.loop = loop;
        next.volume = 0f;
        next.Play();
        currentMusic = next;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.mKey.wasPressedThisFrame)
        {
            bool mute = AudioListener.volume > 0f;
            AudioListener.volume = mute ? 0f : 1f;
            PlayerPrefs.SetInt(MuteKey, mute ? 1 : 0);
            PlayerPrefs.Save();
        }

        // trecere lina intre piese (in timp real: merge si cu jocul pe pauza)
        float dt = Time.unscaledDeltaTime;
        duck = Mathf.MoveTowards(duck, voice.isPlaying ? 0.45f : 1f, dt * 2f);
        float target = (ending ? 1f : MusicVolume) * duck;
        foreach (var s in new[] { musicA, musicB })
        {
            float want = s == currentMusic ? target : 0f;
            s.volume = Mathf.MoveTowards(s.volume, want, dt / FadeTime);
            if (s != currentMusic && s.volume <= 0f && s.isPlaying) s.Stop();
        }

        // replicile skin-ului: la intamplare, doar cat se joaca (nu pe title screen, nu in dialog/pauza)
        if (Time.timeScale > 0f && !ending && SceneManager.GetActiveScene().name != "TitleScreen" && !voice.isPlaying)
        {
            voiceTimer -= Time.deltaTime;
            if (voiceTimer <= 0f)
            {
                voiceTimer = Random.Range(14f, 35f);
                SayRandomLine();
            }
        }
    }

    void SayRandomLine()
    {
        var skin = MochiSkins.Current;
        if (skin.texture == null) return;   // doar skin-urile cu poza au voci
        if (!voices.TryGetValue(skin.name, out var lines))
        {
            lines = Resources.LoadAll<AudioClip>("Audio/Voices/" + skin.name);
            voices[skin.name] = lines;
        }
        if (lines == null || lines.Length == 0) return;
        AudioClip pick = lines[Random.Range(0, lines.Length)];
        if (lines.Length > 1 && pick == lastVoice) pick = lines[(System.Array.IndexOf(lines, pick) + 1) % lines.Length];
        lastVoice = pick;
        voice.Stop();
        voice.clip = pick;
        voice.volume = 1f;
        voice.Play();
    }
}
