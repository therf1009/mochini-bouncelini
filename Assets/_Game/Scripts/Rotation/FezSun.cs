using UnityEngine;

/// <summary>
/// Soarele se roteste odata cu lumea: fata pe care o privesti e mereu luminata (altfel, din spate,
/// tot turnul ar fi in umbra). Umbrele raman, doar ca vin mereu din stanga-sus, ca intr-un desen.
/// Pus pe lumina directionala din nivelurile cu camera rotativa.
/// </summary>
[RequireComponent(typeof(Light))]
public class FezSun : MonoBehaviour
{
    public float pitch = 42f;
    public float yawOffset = -35f;

    void LateUpdate()
    {
        float yaw = FezRotator.Active != null ? FezRotator.Active.Yaw : 0f;
        transform.rotation = Quaternion.Euler(pitch, yaw + yawOffset, 0f);
    }
}
