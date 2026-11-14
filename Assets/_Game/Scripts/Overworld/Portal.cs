using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Portal spre un nivel: cand mochi-ul e aproape, se aprinde si apare "E - LEVEL N".
/// E / Enter / Y pe gamepad -> fade si porneste nivelul.
/// Culori: cyan = neinceput, portocaliu pulsand = esti langa el, verde = terminat.
/// </summary>
public class Portal : MonoBehaviour
{
    public int levelIndex = 1;
    public string levelScene = "Level1";
    public float interactRadius = 2.4f;

    public Renderer glow;
    public TextMeshPro prompt;
    [Tooltip("Unde sta textul fata de portal: deasupra grinzii si in fata ramei (spre camera), ca sa nu fie acoperit.")]
    public Vector3 promptOffset = new Vector3(0f, 4.2f, -1.6f);

    public Color idleColor = new Color(0.35f, 0.85f, 0.95f);
    public Color nearColor = new Color(1f, 0.72f, 0.30f);
    public Color doneColor = new Color(0.55f, 0.95f, 0.50f);

    Transform player;
    Transform cam;
    MaterialPropertyBlock block;
    bool near;

    void Start()
    {
        var controller = FindFirstObjectByType<OverworldController>();
        if (controller != null) player = controller.transform;
        if (Camera.main != null) cam = Camera.main.transform;
        block = new MaterialPropertyBlock();
        if (prompt != null) prompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        Vector3 d = player.position - transform.position;
        bool nowNear = Mathf.Abs(d.y) < 2f && new Vector2(d.x, d.z).magnitude <= interactRadius;
        if (nowNear != near)
        {
            near = nowNear;
            if (prompt != null) prompt.gameObject.SetActive(near);
        }

        Color c = GameProgress.IsLevelDone(levelIndex) ? doneColor : idleColor;
        if (near) c = Color.Lerp(nearColor, Color.white, 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.time * 6f)));
        if (glow != null)
        {
            glow.GetPropertyBlock(block);
            block.SetColor("_BaseColor", c);
            glow.SetPropertyBlock(block);
        }

        if (prompt != null && near)
        {
            // pozitionat din cod la fiecare cadru, nu din scena: oricum ar fi fost salvat, nu ajunge in spatele portalului
            prompt.transform.position = transform.position + promptOffset;
            if (cam != null) prompt.transform.rotation = cam.rotation;
        }

        if (near && InteractPressed() && !ScreenFader.Busy)
        {
            GameProgress.LastPortal = levelIndex;
            ScreenFader.FadeToScene(levelScene);
        }
    }

    static bool InteractPressed()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool key = kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        bool button = pad != null && pad.buttonNorth.wasPressedThisFrame;
        return key || button;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
