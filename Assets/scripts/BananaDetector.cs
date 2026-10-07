using UnityEngine;
using UnityEngine.SceneManagement;

public class BananaDetector : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float signalRadius = 12f;
    [SerializeField] private BananaPickup targetBanana;
    [SerializeField] private AudioClip signalClip;

    private bool targetRevealed;
    private BananaPickup buriedBanana;
    private Vector3 surfacePosition;
    private Scene targetScene;
    private AudioSource signalAudio;
    private bool generatedSignalClip;
    private Transform targetMarker;
    private Renderer targetMarkerRenderer;
    private Material targetMarkerMaterial;

    private void Awake()
    {
        targetScene = gameObject.scene;
        CapsuleCollider handleCollider = GetComponentInChildren<CapsuleCollider>(true);

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

        if (signalClip == null)
        {
            signalClip = CreateDefaultSignalClip();
            generatedSignalClip = true;
        }

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
        if (targetRevealed)
        {
            StopSignal();
            SetMarkerVisible(false);
            return;
        }

        PlayerPickup playerPickup = GetComponentInParent<PlayerPickup>();
        if (buriedBanana == null)
            FindBuriedBanana();

        if (playerPickup == null || playerPickup.HeldObject != gameObject || buriedBanana == null ||
            !buriedBanana.gameObject.scene.IsValid() || buriedBanana.gameObject.scene != targetScene)
        {
            StopSignal();
            SetMarkerVisible(false);
            return;
        }

        Vector3 detectorPosition = playerPickup.transform.position;
        Vector2 horizontalOffset = new Vector2(
            detectorPosition.x - surfacePosition.x,
            detectorPosition.z - surfacePosition.z);
        float distance = horizontalOffset.magnitude;
        UpdateSignal(distance);
        UpdateMarker(distance);

        if (distance > detectionRadius)
            return;

        buriedBanana.transform.position = surfacePosition;
        buriedBanana.gameObject.SetActive(true);
        targetRevealed = true;
        StopSignal();
        SetMarkerVisible(false);
        buriedBanana.RevealDetectorTarget();
        Debug.Log("Banana detector revealed the banana. Pick it up to return to the office.");
    }

    private void FindBuriedBanana()
    {
        if (!targetScene.IsValid())
            return;

        BananaPickup banana = targetBanana;
        if (banana == null)
        {
            BananaPickup[] bananas = FindObjectsByType<BananaPickup>(FindObjectsSortMode.None);
            foreach (BananaPickup candidate in bananas)
            {
                if (candidate != null && candidate.gameObject.scene == targetScene &&
                    candidate.GetComponent<BananaDetector>() == null)
                {
                    banana = candidate;
                    break;
                }
            }
        }

        if (banana == null || banana.gameObject.scene != targetScene ||
            banana.GetComponent<BananaDetector>() != null)
        {
            return;
        }

        buriedBanana = banana;
        buriedBanana.MarkAsDetectorTarget();
        surfacePosition = banana.transform.position;
        CreateTargetMarker(FindGroundPosition(surfacePosition, banana));
        Rigidbody body = banana.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
        }

        banana.gameObject.SetActive(false);
    }

    private void UpdateSignal(float distance)
    {
        if (signalClip == null || distance > signalRadius)
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

    private void UpdateMarker(float distance)
    {
        if (targetMarker == null)
            return;

        bool visible = distance <= signalRadius;
        SetMarkerVisible(visible);
        if (!visible)
            return;

        float closeness = 1f - Mathf.Clamp01(distance / signalRadius);
        float markerSize = Mathf.Lerp(0.035f, 0.13f, closeness);
        targetMarker.localScale = new Vector3(markerSize, 0.004f, markerSize);
        targetMarkerMaterial.color = Color.Lerp(
            new Color(0.28f, 0.22f, 0.08f),
            new Color(0.72f, 0.55f, 0.19f),
            closeness);
    }

    private void CreateTargetMarker(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "BananaDetectorMark";
        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
            Destroy(markerCollider);

        targetMarker = marker.transform;
        targetMarker.position = position;
        targetMarkerRenderer = marker.GetComponent<Renderer>();

        Shader markerShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (markerShader == null)
            markerShader = Shader.Find("Unlit/Color");

        if (markerShader != null)
        {
            targetMarkerMaterial = new Material(markerShader);
            targetMarkerRenderer.material = targetMarkerMaterial;
        }

        targetMarkerRenderer.enabled = false;
    }

    private Vector3 FindGroundPosition(Vector3 position, BananaPickup banana)
    {
        Vector3 rayOrigin = position + Vector3.up * 5f;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            100f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        Vector3 groundPosition = position - Vector3.up * 0.5f;
        Rigidbody bananaBody = banana.GetComponent<Rigidbody>();
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.normal.y < 0.65f ||
                hit.collider.transform == banana.transform || hit.collider.transform.IsChildOf(banana.transform) ||
                (bananaBody != null && hit.collider.attachedRigidbody == bananaBody) ||
                hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            groundPosition = hit.point + Vector3.up * 0.012f;
        }

        return groundPosition;
    }

    private void SetMarkerVisible(bool visible)
    {
        if (targetMarkerRenderer != null)
            targetMarkerRenderer.enabled = visible;
    }

    private void StopSignal()
    {
        if (signalAudio != null && signalAudio.isPlaying)
            signalAudio.Stop();
    }

    private static AudioClip CreateDefaultSignalClip()
    {
        const int sampleRate = 44100;
        const float clipDuration = 1f;
        const float beepDuration = 0.09f;
        const float fadeDuration = 0.01f;
        int sampleCount = Mathf.RoundToInt(sampleRate * clipDuration);
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
        if (generatedSignalClip && signalClip != null)
            Destroy(signalClip);

        if (targetMarker != null)
            Destroy(targetMarker.gameObject);

        if (targetMarkerMaterial != null)
            Destroy(targetMarkerMaterial);
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