using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float smoothTime = 0.1f; // Time for smoothing
    public Transform playerBody;  // Assign the player GameObject's transform here

    private float xRotation = 0f;  // Vertical rotation
    private float currentBodyRotationY; // Current horizontal rotation of the player body

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;  // Lock and hide cursor
        Cursor.visible = false;
        if (playerBody != null)
            currentBodyRotationY = playerBody.eulerAngles.y;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;
#else
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
#endif

        // Vertical rotation (look up/down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply vertical rotation to the camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null && Mathf.Abs(mouseX) > 0.0001f)
        {
            currentBodyRotationY += mouseX;
            playerBody.rotation = Quaternion.Euler(0f, currentBodyRotationY, 0f);
        }
    }
}
