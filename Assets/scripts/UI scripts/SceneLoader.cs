using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string sceneToLoad = "Level2"; // Assign your scene name here

    public void LoadNextScene()
    {
        SceneReturnTracker.LoadScene(sceneToLoad);
    }
}
