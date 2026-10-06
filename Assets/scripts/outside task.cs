using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class outsidetask : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CompleteTaskAndReturn());
    }

    private IEnumerator CompleteTaskAndReturn()
    {
        yield return new WaitForSeconds(30f);

        NPC.CompleteSecondTask();
        SceneReturnTracker.ReturnToPreviousScene();
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
        string previousScene = PlayerPrefs.GetString(PreviousSceneKey);
        if (string.IsNullOrEmpty(previousScene))
        {
            Debug.LogWarning("No previous scene was recorded; cannot return.");
            return;
        }

        PlayerPrefs.DeleteKey(PreviousSceneKey);
        SceneManager.LoadScene(previousScene);
    }
}
