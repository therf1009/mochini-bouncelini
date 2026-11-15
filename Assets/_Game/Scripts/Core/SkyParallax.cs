using UnityEngine;

/// <summary>
/// Cerul din spate (copil al camerei): patrate mari, decolorate, care se misca mai incet decat lumea,
/// ca in video-ul de referinta. Urca putin cand camera urca si aluneca lateral cand se roteste lumea,
/// ca sa se simta adancimea. Copiii se repeta pe verticala (wrap), deci cerul nu se termina.
/// </summary>
public class SkyParallax : MonoBehaviour
{
    [Tooltip("Cat de mult urmeaza inaltimea camerei (0 = deloc, 1 = ca lumea).")]
    public float verticalFactor = 0.12f;
    [Tooltip("Cat aluneca lateral la o rotire de 90 de grade.")]
    public float shiftPerTurn = 7f;
    public float wrapHeight = 24f;
    public float drift = 0.15f;      // pluteste foarte incet si singur

    Transform[] items;
    Vector3[] basePos;
    Transform cam;

    void Start()
    {
        cam = transform.parent;
        items = new Transform[transform.childCount];
        basePos = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = transform.GetChild(i);
            basePos[i] = items[i].localPosition;
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;
        float yaw = FezRotator.Active != null ? FezRotator.Active.Yaw : 0f;
        float camY = cam.position.y;
        for (int i = 0; i < items.Length; i++)
        {
            float speed = 0.6f + 0.4f * ((i * 37) % 10) / 10f;      // fiecare patrat la alta "distanta"
            float x = basePos[i].x - yaw / 90f * shiftPerTurn * speed + Mathf.Sin(Time.time * drift + i) * 0.4f;
            float y = basePos[i].y - camY * verticalFactor * speed;
            y = Mathf.Repeat(y + wrapHeight * 0.5f, wrapHeight) - wrapHeight * 0.5f;
            x = Mathf.Repeat(x + 20f, 40f) - 20f;
            items[i].localPosition = new Vector3(x, y, basePos[i].z);
        }
    }
}
