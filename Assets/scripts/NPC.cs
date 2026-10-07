using UnityEngine;
using UnityEngine.InputSystem;

public class NPC : MonoBehaviour
{
    private const string SecondTaskCompleteKey = "NPC.SecondTaskComplete";
    private const string ThirdTaskCompleteKey = "NPC.ThirdTaskComplete";
    private const string SeventhTaskCompleteKey = "NPC.SeventhTaskComplete";
    private const string SteveFoundKey = "NPC.SteveFound";

    [System.Serializable]
    public class TaskStep
    {
        public string story;
        public string objective;
        public string completionText;
        public bool isComplete;
        public bool storyShown;
        public bool objectiveShown;
    }

    [Header("Quest Settings")]
    public TaskStep[] tasks = new TaskStep[10];
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public Transform player;
    [SerializeField] private GameObject bananaPrefab;
    [Header("Quest Testing")]
    [Tooltip("Check a task here to force it complete during play mode.")]
    [SerializeField] private bool[] testCompleteTasks = new bool[10];

    private int currentTaskIndex = 0;
    private bool playerNearby;
    private string currentDialogue = "";
    private bool steveFound;
    private bool seventhBananaSpawned;
    private bool bananaPrefabWarningShown;
    private int seventhDialogueStep;

    public GameObject HeldObject => player != null
        ? player.GetComponentInParent<PlayerPickup>()?.HeldObject
        : null;

    private void Awake()
    {
        CreateDefaultTasks();
        for (int i = 0; i < tasks.Length; i++)
        {
            string completionKey = GetTaskCompleteKey(i);
            if (completionKey != null && PlayerPrefs.GetInt(completionKey, 0) == 1)
            {
                tasks[i].isComplete = true;
            }
        }

        steveFound = PlayerPrefs.GetInt(SteveFoundKey, 0) == 1;
        ApplyTestCompletionFlags();
        UpdateDialogueText();
    }

    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    private void Update()
    {
        if (WasResetKeyPressed())
        {
            ResetQuestProgress();
        }

        ApplyTestCompletionFlags();

        if (currentTaskIndex == 6 && !tasks[6].isComplete)
        {
            EnsureSeventhTaskBanana();
        }

        if (player == null)
            return;

        playerNearby = Vector3.Distance(transform.position, player.position) <= interactionRange;

        if (!playerNearby)
            return;

        if (IsInteractPressed())
        {
            HandleDialogue();
        }
    }

    private void HandleDialogue()
    {
        if (currentTaskIndex >= tasks.Length)
        {
            currentDialogue = "You completed every task. Thank you for your help.";
            return;
        }

        TaskStep currentTask = tasks[currentTaskIndex];

        if (currentTaskIndex == 9 && steveFound && !currentTask.isComplete)
        {
            CompleteTask(9);
            currentDialogue = "Screw you, I'm quitting.";
            return;
        }

        if (currentTaskIndex == 0 && BananaPickup.HasBanana)
        {
            if (!currentTask.isComplete)
            {
                CompleteTask(0);
            }

            AdvanceTask();
            return;
        }

        if (!currentTask.storyShown)
        {
            currentDialogue = currentTask.story;
            currentTask.storyShown = true;
            return;
        }

        if (!currentTask.objectiveShown)
        {
            currentDialogue = currentTask.objective;
            currentTask.objectiveShown = true;
            return;
        }

        if (currentTaskIndex == 6 && BananaPickup.HasBanana && !currentTask.isComplete)
        {
            if (seventhDialogueStep == 0)
            {
                currentDialogue = "You: Banana, where did my life choices go wrong?";
                seventhDialogueStep = 1;
                return;
            }

            CompleteTask(6);
            currentDialogue = "Banana: ...";
            return;
        }

        if (currentTaskIndex == 5 && !currentTask.isComplete && IsHoldingRedChair())
        {
            CompleteTask(5);
            currentDialogue = currentTask.completionText;
            return;
        }

        if (currentTask.isComplete)
        {
            currentDialogue = currentTask.completionText;
            AdvanceTask();
            return;
        }

        currentDialogue = "Come back when you've finished this task: " + currentTask.objective;
    }

