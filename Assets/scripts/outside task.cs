using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class outsidetask : MonoBehaviour
{
    private Coroutine taskTimer;

    private void Start()
    {
        RestartTimer();
    }

    private IEnumerator CompleteTaskAndReturn()
    {
        while (!NPC.IsTaskActive(1))
        {
            yield return null;
        }

        yield return new WaitForSeconds(30f);

        if (!NPC.IsTaskActive(1))
            yield break;

        NPC.CompleteSecondTask();
        SceneReturnTracker.ReturnToPreviousScene();
    }

    private void RestartTimer()
    {
        if (taskTimer != null)
        {
            StopCoroutine(taskTimer);
        }

        taskTimer = StartCoroutine(CompleteTaskAndReturn());
    }

    public static void ResetTimersForTesting()
    {
        outsidetask[] timers = FindObjectsByType<outsidetask>(FindObjectsSortMode.None);
        foreach (outsidetask timer in timers)
        {
            timer.RestartTimer();
        }
    }
}

public static class SceneReturnTracker
{
    private const string PreviousSceneKey = "PreviousScene";

    public static void LoadScene(string sceneName)
    {
        PlayerPrefs.SetString(PreviousSceneKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneName);
    }

    public static void ReturnToPreviousScene()
    {
        PlayerPrefs.DeleteKey(PreviousSceneKey);
        SceneManager.LoadScene("Office");
    }
}
