using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class RedChairQuest : MonoBehaviour
{
    [SerializeField] private float apologyRange = 2.5f;

    private bool wasThrown;
    private bool hitWall;
    private Transform player;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (!hitWall || !NPC.IsTaskActive(4))
            return;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player == null || Vector3.Distance(player.position, transform.position) > apologyRange)
            return;

        if (WasInteractPressed())
        {
            NPC.CompleteTaskInLoadedScenes(4);
            SceneReturnTracker.ReturnToPreviousScene();
        }
    }

    public void MarkThrown()
    {
        wasThrown = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!wasThrown || collision.relativeVelocity.magnitude < 1f || collision.contactCount == 0)
            return;

        Vector3 surfaceNormal = collision.GetContact(0).normal.normalized;
        if (Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) < 0.3f)
        {
            hitWall = true;
        }
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}