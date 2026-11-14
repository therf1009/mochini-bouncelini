using UnityEngine;

/// <summary>
/// Fara game over: daca mochi-ul cade sub killY, reapare instant la ultimul checkpoint.
/// Anunta prin Respawned, ca alte sisteme (ex. rotatia) sa revina la starea checkpoint-ului.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Respawn2D : MonoBehaviour
{
    public float killY = -6f;
    public Vector2 checkpoint;

    public event System.Action Respawned;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        checkpoint = rb.position;
    }

    void FixedUpdate()
    {
        if (rb.position.y < killY) RespawnNow();
    }

    public void SetCheckpoint(Vector2 point) => checkpoint = point;

    public void RespawnNow()
    {
        rb.linearVelocity = Vector2.zero;
        rb.position = checkpoint;
        transform.position = checkpoint;
        Respawned?.Invoke();
    }
}