    private bool IsInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Key key = interactKey switch
            {
                KeyCode.E => Key.E,
                KeyCode.Q => Key.Q,
                KeyCode.F => Key.F,
                KeyCode.Space => Key.Space,
                _ => Key.E
            };

            return Keyboard.current[key].wasPressedThisFrame;
        }

        return false;
#else
        return Input.GetKeyDown(interactKey);
#endif
    }

        private bool WasResetKeyPressed()
        {
    #if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftBracketKey.wasPressedThisFrame;
    #else
        return Input.GetKeyDown(KeyCode.LeftBracket);
    #endif
        }

    public void CompleteCurrentTask()
    {
        if (currentTaskIndex >= tasks.Length)
            return;

        tasks[currentTaskIndex].isComplete = true;
        currentDialogue = tasks[currentTaskIndex].completionText;
        Debug.Log(currentDialogue);
    }

    [ContextMenu("Reset Quest Progress (Testing)")]
    public void ResetQuestProgress()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play mode before resetting quest progress.");
            return;
        }

        for (int i = 0; i < tasks.Length; i++)
        {
            if (tasks[i] != null)
            {
                tasks[i].isComplete = false;
                tasks[i].storyShown = false;
                tasks[i].objectiveShown = false;
            }

            if (testCompleteTasks != null && i < testCompleteTasks.Length)
            {
                testCompleteTasks[i] = false;
            }

            string completionKey = GetTaskCompleteKey(i);
            if (completionKey != null)
            {
                PlayerPrefs.DeleteKey(completionKey);
            }
        }

        PlayerPrefs.DeleteKey(SteveFoundKey);
        PlayerPrefs.Save();
        steveFound = false;
        currentTaskIndex = 0;
        seventhBananaSpawned = false;
        seventhDialogueStep = 0;
        currentDialogue = tasks[0].story;

        PlayerPickup[] pickups = FindObjectsByType<PlayerPickup>(FindObjectsSortMode.None);
        foreach (PlayerPickup pickup in pickups)
        {
            pickup.ReleaseBananaForQuestReset();
        }

        BananaPickup.ResetBanana();
        outsidetask.ResetTimersForTesting();
        Debug.Log("Quest progress reset for testing.");
    }

    public void CompleteTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= tasks.Length || tasks[taskIndex] == null)
            return;

        string completionKey = GetTaskCompleteKey(taskIndex);
        if (completionKey != null)
        {
            PlayerPrefs.SetInt(completionKey, 1);
            PlayerPrefs.Save();
        }

        SetTaskComplete(taskIndex);
    }

    public static void CompleteTaskInLoadedScenes(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= 10)
            return;

        string completionKey = GetTaskCompleteKey(taskIndex);
        if (completionKey != null)
        {
            PlayerPrefs.SetInt(completionKey, 1);
            PlayerPrefs.Save();
        }

        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            npc.SetTaskComplete(taskIndex);
        }
    }

    public static void CompleteSecondTask()
    {
        CompleteTaskAcrossScenes(1);
    }

    public static void CompleteThirdTask()
    {
        CompleteTaskAcrossScenes(2);
    }

    public static void CompleteFourthTask()
    {
        CompleteTaskAcrossScenes(3);
    }

    public static void RegisterSteveFound()
    {
        PlayerPrefs.SetInt(SteveFoundKey, 1);
        PlayerPrefs.Save();

        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            npc.steveFound = true;
        }
    }

    public static bool TryDeliverRedChair(GameObject chair)
    {
        if (chair == null || chair.GetComponent<RedChairQuest>() == null)
            return false;

        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            if (npc.currentTaskIndex != 5 || npc.player == null ||
                Vector3.Distance(npc.transform.position, npc.player.position) > npc.interactionRange)
            {
                continue;
            }

            npc.CompleteTask(5);
            npc.currentDialogue = npc.tasks[5].completionText;
            return true;
        }

        return false;
    }

    public static void CompleteSeventhTaskWithBanana()
    {
        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            if (npc.currentTaskIndex != 6)
                continue;

            if (!npc.tasks[6].isComplete)
                npc.CompleteTask(6);

            npc.AdvanceTask();
        }
    }

    public static bool IsTaskActive(int taskIndex)
    {
        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            if (npc.currentTaskIndex == taskIndex)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsTaskActiveOrNoNpcLoaded(int taskIndex)
    {
        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        if (loadedNpcs.Length == 0)
        {
            string completionKey = GetTaskCompleteKey(taskIndex);
            return completionKey != null && PlayerPrefs.GetInt(completionKey, 0) == 0;
        }

        foreach (NPC npc in loadedNpcs)
        {
            if (npc.currentTaskIndex == taskIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static void CompleteTaskAcrossScenes(int taskIndex)
    {
        string completionKey = GetTaskCompleteKey(taskIndex);
        if (completionKey != null)
        {
            PlayerPrefs.SetInt(completionKey, 1);
            PlayerPrefs.Save();
        }

        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            npc.SetTaskComplete(taskIndex);
        }
    }

    private static string GetTaskCompleteKey(int taskIndex)
    {
        return taskIndex switch
        {
            1 => SecondTaskCompleteKey,
            2 => ThirdTaskCompleteKey,
            6 => SeventhTaskCompleteKey,
            >= 0 and < 10 => "NPC.TaskComplete." + taskIndex,
            _ => null
        };
    }

    private void ApplyTestCompletionFlags()
    {
        if (testCompleteTasks == null || tasks == null)
            return;

        int count = Mathf.Min(testCompleteTasks.Length, tasks.Length);
        for (int i = 0; i < count; i++)
        {
            if (testCompleteTasks[i] && tasks[i] != null && !tasks[i].isComplete)
            {
                SetTaskComplete(i);
            }
        }
    }

    private void EnsureSeventhTaskBanana()
    {
        if (seventhBananaSpawned)
            return;

        if (bananaPrefab == null)
        {
            if (!bananaPrefabWarningShown)
            {
                Debug.LogWarning("Assign a banana prefab on the NPC to spawn the task-seven banana.");
                bananaPrefabWarningShown = true;
            }

            return;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        PlayerPickup playerPickup = player != null
            ? player.GetComponentInParent<PlayerPickup>()
            : null;

        if (playerPickup != null && BananaPickup.SpawnIntoHand(bananaPrefab, playerPickup))
        {
            seventhBananaSpawned = true;
        }
    }

    private bool IsHoldingRedChair()
    {
        GameObject heldObject = HeldObject;
        return heldObject != null && heldObject.GetComponent<RedChairQuest>() != null;
    }

    private void SetTaskComplete(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= tasks.Length || tasks[taskIndex] == null)
            return;

        tasks[taskIndex].isComplete = true;
        if (currentTaskIndex == taskIndex)
        {
            currentDialogue = tasks[taskIndex].completionText;
        }
    }

    public void AdvanceTask()
    {
        if (currentTaskIndex < tasks.Length - 1)
        {
            currentTaskIndex++;
            ResetCurrentTaskFlags();
            UpdateDialogueText();
            Debug.Log("Next task: " + tasks[currentTaskIndex].objective);
        }
        else
        {
            currentTaskIndex = tasks.Length;
            currentDialogue = "You finished all 10 tasks!";
            Debug.Log(currentDialogue);
        }
    }

    private void ResetCurrentTaskFlags()
    {
        if (currentTaskIndex >= tasks.Length)
            return;

        tasks[currentTaskIndex].storyShown = false;
        tasks[currentTaskIndex].objectiveShown = false;
    }

    private void UpdateDialogueText()
    {
              if (currentTaskIndex >= tasks.Length)
        {
            currentDialogue = "You completed every task. Thank you for your help.";
            return;
        }

        currentDialogue = tasks[currentTaskIndex].story;
    }

    private void CreateDefaultTasks()
    {
        if (tasks == null || tasks.Length != 10)
        {
            tasks = new TaskStep[10];
        }

        string[] defaultStories =
        {
            "I feel like having a banana.",
            "GO OUTSIDE!",
            "You know my ex Pena? He is near our company well at the moment.",
            "THE SCAMMERS ARE BACK! Go clean up.",
            "There is a red chair that you need to go apologize to.",
            "I want the chair.",
            "You need help so pls go get help.",
            "You have spent too much time on your computer so go touch grass.",
            "Go find a banana using the other banana.",
            "Go get Steve."
        };

        string[] defaultObjectives =
        {
            "Objective: Pick up the banana and keep it with you. You may drop it briefly, but don't lose it.",
            "Objective: Stay outside for 30 seconds.",
            "Objective: Throw the guy into the well.",
            "Objective: Use the gun to clear the scammers out of the office.",
            "Objective: Find the red chair you threw at the wall and apologize to it.",
            "Objective: Take the red chair and deliver it to your boss.",
            "Objective: Ask the banana where your life choices went wrong.",
            "Objective: Go outside and touch grass.",
            "Objective: Use another banana and the banana detector to find the first banana.",
            "Objective: Find Steve."
        };

        string[] defaultCompletion =
        {
            "Banana secured. You will carry it for the foreseeable future.",
            "Thirty seconds outside have passed. Nobody knows why you were sent out there.",
            "The guy is in the well. You choose not to think too hard about it.",
            "The scammers are gone and the office is quiet again.",
            "You apologized to the chair. It says nothing, but the moment feels sincere.",
            "The chair has been delivered. Your boss seems pleased.",
            "The banana remains silent. Somehow, that feels like an answer.",
            "You touched grass. It was grass.",
            "The detector has led you back to the first banana. Your collection is reunited.",
            "You quit. Steve is still missing, but that is no longer your problem."
        };

        for (int i = 0; i < tasks.Length; i++)
        {
            if (tasks[i] == null)
                tasks[i] = new TaskStep();

            tasks[i].story = defaultStories[i];
            tasks[i].objective = defaultObjectives[i];
            tasks[i].completionText = defaultCompletion[i];
            tasks[i].storyShown = false;
            tasks[i].objectiveShown = false;
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width - 180f, 12f, 168f, 32f), "Reset Quest (Å)"))
        {
            ResetQuestProgress();
        }

        if (player == null || !playerNearby)
            return;

        float boxWidth = Screen.width * 0.68f;
        float boxHeight = Screen.height * 0.22f;
        float boxX = (Screen.width - boxWidth) * 0.5f;
        float boxY = Screen.height - boxHeight - 30f;

        string textToShow = currentTaskIndex < tasks.Length
            ? "Current task: " + tasks[currentTaskIndex].objective
            : "All tasks complete!";

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        GUIStyle dialogueStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            normal = { textColor = new Color(0.97f, 0.94f, 0.88f) },
            padding = new RectOffset(20, 20, 10, 10)
        };

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        GUI.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", panelStyle);

        GUI.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
        GUI.Box(new Rect(boxX + 12f, boxY + 12f, boxWidth - 24f, 36f), textToShow, panelStyle);

        GUI.backgroundColor = new Color(0.16f, 0.12f, 0.08f, 0.8f);
        GUI.Box(new Rect(boxX + 12f, boxY + 58f, boxWidth - 24f, boxHeight - 88f), currentDialogue, dialogueStyle);

        GUI.Label(new Rect(boxX + 12f, boxY + boxHeight - 22f, boxWidth - 24f, 18f), "Press E to continue", hintStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
