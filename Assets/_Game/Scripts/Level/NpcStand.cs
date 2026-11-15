using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Standul de la inceputul nivelului: cand mochi-ul ajunge prima oara langa el, personajul ii vorbeste
/// (discutie -> scop, ca pe tabla). Dupa aceea, "E - talk" il face sa repete.
/// La finalul discutiei, scopul apare ca obiectiv sus-stanga.
/// In camerele rotative, Q/E rotesc lumea, deci acolo vorbesti cu S / sageata jos / Enter (E ramane doar in 2D).
/// Distanta se masoara pe ecran (ca in Fez), din orice unghi.
/// </summary>
public class NpcStand : MonoBehaviour
{
    public string speaker = "QA_Tester_04";
    [TextArea] public string[] lines;
    public string goalAfter = "";
    public float talkRadius = 2.6f;
    public TextMeshPro talkPrompt;

    Transform player;
    bool talked;

    void Start()
    {
        var mochi = FindAnyObjectByType<MochiController>();
        if (mochi != null) player = mochi.transform;
        if (talkPrompt != null) talkPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null || DialogueUI.IsOpen || DialogueUI.JustClosed) return;
        if (Time.timeScale < 0.99f || FezRotator.Locked) return;   // rotire, incetinirea REVERT-ului, lupta

        Vector3 d = (Vector2)player.position - FezRoom.Screen(transform.position);
        bool near = Mathf.Abs(d.x) <= talkRadius && Mathf.Abs(d.y) < 3f;
        if (talkPrompt != null) talkPrompt.gameObject.SetActive(near && talked);
        if (!near) return;

        if (!talked) { Talk(); return; }

        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool fez = FezRoom.Active != null;
        bool key = kb != null && ((!fez && kb.eKey.wasPressedThisFrame) || kb.sKey.wasPressedThisFrame
                                  || kb.downArrowKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        if (key || (pad != null && pad.buttonNorth.wasPressedThisFrame))
            Talk();
    }

    void Talk()
    {
        talked = true;
        if (talkPrompt != null) talkPrompt.gameObject.SetActive(false);
        DialogueUI.Show(speaker, lines, () =>
        {
            if (!string.IsNullOrEmpty(goalAfter)) PatchNoteUI.SetGoal(goalAfter);
        });
    }
}
