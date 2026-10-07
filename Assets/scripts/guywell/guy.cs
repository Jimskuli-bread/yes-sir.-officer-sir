using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class guy : MonoBehaviour
{
    [SerializeField] private Collider wellTrigger;

    private bool taskCompleted;
    private Collider guyCollider;

    private void Awake()
    {
        guyCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        int pickableLayer = LayerMask.NameToLayer("Pickable");
        if (pickableLayer >= 0)
        {
            gameObject.layer = pickableLayer;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCompleteTask(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCompleteTask(other);
    }

    private void FixedUpdate()
    {
        if (!taskCompleted && guyCollider != null && wellTrigger != null &&
            guyCollider.bounds.Intersects(wellTrigger.bounds))
        {
            CompleteTaskAtWell();
        }
    }

    private void TryCompleteTask(Collider other)
    {
        if (taskCompleted || wellTrigger == null || other != wellTrigger)
        {
            return;
        }

        CompleteTaskAtWell();
    }

    private void CompleteTaskAtWell()
    {
        if (taskCompleted)
            return;

        taskCompleted = true;
        NPC.CompleteThirdTask();
        Destroy(gameObject);
        SceneReturnTracker.ReturnToPreviousScene();
    }
}
