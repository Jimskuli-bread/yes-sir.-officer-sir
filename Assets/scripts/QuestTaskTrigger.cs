using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuestTaskTrigger : MonoBehaviour
{
    public enum TriggerAction
    {
        CompleteTask,
        FindSteve
    }

    [SerializeField] private TriggerAction action;
    [SerializeField, Range(1, 10)] private int taskNumber = 8;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerPickup>() == null)
            return;

        int taskIndex = taskNumber - 1;
        if (!NPC.IsTaskActive(taskIndex))
            return;

        if (action == TriggerAction.FindSteve)
        {
            NPC.RegisterSteveFound();
        }
        else
        {
            NPC.CompleteTaskInLoadedScenes(taskIndex);
            SceneReturnTracker.ReturnToPreviousScene();
        }
    }
}