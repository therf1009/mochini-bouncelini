using UnityEngine;

/// <summary>
/// Cand te intorci dintr-un nivel, apari in fata portalului prin care ai intrat (GameProgress.LastPortal),
/// nu la casa de start.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OverworldSpawn : MonoBehaviour
{
    public Vector3 offsetFromPortal = new Vector3(0f, 0.1f, -2.6f);   // putin in fata portalului, spre camera

    void Start()
    {
        int last = GameProgress.LastPortal;
        if (last <= 0) return;

        var spot = GameObject.Find("PortalSpot_" + last);
        if (spot == null) return;

        var cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = spot.transform.position + offsetFromPortal;
        cc.enabled = true;

        var cam = FindFirstObjectByType<OverworldCamera>();
        if (cam != null) cam.Snap();
    }
}
