using UnityEngine;

public class EnemyKillCounter : MonoBehaviour
{
    public int killCount = 0;
    public int requiredKills = 10;

    public TaskManager taskManager;

    public void RegisterKill()
    {
        killCount++;

        if (killCount >= requiredKills)
        {
            taskManager.CompleteKillTask();
        }
    }
}
