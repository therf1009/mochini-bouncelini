using UnityEngine;

/// <summary>
/// Corpul de fizica al mochi-ului traieste in planul ecranului (u, v). Asta muta doar vizualul
/// in lumea 3D, la unghiul curent, ca sa apara corect cand camera se roteste.
/// Vizualul sta mereu putin in fata camerei rotative (adancime negativa = spre camera),
/// ca sa nu fie acoperit de cuburi - pe ecran, totul e oricum un singur plan.
/// </summary>
public class FezMochiView : MonoBehaviour
{
    public FezRoom room;
    public FezRotator rotator;
    public Transform visual;
    public float depth = -5f;

    void LateUpdate()
    {
        if (visual == null) return;
        Vector3 p = transform.position;   // (u, v), deja interpolat de Rigidbody2D
        float yaw = rotator != null ? rotator.Yaw : 0f;
        visual.position = room != null ? room.ToWorld(p.x, p.y, depth, yaw) : new Vector3(p.x, p.y, depth);
        visual.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
