using UnityEngine;

public class LanternFlicker : MonoBehaviour
{
    [Header("Lights to Flicker")]
    public Light[] lanternLights;

    [Header("Flicker Settings")]
    public float minInterval = 0.05f;
    public float maxInterval = 0.2f;

    public float minIntensity = 0.6f;
    public float maxIntensity = 1.2f;

    public float minRange = 3f;
    public float maxRange = 5f;

    private float nextFlickerTime = 0f;

    void Update()
    {
        if (Time.time >= nextFlickerTime)
        {
            Flicker();
            nextFlickerTime = Time.time + Random.Range(minInterval, maxInterval);
        }
    }

    void Flicker()
    {
        foreach (Light l in lanternLights)
        {
            l.intensity = Random.Range(minIntensity, maxIntensity);
            l.range = Random.Range(minRange, maxRange);
        }
    }
}
