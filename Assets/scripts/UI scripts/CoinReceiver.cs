using UnityEngine;
using UnityEngine.SceneManagement;

public class CoinReceiver : MonoBehaviour
{
    [Header("Assign the Player Prefab")]
    public GameObject playerPrefab;

    [Header("Assign the Coin Prefab")]
    public GameObject coinPrefab;

    [Header("Next Scene")]
    public string nextSceneName = "Level2";

    private void OnTriggerEnter(Collider other)
    {
        // Compare by prefab name (runtime safe)
        if (other.gameObject.name.Contains(playerPrefab.name))
        {
            Debug.Log("Player detected by prefab name.");
        }

        if (other.gameObject.name.Contains(coinPrefab.name))
        {
            Destroy(other.gameObject);
            SceneReturnTracker.LoadScene(nextSceneName);
        }
    }
}
