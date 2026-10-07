using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    [Header("Portal Settings")]
    [Tooltip("Name of the scene to load when the player enters the portal.")]
    public string targetSceneName;

    [Tooltip("Tag to identify the player GameObject.")]
    public string playerTag = "Player";

    [Tooltip("Optional zero-based task index required before this portal can be used. Leave at -1 to allow it at any time.")]
    [SerializeField] private int requiredTaskIndex = -1;

    private bool isLoadingScene;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoadingScene || !IsPlayer(other))
            return;

        if (requiredTaskIndex >= 0 && !NPC.IsTaskActive(requiredTaskIndex))
            return;

        string destination = ResolveTargetScene();
        if (destination == null)
            return;

        isLoadingScene = true;
        SceneReturnTracker.LoadScene(destination);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag) || other.GetComponentInParent<PlayerPickup>() != null;
    }

    private string ResolveTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("Portal has no target scene assigned.", this);
            return null;
        }

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (string.Equals(targetSceneName, sceneName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetSceneName, scenePath, StringComparison.OrdinalIgnoreCase))
            {
                return scenePath;
            }
        }

        Debug.LogError($"Portal target '{targetSceneName}' is not enabled in Build Settings.", this);
        return null;
    }
}
