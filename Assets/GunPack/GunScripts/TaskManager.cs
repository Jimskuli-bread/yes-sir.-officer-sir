using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TaskManager : MonoBehaviour
{
    [Header("Mission Requirements")]
    public int coinsRequired = 5;
    public int coinsDelivered = 0;

    public int killsRequired = 10;
    public int killsDone = 0;

    private bool coinTaskComplete = false;
    private bool killTaskComplete = false;

    [Header("UI")]
    public TMP_Text coinTaskText;
    public TMP_Text killTaskText;
    public TMP_Text finalTaskText;

    [Header("Final Objective")]
    public GameObject finalCoinPrefab;
    public Transform finalCoinSpawnPoint;

    [Header("Win Scene")]
    public string winSceneName = "WinScene";

    void Start()
    {
        UpdateUI();
    }

    // -----------------------------
    // COIN DELIVERY
    // -----------------------------
    public void RegisterCoinDelivery()
    {
        coinsDelivered++;

        if (coinsDelivered >= coinsRequired)
            coinTaskComplete = true;

        UpdateUI();
        CheckAllTasks();
    }

    // -----------------------------
    // ENEMY KILL COUNT
    // -----------------------------
    public void RegisterKill()
    {
        killsDone++;

        if (killsDone >= killsRequired)
            killTaskComplete = true;

        UpdateUI();
        CheckAllTasks();
    }

    // -----------------------------
    // CHECK IF BOTH TASKS ARE DONE
    // -----------------------------
    private void CheckAllTasks()
    {
        if (coinTaskComplete && killTaskComplete)
        {
            SpawnFinalCoin();
            finalTaskText.text = "Final Task: Deliver the last coin!";
        }
    }

    // -----------------------------
    // SPAWN FINAL COIN
    // -----------------------------
    private void SpawnFinalCoin()
    {
        Instantiate(finalCoinPrefab, finalCoinSpawnPoint.position, finalCoinSpawnPoint.rotation);
    }

    // -----------------------------
    // FINAL COIN DELIVERED
    // -----------------------------
    public void FinalCoinDelivered()
    {
        SceneManager.LoadScene(winSceneName);
    }

    public void CompleteKillTask()
    {
        killTaskComplete = true;
        CheckAllTasks();
    }

    // -----------------------------
    // UPDATE UI TEXT
    // -----------------------------
    private void UpdateUI()
    {
        if (coinTaskText != null)
            coinTaskText.text = $"Coins Delivered: {coinsDelivered}/{coinsRequired}";

        if (killTaskText != null)
            killTaskText.text = $"Enemies Killed: {killsDone}/{killsRequired}";

        if (finalTaskText != null)
        {
            if (!coinTaskComplete || !killTaskComplete)
            finalTaskText.text = "Final Task: Locked";
        }
    }
}
