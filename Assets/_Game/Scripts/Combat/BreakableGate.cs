using System.Collections;
using UnityEngine;

/// <summary>
/// Zid "deprecated": blocheaza drumul pana cand cineva il sparge (ex. turela care il incuie).
/// </summary>
public class BreakableGate : MonoBehaviour
{
    bool broken;

    public void Break()
    {
        if (broken) return;
        broken = true;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        StartCoroutine(Crumble());
    }

    IEnumerator Crumble()
    {
        Vector3 start = transform.localScale;
        Vector3 end = new Vector3(start.x, 0.01f, start.z);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(start, end, t / 0.4f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
