using UnityEngine;

/// <summary>
/// Efectele lui mochi: umbre-fantoma si linii de viteza in dash, pufaitura la aterizare,
/// pleosc cand se lipeste de perete si un val de bucatele la lansarea de pe perete.
/// Doar vizual; asculta evenimentele din MochiController.
/// </summary>
[RequireComponent(typeof(MochiController))]
public class DashFx : MonoBehaviour
{
    public Color trailColor = new Color(1f, 0.70f, 0.35f, 0.55f);
    public Color dustColor = new Color(1f, 0.95f, 0.85f, 0.8f);
    public float afterimageInterval = 0.025f;

    MochiController mochi;
    float timer;
    float lastDir = 1f;

    void Awake()
    {
        mochi = GetComponent<MochiController>();
        mochi.Dashed += d => { lastDir = d; timer = 0f; Fx.Burst(Feet(), dustColor, 6, 2.5f, 0.12f, 2f); };
        mochi.Landed += v => { if (v > 5f) Fx.Puff(Feet(), ScreenRight(), dustColor, v > 12f ? 12 : 6); };
        mochi.WallStuck += s => Fx.Puff(VisualPos() + ScreenRight() * (s * 0.5f), Vector3.up, dustColor, 6);
        mochi.WallLaunched += t => Fx.Burst(VisualPos(), trailColor, 10 + Mathf.RoundToInt(10 * t), 4f, 0.14f, 3f);
    }

    void Update()
    {
        if (!mochi.IsDashing || Time.deltaTime <= 0f) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = afterimageInterval;
        Fx.Afterimage(mochi.visual, trailColor, 0.22f);
        Vector3 dir = ScreenRight() * lastDir;
        Fx.SpeedLine(VisualPos() + Vector3.up * Random.Range(-0.4f, 0.4f) - dir * 0.6f, dir, new Color(1f, 1f, 1f, 0.7f));
    }

    Vector3 VisualPos() => mochi.visual != null ? mochi.visual.position : transform.position;
    Vector3 Feet() => VisualPos() + Vector3.down * 0.45f;

    static Vector3 ScreenRight()
    {
        if (FezRotator.Active != null) return FezRoom.Right(FezRotator.Active.Yaw);
        return Vector3.right;
    }
}
