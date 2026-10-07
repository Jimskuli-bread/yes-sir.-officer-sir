using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMovement : MonoBehaviour
{
    public float moveSpeed = 12.0f;
    public float jumpHeight = 1.25f;
    public float groundDistance = 0.3f;
    public LayerMask groundLayer;

    public Transform groundCheck;

    private CharacterController characterController;
    private Transform cameraTransform;
    private Vector3 velocity;
    private bool isGrounded;
    private float gravity = -24f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        PlayerPickup.EnsureAttached(gameObject, cameraTransform);

        if (groundCheck == null)
        {
            Debug.LogError("Assign a GroundCheck object to the PlayerMovement script in the inspector.");
            this.enabled = false;
            return;
        }
    }

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Transform movementFrame = cameraTransform != null && cameraTransform.parent != null
            ? cameraTransform.parent
            : transform;
        Vector3 forwardDirection = movementFrame.forward;
        Vector3 rightDirection = movementFrame.right;
        forwardDirection.y = 0f;
        rightDirection.y = 0f;
        forwardDirection.Normalize();
        rightDirection.Normalize();

        Vector3 desiredDirection = (forwardDirection * vertical + rightDirection * horizontal).normalized;
        Vector3 movement = desiredDirection * moveSpeed * Time.deltaTime;

        characterController.Move(movement);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        HandleGravity();

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleGravity()
    {
        // Normal gravity only � flight script disables this entire component
        velocity.y += gravity * Time.deltaTime;
    }
}
