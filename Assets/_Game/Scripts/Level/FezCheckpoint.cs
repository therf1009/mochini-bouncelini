using UnityEngine;

/// <summary>
/// Checkpoint intr-o camera rotativa (un felinar pus pe un bloc). Cand mochi il atinge PE ECRAN (ca in Fez,
/// adancimea nu conteaza), devine punctul de respawn: pozitia 3D + unghiul lumii de atunci.
/// La respawn, FezRotator te pune inapoi exact asa: acelasi unghi, pe acelasi bloc.
/// Pozitia obiectului = punctul 3D unde sta talpa lui mochi (deasupra blocului).
/// </summary>
public class FezCheckpoint : MonoBehaviour
{
    public float radius = 1.1f;
    public Renderer lamp;
    public Color offColor = new Color(0.45f, 0.42f, 0.40f);
    public Color onColor = new Color(1f, 0.85f, 0.35f);

    Respawn2D player;
    FezMochiView view;
    MochiController mochi;
    MaterialPropertyBlock block;
    bool reached;
    float pop;

    void Start()
    {
        player = FindAnyObjectByType<Respawn2D>();
        if (player != null) { view = player.GetComponent<FezMochiView>(); mochi = player.GetComponent<MochiController>(); }
        block = new MaterialPropertyBlock();
        SetLamp(offColor);
    }

    void Update()
    {
        if (pop > 0f && lamp != null)
        {
            pop = Mathf.Max(0f, pop - Time.deltaTime * 3f);
            lamp.transform.localScale = Vector3.one * (0.35f + 0.25f * pop);
        }
        if (player == null) return;

        Vector2 me = FezRoom.Screen(transform.position + Vector3.up * 0.5f);
        if (Vector2.Distance(player.transform.position, me) > radius) return;
        int angle = FezRoom.Active != null ? FezRoom.Active.Angle : 0;
        // salvam blocul pe care sta mochi acum (pe sol, deci un loc sigur), nu felinarul: asa nu reapari intr-un bloc.
        // Dar CENTRUL blocului, nu pozitia exacta: altfel, daca treceai pe langa felinar chiar pe muchie (ex. inainte de
        // o saritura), reapareai pe muchie, alunecai si cadeai imediat (uneori in bucla).
        if (view != null && mochi != null)
        {
            if (!mochi.IsGrounded) return;
            if (view.Platform != null) return;   // pe o platforma de pe sina nu salvam: pleaca de acolo
            if (FezRoom.Active != null && !view.HasSupport) return;   // pe colt (centrul trecut de muchie): pastram salvarea de dinainte
            Vector3 w = view.WorldPosition() + Vector3.up * 0.05f;
            Vector2 screen = (Vector2)player.transform.position + Vector2.up * 0.05f;
            var room = FezRoom.Active;
            if (view.HasSupport && room != null)
            {
                var cell = view.SupportCell;
                // inaltimea din bloc (fata de sus + raza), nu cea de acum: pe colt mochi e putin scufundat
                var centered = room.CellCenter(cell) + Vector3.up * 1.05f;
                Vector2 cs = room.ToPhysics(centered, angle);
                if (!room.Overlaps(angle, cs, 0.45f)) { w = centered; screen = cs; }
                else if (player.hasWorldCheckpoint) return;   // centrul nu e liber din unghiul asta: pastram salvarea buna
            }
            player.SetFezCheckpoint(screen, w, angle);
        }
        else player.SetFezCheckpoint(me + Vector2.up * 0.1f, transform.position + Vector3.up * 0.6f, angle);
        if (!reached) { pop = 1f; CameraShake.Shake(0.05f); }
        reached = true;
        SetLamp(onColor);
    }

    void SetLamp(Color c)
    {
        if (lamp == null) return;
        lamp.GetPropertyBlock(block);
        block.SetColor("_BaseColor", c);
        lamp.SetPropertyBlock(block);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);
    }
}
