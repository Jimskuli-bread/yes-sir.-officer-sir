using UnityEngine;
using UnityEngine.InputSystem;

public class TouchGrass : MonoBehaviour
{
    [SerializeField] private Transform grassObject;
    [SerializeField] private float interactionDistance = 3f;

    private Collider grassCollider;
    private Transform player;
    private bool taskCompleted;

    private void Start()
    {
        if (grassObject == null)
            grassObject = transform;

        FPSMovement fpsMovement = FindObjectOfType<FPSMovement>();
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        player = fpsMovement != null ? fpsMovement.transform
            : playerMovement != null ? playerMovement.transform
            : taggedPlayer != null ? taggedPlayer.transform
            : null;

        if (grassObject == null)
        {
            Debug.LogWarning("Assign the grass Plane to the TouchGrass component.", this);
            return;
        }

        grassCollider = grassObject.GetComponent<Collider>();
        if (grassCollider == null)
            grassCollider = grassObject.GetComponentInChildren<Collider>();

        if (player == null)
            Debug.LogWarning("TouchGrass could not find a player with FPSMovement, PlayerMovement, or the Player tag.", this);
    }

    private void Update()
    {
        if (taskCompleted || !NPC.IsTaskActive(5) || grassObject == null || player == null || !IsNearGrass())
            return;

        if (IsInteractPressed())
        {
            taskCompleted = true;
            NPC.CompleteTaskInLoadedScenes(5);
            Debug.Log("Grass task completed.", this);
            SceneReturnTracker.ReturnToPreviousScene();
        }
    }

    private void OnGUI()
    {
        if (player == null || grassObject == null || !IsNearGrass())
            return;

        string message = taskCompleted ? "Grass task complete!" : "Press E to touch the grass";
        GUI.Label(new Rect(20f, Screen.height - 50f, 400f, 30f), message);
    }

    private bool IsNearGrass()
    {
        if (grassCollider == null)
            return Vector3.Distance(player.position, grassObject.position) <= interactionDistance;

        Vector3 closestPoint = grassCollider.ClosestPoint(player.position);
        return Vector3.Distance(player.position, closestPoint) <= interactionDistance;
    }

    private bool IsInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.eKey.wasPressedThisFrame;

    return false;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}