using UnityEngine;

/// <summary>
/// Iesire din scena: cand mochi-ul ajunge aici, fade si incarca scena urmatoare.
/// Intr-o camera rotativa (room + worldAnchor setate), pozitia iesirii se calculeaza din punctul 3D
/// la unghiul curent - ex. varful unei scari care exista doar privita din lateral.
/// </summary>
public class SceneExit2D : MonoBehaviour
{
    public string targetScene = "OpenWorld";
    public float radius = 1.4f;

    [Header("Optional: iesire intr-o camera rotativa")]
    public FezRoom room;
    public Transform worldAnchor;   // punctul 3D pe care stai (ex. fata de sus a ultimei trepte)

    Transform player;
    bool used, warned;

    void Start()
    {
        var mochi = FindFirstObjectByType<MochiController>();
        if (mochi != null) player = mochi.transform;
    }

    Vector2 Target()
    {
        if (room != null && worldAnchor != null)
            return room.ToPhysics(worldAnchor.position, room.Angle) + Vector2.up * 0.5f;   // centrul mochi-ului cand sta pe el
        return transform.position;
    }

    void Update()
    {
        if (used || player == null || Time.timeScale == 0f) return;
        if (Vector2.Distance(player.position, Target()) > radius) return;

        if (!ScreenFader.CanLoad(targetScene))
        {
            if (!warned) Debug.LogWarning("[NERFED] Scena '" + targetScene + "' nu e construita inca.");
            warned = true;
            return;
        }

        used = true;
        ScreenFader.FadeToScene(targetScene);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector2 t = Application.isPlaying ? Target() : (Vector2)transform.position;
        Gizmos.DrawWireSphere(new Vector3(t.x, t.y, 0f), radius);
    }
}
