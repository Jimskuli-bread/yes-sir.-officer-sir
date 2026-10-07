using UnityEngine;
using UnityEngine.InputSystem;

public class InspectaBanana : MonoBehaviour
{
    [SerializeField] private float rotationSensitivity = 0.2f;
    [SerializeField] private AudioClip rotationSound;

    private CameraControl cameraControl;
    private AudioSource rotationAudioSource;
    private bool wasCameraControlEnabled;
    private bool isRotating;
    private bool playedRotationSound;

    private void Awake()
    {
        rotationAudioSource = GetComponent<AudioSource>();
        if (rotationAudioSource == null)
            rotationAudioSource = gameObject.AddComponent<AudioSource>();

        rotationAudioSource.playOnAwake = false;
        rotationAudioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        PlayerPickup playerPickup = GetComponentInParent<PlayerPickup>();
        bool isHeld = playerPickup != null && playerPickup.HeldObject == gameObject;

        if (!isHeld || !IsRotateButtonHeld())
        {
            StopRotating();
            return;
        }

        if (!isRotating)
            StartRotating(playerPickup);

        Vector2 mouseDelta = ReadMouseDelta();
        if (mouseDelta.sqrMagnitude > 0f)
        {
            NPC.CompleteSeventhTaskWithBanana();

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
}