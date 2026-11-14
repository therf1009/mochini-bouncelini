using UnityEngine;

/// <summary>
/// Camera pentru nivelurile cu rotatie. Urmareste mochi-ul in planul ecranului (u, v)
/// si se roteste odata cu lumea. In camera rotativa, cand nu esti la unghiul 0, se centreaza pe camera.
/// Ierarhie: CameraRig (asta) -> ShakeRig (CameraShake) -> Main Camera (local z negativ).
/// Fara FezRoom / FezRotator se poarta ca o camera laterala obisnuita.
/// </summary>
public class FezCamera : MonoBehaviour
{
    public Rigidbody2D target;
    public FezRoom room;
    public FezRotator rotator;

    public Vector2 offset = new Vector2(2f, 1.5f);
    public float smoothU = 6f;
    public float smoothV = 3f;

    [Header("Limite (in coordonate de ecran u, v)")]
    public float minU = -999f;
    public float maxU = 999f;
    public float minV = -999f;
    public float maxV = 999f;

    float camU, camV;

    void Start() => Snap();

    public void Snap()
    {
        if (target == null) return;
        camU = DesiredU();
        camV = DesiredV();
        Apply();
    }

    void LateUpdate()
    {
        if (target == null) return;
        // timp nescalat: camera se misca si cat timp lumea e oprita in rotire
        float dt = Time.unscaledDeltaTime;
        camU = Mathf.Lerp(camU, DesiredU(), 1f - Mathf.Exp(-smoothU * dt));
        camV = Mathf.Lerp(camV, DesiredV(), 1f - Mathf.Exp(-smoothV * dt));
        Apply();
    }

    float DesiredU()
    {
        bool rotated = rotator != null && (rotator.Angle != 0 || rotator.IsRotating);
        float u = rotated && room != null ? room.CenterU : target.transform.position.x + offset.x;
        return Mathf.Clamp(u, minU, maxU);
    }

    float DesiredV() => Mathf.Clamp(target.transform.position.y + offset.y, minV, maxV);

    void Apply()
    {
        float yaw = rotator != null ? rotator.Yaw : 0f;
        transform.position = room != null ? room.ToWorld(camU, camV, 0f, yaw) : new Vector3(camU, camV, 0f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
