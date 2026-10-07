using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;

    [Header("Stamina Settings")]
    public float maxStamina = 5f;
    public float staminaDrainRate = 1f;
    public float staminaRegenRate = 0.5f;

    [Header("References")]
    public Transform cameraTransform;
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.2f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.25f;
    [SerializeField] private LayerMask groundMask = (1 << 0) | (1 << 3);

#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionReference sprintAction;
#endif

    private Rigidbody rb;
    private float currentStamina;
    private bool isSprinting;
    private bool jumpRequested;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent unwanted rotation

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (groundCheck == null)
            groundCheck = transform.Find("GroundCheck");

        PlayerPickup.EnsureAttached(gameObject, cameraTransform);
        currentStamina = maxStamina;
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        sprintAction?.action.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        sprintAction?.action.Disable();
#endif
    }

    private void FixedUpdate()
    {
        HandleMovement();

        if (jumpRequested)
        {
            TryJump();
            jumpRequested = false;
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpRequested = true;
#else
        if (Input.GetKeyDown(KeyCode.Space))
            jumpRequested = true;
#endif
    }

    private Vector2 GetMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return Vector2.zero;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;
        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;
        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;
        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        return input;
#else
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) input.y += 1f;
        if (Input.GetKey(KeyCode.S)) input.y -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        return input;
#endif
    }

    private bool IsSprintingHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (sprintAction != null)
            return sprintAction.action.IsPressed();

        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    void HandleMovement()
    {
        Vector2 moveInput = GetMovementInput();
        float x = moveInput.x;
        float z = moveInput.y;

        bool sprintInput = IsSprintingHeld();

        if (sprintInput && currentStamina > 0 && (x != 0 || z != 0))
        {
            isSprinting = true;
            currentStamina -= staminaDrainRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
        else
        {
            isSprinting = false;
            currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Transform movementFrame = cameraTransform != null && cameraTransform.parent != null
            ? cameraTransform.parent
            : transform;
        Vector3 forward = movementFrame.forward;
        Vector3 right = movementFrame.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * z + right * x;
        if (direction.magnitude > 1f)
            direction.Normalize();

        Vector3 velocity = rb.linearVelocity;
        velocity.x = direction.x * speed;
        velocity.z = direction.z * speed;
        rb.linearVelocity = velocity;
    }

    private void TryJump()
    {
        if (groundCheck == null)
            return;

        Collider[] overlaps = Physics.OverlapSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore);
        bool isGrounded = false;
        foreach (Collider overlap in overlaps)
        {
            if (overlap.attachedRigidbody == rb)
                continue;

            isGrounded = true;
            break;
        }

        if (!isGrounded)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        rb.linearVelocity = velocity;
    }

}
