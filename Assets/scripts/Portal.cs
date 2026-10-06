using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    [Tooltip("Name of the scene to load when the player enters the portal.")]
    public string targetSceneName;

    [Tooltip("Tag to identify the player GameObject.")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
