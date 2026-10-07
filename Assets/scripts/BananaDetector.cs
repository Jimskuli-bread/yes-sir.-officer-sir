using UnityEngine;
using UnityEngine.SceneManagement;

public class BananaDetector : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 4f;
    [SerializeField] private float signalRadius = 12f;
    [SerializeField] private float buriedDepth = 2.5f;
    [SerializeField] private float riseSpeed = 1.5f;

    private bool firstBananaFound;
    private BananaPickup buriedBanana;
    private Vector3 surfacePosition;
    private Scene targetScene;
    private Transform handleTransform;
    private AudioSource signalAudio;
    private AudioClip signalClip;

    private void Awake()
    {
        targetScene = gameObject.scene;
        CapsuleCollider handleCollider = GetComponentInChildren<CapsuleCollider>(true);
        handleTransform = handleCollider != null ? handleCollider.transform : transform;

        int pickableLayer = LayerMask.NameToLayer("Pickable");
        if (pickableLayer >= 0)
        {
            SetLayerRecursively(transform, 0);
            if (handleCollider != null)
            {
                handleCollider.gameObject.layer = pickableLayer;
            }
        }

        signalAudio = GetComponent<AudioSource>();
        if (signalAudio == null)
            signalAudio = gameObject.AddComponent<AudioSource>();

        signalClip = CreateSignalClip();
        signalAudio.clip = signalClip;
        signalAudio.playOnAwake = false;
        signalAudio.loop = true;
        signalAudio.spatialBlend = 0f;
        signalAudio.volume = 0f;
    }

    private void Start()
    {
        FindBuriedBanana();
    }

    private void Update()
    {
        if (firstBananaFound || !BananaPickup.HasBanana || !NPC.IsTaskActiveOrNoNpcLoaded(8))
        {
            StopSignal();
            return;
        }

        PlayerPickup playerPickup = GetComponentInParent<PlayerPickup>();
        if (buriedBanana == null)
            FindBuriedBanana();

        if (playerPickup == null || playerPickup.HeldObject != gameObject || buriedBanana == null ||
            !buriedBanana.gameObject.scene.IsValid() || buriedBanana.gameObject.scene != targetScene)
        {
            StopSignal();
            return;
        }

        Vector3 detectorPosition = handleTransform.position;
        Vector2 horizontalOffset = new Vector2(
            detectorPosition.x - surfacePosition.x,
            detectorPosition.z - surfacePosition.z);
        float distance = horizontalOffset.magnitude;
        UpdateSignal(distance);

        if (distance > detectionRadius)
            return;

        buriedBanana.transform.position = Vector3.MoveTowards(
            buriedBanana.transform.position,
            surfacePosition,
            riseSpeed * Time.deltaTime);

        if (Vector3.Distance(buriedBanana.transform.position, surfacePosition) <= 0.01f)
        {
            firstBananaFound = true;
            StopSignal();
            NPC.CompleteTaskInLoadedScenes(8);
            Debug.Log("Banana detector uncovered the buried banana.");
        }
    }

    private void FindBuriedBanana()
    {
        if (!targetScene.IsValid())
            return;

        BananaPickup[] bananas = FindObjectsByType<BananaPickup>(FindObjectsSortMode.None);
        foreach (BananaPickup banana in bananas)
        {
            if (banana == null || banana.gameObject.scene != targetScene ||
                banana.GetComponent<BananaDetector>() != null)
            {
                continue;
            }

            buriedBanana = banana;
            surfacePosition = banana.transform.position;
            banana.transform.position = surfacePosition - Vector3.up * buriedDepth;
            Rigidbody body = banana.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
            }

            return;
        }
    }

    private void UpdateSignal(float distance)
    {
        if (distance > signalRadius)
        {
            StopSignal();
            return;
        }

        float closeness = 1f - Mathf.Clamp01(distance / signalRadius);
        signalAudio.volume = Mathf.Lerp(0.12f, 0.8f, closeness);
        signalAudio.pitch = Mathf.Lerp(0.65f, 2f, closeness);
        if (!signalAudio.isPlaying)
            signalAudio.Play();
    }

    private void StopSignal()
    {
        if (signalAudio != null && signalAudio.isPlaying)
            signalAudio.Stop();
    }

    private static AudioClip CreateSignalClip()
    {
        const int sampleRate = 44100;
        const float pulseDuration = 1f;
        const float beepDuration = 0.09f;
        const float fadeDuration = 0.01f;
        int sampleCount = Mathf.RoundToInt(sampleRate * pulseDuration);
        int beepSampleCount = Mathf.RoundToInt(sampleRate * beepDuration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < beepSampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float fade = Mathf.Clamp01(Mathf.Min(time / fadeDuration, (beepDuration - time) / fadeDuration));
            samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * time) * fade * 0.35f;
        }

        AudioClip clip = AudioClip.Create("BananaDetectorSignal", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (signalClip != null)
            Destroy(signalClip);
    }

    private static void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;
        foreach (Transform child in target)
        {
            SetLayerRecursively(child, layer);
        }
    }

}