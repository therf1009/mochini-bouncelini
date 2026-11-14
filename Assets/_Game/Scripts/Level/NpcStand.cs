using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Standul de la inceputul nivelului: cand mochi-ul ajunge prima oara langa el, personajul ii vorbeste
/// (discutie -> scop, ca pe tabla). Dupa aceea, "E - talk" il face sa repete.
/// La finalul discutiei, scopul apare ca obiectiv sus-stanga.
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
        var mochi = FindFirstObjectByType<MochiController>();
        if (mochi != null) player = mochi.transform;
        if (talkPrompt != null) talkPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null || DialogueUI.IsOpen || DialogueUI.JustClosed) return;

        Vector3 d = player.position - transform.position;
        bool near = Mathf.Abs(d.x) <= talkRadius && Mathf.Abs(d.y) < 3f;
        if (talkPrompt != null) talkPrompt.gameObject.SetActive(near && talked);
        if (!near) return;

        if (!talked) { Talk(); return; }

        var kb = Keyboard.current;
        var pad = Gamepad.current;
        if ((kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame))
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
