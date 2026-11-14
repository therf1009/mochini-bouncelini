using UnityEngine;

/// <summary>
/// Efect de schita: textul se leagana lin, ca un desen pe hartie.
/// (Varianta veche "fierbea" - sarea aleator de cateva ori pe secunda - si arata ca un glitch.)
/// faceCamera = se intoarce mereu spre camera; pentru texte din camera rotativa.
/// Pune-l doar pe obiecte fara collider.
/// </summary>
public class PromptWobble : MonoBehaviour
{
    public float swayAngle = 1.5f;
    public float swaySpeed = 1.2f;
    public float bob = 0.05f;
    public bool faceCamera;

    Vector3 basePos;
    Quaternion baseRot;
    float baseTilt, seed;
    Transform cam;

    void Start()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        baseTilt = transform.localEulerAngles.z;
        seed = Random.value * 10f;
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        float t = Time.unscaledTime * swaySpeed + seed;
        float sway = Mathf.Sin(t) * swayAngle;

        if (faceCamera && cam != null)
            transform.rotation = cam.rotation * Quaternion.Euler(0f, 0f, baseTilt + sway);
        else
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, sway);

        transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * 1.7f) * bob);
    }
}
