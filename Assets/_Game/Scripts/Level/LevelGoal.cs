using System.Collections;
using UnityEngine;

/// <summary>
/// Steagul de la finalul nivelului: il marchezi terminat (abilitatea ramane pentru totdeauna),
/// apare patch note-ul "BUILD COMPLETE" si te intorci in open world, langa portalul nivelului.
/// </summary>
public class LevelGoal : MonoBehaviour
{
    public int levelIndex = 1;
    public string targetScene = "OpenWorld";
    public float radius = 1.3f;

    Transform player;
    bool done;

    void Start()
    {
        var mochi = FindAnyObjectByType<MochiController>();
        if (mochi != null) player = mochi.transform;
    }

    void Update()
    {
        if (done || player == null || Time.timeScale == 0f) return;
        if (Vector2.Distance(player.position, FezRoom.Screen(transform.position)) > radius) return;   // pe ecran, din orice unghi

        done = true;
        FezRotator.Locked = true;   // nimic nu mai porneste pana la fade
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        GameProgress.CompleteLevel(levelIndex);
        PatchNoteUI.SetGoal("");
        PatchNoteUI.Show("BUILD v1." + (levelIndex - 1) + "  -  COMPLETE",
                         RewardName(GameProgress.RewardFor(levelIndex)) + " is yours to keep.",
                         "Returning to the world map...");
        CameraShake.Shake(0.2f);
        yield return new WaitForSecondsRealtime(2.2f);
        ScreenFader.FadeToScene(targetScene);
    }

    public static string RewardName(Ability a)
    {
        switch (a)
        {
            case Ability.Dash: return "Dash";
            case Ability.DoubleJump: return "Double jump";
            case Ability.WallStick: return "Wall stick";
            default: return "Bounce Back shield";
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
