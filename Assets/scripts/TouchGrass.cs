using UnityEngine;

public class TouchGrass : MonoBehaviour
{
    [SerializeField] private Transform grassTarget;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float touchDistance = 1f;

    private CharacterController characterController;
    private Rigidbody body;
    private PlayerMovement playerMovement;
    private FPSMovement fpsMovement;
    private bool playerMovementWasEnabled;
    private bool fpsMovementWasEnabled;
    private bool isMovingToGrass;

    private void Start()
    {
        if (grassTarget == null)
        {
            Debug.LogWarning("Assign a grass target to the TouchGrass component.", this);
            return;
        }

        characterController = GetComponent<CharacterController>();
        body = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        fpsMovement = GetComponent<FPSMovement>();

        playerMovementWasEnabled = playerMovement != null && playerMovement.enabled;
        fpsMovementWasEnabled = fpsMovement != null && fpsMovement.enabled;

        if (playerMovement != null)
            playerMovement.enabled = false;
        if (fpsMovement != null)
            fpsMovement.enabled = false;

        isMovingToGrass = true;
    }

    private void Update()
    {
        if (!isMovingToGrass)
            return;

        Vector3 direction = grassTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= touchDistance * touchDistance)
        {
            RestoreMovement();
            return;
        }

        Vector3 step = direction.normalized * moveSpeed * Time.deltaTime;
        if (characterController != null)
            characterController.Move(step);
        else if (body != null)
            body.MovePosition(body.position + step);
        else
            transform.position += step;
    }

    private void OnDisable()
    {
        RestoreMovement();
    }

    private void RestoreMovement()
    {
        if (!isMovingToGrass)
            return;

        isMovingToGrass = false;

        if (playerMovement != null)
            playerMovement.enabled = playerMovementWasEnabled;
        if (fpsMovement != null)
            fpsMovement.enabled = fpsMovementWasEnabled;
    }
}