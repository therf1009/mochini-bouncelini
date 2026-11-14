using UnityEngine;

/// <summary>
/// Camera de open world: deasupra si in spatele mochi-ului, la unghi fix (nu se clatina),
/// il urmareste lin. Unghiul vine din offset: (0, 13, -12) ~ 47 de grade in jos.
/// </summary>
public class OverworldCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 13f, -12f);
    public float smooth = 5f;

    void Start() => Snap();

    public void Snap()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(-offset);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 want = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(-offset);
    }
}
