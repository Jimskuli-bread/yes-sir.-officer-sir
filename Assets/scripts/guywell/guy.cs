using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class guy : MonoBehaviour
{
    [SerializeField] private Collider wellTrigger;

    private bool taskCompleted;

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
        if (taskCompleted || other != wellTrigger)
        {
            return;
        }

        taskCompleted = true;
        NPC.CompleteThirdTask();
        Destroy(gameObject);
        SceneReturnTracker.ReturnToPreviousScene();
    }
}
