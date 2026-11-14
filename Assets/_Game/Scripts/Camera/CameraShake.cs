using UnityEngine;


/// Screen shake minimal, apelabil static de oriunde: CameraShake.Shake(0.2f).
/// Se pune pe un parinte al camerei, nu pe camera insasi, ca sa nu se bata cu un eventual sistem de urmarire sau cu rotatia de 90 de grade.
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Reglaje")]
    public float maxOffset = 0.35f;
    public float decay = 2.2f;
    public float frequency = 26f;

    float trauma;
    Vector3 basePosition;
    float seed;

    void Awake()
    {
        Instance = this;
        basePosition = transform.localPosition;
        seed = Random.value * 100f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// Adauga trauma (0..1). Apelurile se cumuleaza.
    public static void Shake(float amount)
    {
        if (Instance == null) return;
        Instance.trauma = Mathf.Clamp01(Instance.trauma + amount);
    }

    void LateUpdate()
    {
        if (trauma <= 0f)
        {
            transform.localPosition = basePosition;
            return;
        }

        // trauma la patrat = shake mai putin zgomotos la intensitati mici
        float s = trauma * trauma;
        float t = Time.unscaledTime * frequency;

        float x = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(seed + 17f, t) - 0.5f) * 2f;

        transform.localPosition = basePosition + new Vector3(x, y, 0f) * (maxOffset * s);

        trauma = Mathf.Max(0f, trauma - decay * Time.unscaledDeltaTime);
    }
}
