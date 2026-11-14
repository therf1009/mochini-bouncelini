using UnityEngine;

/// <summary>
/// Iesire din scena: cand mochi-ul ajunge aici, fade si incarca scena urmatoare.
/// </summary>
public class SceneExit2D : MonoBehaviour
{
    public string targetScene = "OpenWorld";
    public float radius = 1.4f;

    Transform player;
    bool used, warned;

    void Start()
    {
        var mochi = FindFirstObjectByType<MochiController>();
        if (mochi != null) player = mochi.transform;
    }

    void Update()
    {
        if (used || player == null) return;
        if (Vector2.Distance(player.position, transform.position) > radius) return;

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
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
