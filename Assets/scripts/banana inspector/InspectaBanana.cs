using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InspectaBanana : MonoBehaviour
{
    public static bool IsEnding { get; private set; }

    [SerializeField] private float rotationSensitivity = 0.2f;
    [SerializeField] private AudioClip rotationSound;
    [Header("Banana Endings")]
    [TextArea(2, 4)]
    [SerializeField] private string secretEndingText = "congrats you ate the banana good job";

    private CameraControl cameraControl;
    private AudioSource rotationAudioSource;
    private bool wasCameraControlEnabled;
    private bool isRotating;
    private bool playedRotationSound;
    private string endingMessage;

    private void Awake()
    {
        IsEnding = false;
        rotationAudioSource = GetComponent<AudioSource>();
        if (rotationAudioSource == null)
            rotationAudioSource = gameObject.AddComponent<AudioSource>();

        rotationAudioSource.playOnAwake = false;
        rotationAudioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        if (!string.IsNullOrEmpty(endingMessage))
            return;

        PlayerPickup playerPickup = GetComponentInParent<PlayerPickup>();
        bool isHeld = playerPickup != null && playerPickup.HeldObject == gameObject;

        if (SceneManager.GetActiveScene().name == "Banana")
        {
            if (isHeld && IsEatButtonPressed())
            {
                FinishEnding(secretEndingText);
                return;
            }

        }

        if (!isHeld)
        {
            StopRotating();
            return;
        }

        if (!IsRotateButtonHeld())
        {
            StopRotating();
            return;
        }

        if (!isRotating)
            StartRotating(playerPickup);

        Vector2 mouseDelta = ReadMouseDelta();
        if (mouseDelta.sqrMagnitude > 0f)
        {
            NPC.CompleteBananaQuestionTaskWithBanana();

            if (!playedRotationSound)
            {
                if (rotationSound != null)
                    rotationAudioSource.PlayOneShot(rotationSound);

                playedRotationSound = true;
            }
        }

        transform.Rotate(Vector3.up, mouseDelta.x * rotationSensitivity, Space.Self);
        transform.Rotate(Vector3.right, -mouseDelta.y * rotationSensitivity, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryFinishTargetEnding(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryFinishTargetEnding(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryFinishTargetEnding(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryFinishTargetEnding(collision.collider);
    }

    private void TryFinishTargetEnding(Collider other)
    {
        if (SceneManager.GetActiveScene().name != "Banana" || !string.IsNullOrEmpty(endingMessage))
            return;

        BananaEndingTarget endingTarget = other.GetComponentInParent<BananaEndingTarget>();
        if (endingTarget != null)
            FinishEnding(endingTarget.EndingText);
    }

    private void FinishEnding(string message)
    {
        if (!string.IsNullOrEmpty(endingMessage))
            return;

        endingMessage = message;
        IsEnding = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StopRotating();
        foreach (Renderer bananaRenderer in GetComponentsInChildren<Renderer>())
            bananaRenderer.enabled = false;
        foreach (Collider bananaCollider in GetComponentsInChildren<Collider>())
            bananaCollider.enabled = false;

        Rigidbody bananaBody = GetComponent<Rigidbody>();
        if (bananaBody != null)
        {
            bananaBody.linearVelocity = Vector3.zero;
            bananaBody.angularVelocity = Vector3.zero;
            bananaBody.isKinematic = true;
        }
    }

    private bool IsEatButtonPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.K);
#endif
    }

    private bool IsRotateButtonHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
        return Input.GetMouseButton(1);
#endif
    }

    private Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private void StartRotating(PlayerPickup playerPickup)
    {
        isRotating = true;
        cameraControl = playerPickup.GetComponentInChildren<CameraControl>(true);
        if (cameraControl == null && Camera.main != null)
            cameraControl = Camera.main.GetComponent<CameraControl>();

        if (cameraControl == null)
            return;

        wasCameraControlEnabled = cameraControl.enabled;
        cameraControl.enabled = false;
    }

    private void StopRotating()
    {
        if (!isRotating)
            return;

        isRotating = false;
        playedRotationSound = false;
        if (cameraControl != null)
            cameraControl.enabled = wasCameraControlEnabled;

        cameraControl = null;
    }

    private void OnDisable()
    {
        StopRotating();
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(endingMessage))
            return;

        GUIStyle endingStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        GUI.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        GUI.Box(new Rect(Screen.width * 0.15f, Screen.height * 0.4f, Screen.width * 0.7f, 100f),
            endingMessage, endingStyle);

        Rect restartButton = new Rect(Screen.width * 0.4f, Screen.height * 0.4f + 116f, Screen.width * 0.2f, 48f);
        if (GUI.Button(restartButton, "Restart Game"))
        {
            NPC.ResetProgressForNewGame();
            IsEnding = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene("Office");
        }
    }
}