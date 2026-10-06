using UnityEngine;

public class EnemyKillCounter : MonoBehaviour
{
    public int killCount = 0;
    public int requiredKills = 10;

    public TaskManager taskManager;
    private bool questTaskCompleted;

    private void Update()
    {
        TryCompleteQuestTask();
    }

    public void RegisterKill()
    {
        killCount++;
        TryCompleteQuestTask();
    }

    private void TryCompleteQuestTask()
    {
        if (killCount >= requiredKills && !questTaskCompleted && NPC.IsTaskActive(3))
        {
            questTaskCompleted = true;
            NPC.CompleteFourthTask();
            if (taskManager != null)
            {
                taskManager.CompleteKillTask();
            }
        }
    }
}
