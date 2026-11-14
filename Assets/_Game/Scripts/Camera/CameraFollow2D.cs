using UnityEngine;

/// <summary>
/// Urmarire laterala pentru nivelurile 2D (tutorial, niveluri).
/// Ierarhie corecta: CameraFollow (asta) -> ShakeRig (CameraShake) -> Main Camera.
/// Asa shake-ul lucreaza pe offset local si nu se bate cu urmarirea.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(2f, 1.5f);
    public float smoothX = 6f;
    public float smoothY = 3f;

    [Header("Limite (in unitati de lume)")]
    public float minX = -999f;
    public float maxX = 999f;
    public float minY = -999f;
    public float maxY = 999f;

    void Start() => Snap();

    public void Snap()
    {
        if (target == null) return;
        transform.position = Desired();
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 want = Desired();
        Vector3 p = transform.position;
        p.x = Mathf.Lerp(p.x, want.x, 1f - Mathf.Exp(-smoothX * Time.deltaTime));
        p.y = Mathf.Lerp(p.y, want.y, 1f - Mathf.Exp(-smoothY * Time.deltaTime));
        transform.position = p;
    }

    Vector3 Desired()
    {
        float x = Mathf.Clamp(target.position.x + offset.x, minX, maxX);
        float y = Mathf.Clamp(target.position.y + offset.y, minY, maxY);
        return new Vector3(x, y, transform.position.z);
    }
}
