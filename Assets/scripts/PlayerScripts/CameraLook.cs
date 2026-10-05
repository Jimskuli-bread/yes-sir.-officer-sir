using UnityEngine;

public class CameraLook : MonoBehaviour

{

    public float mouseSensitivity = 100.0f;
    private Transform playerBody;
    private float xRotation = 0.0f;

    private void Start()

    {

        playerBody = transform.parent;

        Cursor.lockState = CursorLockMode.Locked; // lukitse kursori keskelle ruutua

    }

    private void Update()

    {

        RotateCamera();

    }

    private void RotateCamera()

    {

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90.0f, 90.0f); // niin että kamera ei pyöri ylösalaisin

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        playerBody.Rotate(Vector3.up * mouseX);

    }
}