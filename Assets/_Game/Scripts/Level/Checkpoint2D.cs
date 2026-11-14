using UnityEngine;

/// <summary>
/// Checkpoint: cand mochi-ul trece de el pe X, devine noul punct de respawn.
/// Fara trigger-e, intentionat: verificarea de sol a mochi-ului ar lua un trigger drept podea.
/// </summary>
public class Checkpoint2D : MonoBehaviour
{
    public Vector2 spawnOffset = new Vector2(0f, 0.6f);

    Respawn2D player;
    bool reached;

    void Start() => player = FindFirstObjectByType<Respawn2D>();

    void Update()
    {
        if (reached || player == null) return;
        if (player.transform.position.x < transform.position.x) return;

        reached = true;
        player.SetCheckpoint((Vector2)transform.position + spawnOffset);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}
