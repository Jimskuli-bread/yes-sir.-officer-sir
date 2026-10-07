using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(PlayerPickup))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;

    [Header("Stamina Settings")]
    public float maxStamina = 5f;
    public float staminaDrainRate = 1f;
    public float staminaRegenRate = 0.5f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("References")]
    public Transform cameraTransform;

#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
#endif

    private Rigidbody rb;
    private bool isGrounded;
    private float currentStamina;
    private bool isSprinting;

    void Awake()
    {
        if (GetComponent<PlayerPickup>() == null)
            gameObject.AddComponent<PlayerPickup>();

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent unwanted rotation

        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
                Debug.LogError("GroundCheck not found! Create a child or assign manually.");
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        currentStamina = maxStamina;
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.action.Enable();
        sprintAction?.action.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.action.Disable();
        sprintAction?.action.Disable();
#endif
    }

    void Update()
    {
        UpdateGroundStatus();
        HandleMovement();
    }

    private void FixedUpdate()
    {
        StopUpwardMotion();
    }

    private void OnCollisionEnter(Collision collision)
    {
        StopUpwardMotion();
    }

    private void OnCollisionStay(Collision collision)
    {
        StopUpwardMotion();
    }

    private void StopUpwardMotion()
    {
        Vector3 velocity = rb.linearVelocity;
        if (velocity.y > 0f)
        {
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }
    }

    void UpdateGroundStatus()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    private Vector2 GetMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction != null)
            return moveAction.action.ReadValue<Vector2>();

        if (Keyboard.current == null)
            return Vector2.zero;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            input.y -= 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            input.x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            input.x += 1f;

        return input;
#else
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
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
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
        else
        {
            isSprinting = false;
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        // Movement relative to camera's horizontal direction
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * z + right * x;
        if (direction.magnitude > 1f)
            direction.Normalize();

        Vector3 move = direction * speed * Time.deltaTime;
        rb.MovePosition(rb.position + move);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
