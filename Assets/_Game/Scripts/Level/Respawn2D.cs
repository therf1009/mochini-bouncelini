using UnityEngine;

/// <summary>
/// Fara game over: daca mochi-ul cade sub killY, reapare instant la ultimul checkpoint.
/// Anunta prin Respawned, ca alte sisteme (ex. rotatia) sa revina la starea checkpoint-ului.
/// In camerele rotative, checkpoint-ul tine minte si pozitia 3D si unghiul (FezCheckpoint).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Respawn2D : MonoBehaviour
{
    public float killY = -6f;
    public Vector2 checkpoint;

    [HideInInspector] public bool hasWorldCheckpoint;
    [HideInInspector] public Vector3 checkpointWorld;
    [HideInInspector] public int checkpointAngle;

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

    public void SetCheckpoint(Vector2 point)
    {
        checkpoint = point;
        hasWorldCheckpoint = false;
    }

    /// <summary>Checkpoint intr-o camera rotativa: pozitia 3D (centrul lui mochi) si unghiul la care a fost atins.</summary>
    public void SetFezCheckpoint(Vector2 screenPoint, Vector3 world, int angle)
    {
        checkpoint = screenPoint;
        checkpointWorld = world;
        checkpointAngle = angle;
        hasWorldCheckpoint = true;
    }

    public void RespawnNow()
    {
        rb.linearVelocity = Vector2.zero;
        rb.position = checkpoint;
        transform.position = checkpoint;
        Respawned?.Invoke();
    }
}
