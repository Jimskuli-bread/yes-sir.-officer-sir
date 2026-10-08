using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NPC : MonoBehaviour
{
    public GameObject banana;
    private const string SecondTaskCompleteKey = "NPC.SecondTaskComplete";
    private const string ThirdTaskCompleteKey = "NPC.ThirdTaskComplete";
    private const string BananaQuestionTaskCompleteKey = "NPC.SeventhTaskComplete";
    private const string SteveFoundKey = "NPC.SteveFound";

    [System.Serializable]
    public class TaskStep
    {
        [TextArea(2, 4)]
        public string story;
        [TextArea(2, 4)]
        public string objective;
        [TextArea(2, 4)]
        public string completionText;
        [HideInInspector]
        public bool isComplete;
        [HideInInspector]
        public bool storyShown;
        [HideInInspector]
        public bool objectiveShown;
    }

    [Header("Quest Settings")]
    [Tooltip("Edit task dialogue and objectives here. The built-in gameplay events use the default task slots in order.")]
    public TaskStep[] tasks = new TaskStep[8];
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public Transform player;
    [SerializeField] private GameObject bananaPrefab;
    [Header("Quest Testing")]
    [Tooltip("Check a task here to force it complete during play mode.")]
    [SerializeField] private bool[] testCompleteTasks = new bool[8];

    private int currentTaskIndex = 0;
    private bool playerNearby;
    private string currentDialogue = "";
    private bool steveFound;
    private bool bananaQuestionBananaSpawned;
    private bool bananaPrefabWarningShown;
    private int bananaQuestionDialogueStep;

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
        currentTaskIndex = GetNextTaskIndex();
        UpdateDialogueText();
    }

    private void Start()
    {
        FindPlayerIfNeeded();
    }

    private void Update()
    {
        if (WasResetKeyPressed())
        {
            ResetQuestProgress();
        }

        ApplyTestCompletionFlags();

        if (banana != null && tasks.Length > 1 && tasks[1].isComplete)
        {
            banana.SetActive(false);
        }

        if (currentTaskIndex == 4 && !tasks[4].isComplete)
        {
            EnsureBananaQuestionTaskBanana();
        }

        FindPlayerIfNeeded();
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

    private void FindPlayerIfNeeded()
    {
        if (player != null)
            return;

        PlayerPickup playerPickup = FindFirstObjectByType<PlayerPickup>();
        player = playerPickup != null
            ? playerPickup.transform
            : GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void HandleDialogue()
    {
        if (currentTaskIndex >= tasks.Length)
        {
            currentDialogue = "You completed every task. Thank you for your help.";
            return;
        }

        TaskStep currentTask = tasks[currentTaskIndex];

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
            LoadSceneForCurrentTask();
            return;
        }

        if (currentTaskIndex == 7 && !currentTask.isComplete)
        {
            CompleteTask(7);
            currentDialogue = "Screw you, I'm quitting.";
            SceneReturnTracker.LoadScene("Banana");
            return;
        }

        if (currentTaskIndex == 4 && BananaPickup.HasBanana && !currentTask.isComplete)
        {
            if (bananaQuestionDialogueStep == 0)
            {
                currentDialogue = "You: Banana, where did my life choices go wrong?";
                bananaQuestionDialogueStep = 1;
                return;
            }

            CompleteTask(4);
            currentDialogue = "Banana: ...";
            return;
        }

        if (currentTaskIndex == 3 && !currentTask.isComplete && IsHoldingRedChair())
        {
            CompleteTask(3);
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
        bananaQuestionBananaSpawned = false;
        bananaQuestionDialogueStep = 0;
        currentDialogue = tasks[0].story;
        if (banana != null)
        {
            banana.SetActive(true);
        }

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
        if (taskIndex < 0 || taskIndex >= 8)
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

    public static void ResetProgressForNewGame()
    {
        for (int i = 0; i < 8; i++)
        {
            string completionKey = GetTaskCompleteKey(i);
            if (completionKey != null)
                PlayerPrefs.DeleteKey(completionKey);
        }

        PlayerPrefs.DeleteKey(SteveFoundKey);
        PlayerPrefs.Save();
        BananaPickup.ResetBanana();
        outsidetask.ResetTimersForTesting();
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
        if (!HasRedChairComponent(chair))
            return false;

        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            if (npc.currentTaskIndex != 3 || npc.player == null ||
                Vector3.Distance(npc.transform.position, npc.player.position) > npc.interactionRange)
            {
                continue;
            }

            npc.CompleteTask(3);
            npc.currentDialogue = npc.tasks[3].completionText;
            return true;
        }

        return false;
    }

    public static void CompleteBananaQuestionTaskWithBanana()
    {
        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in loadedNpcs)
        {
            if (npc.currentTaskIndex != 4)
                continue;

            if (!npc.tasks[4].isComplete)
                npc.CompleteTask(4);

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

        return loadedNpcs.Length == 0 && GetNextTaskIndex() == taskIndex;
    }

    public static bool IsTaskActiveOrNoNpcLoaded(int taskIndex)
    {
        NPC[] loadedNpcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        if (loadedNpcs.Length == 0)
        {
            return GetNextTaskIndex() == taskIndex;
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

    private void LoadSceneForCurrentTask()
    {
        string sceneName = currentTaskIndex switch
        {
            1 => "Park",
            2 => "guywell",
            3 => "Red Chair",
            5 => "Touch Grass",
            6 => "Banana detector",
            _ => null
        };

        if (!string.IsNullOrEmpty(sceneName) && SceneManager.GetActiveScene().name != sceneName)
        {
            SceneReturnTracker.LoadScene(sceneName);
        }
    }

    private static int GetNextTaskIndex()
    {
        for (int i = 0; i < 8; i++)
        {
            string completionKey = GetTaskCompleteKey(i);
            if (completionKey == null || PlayerPrefs.GetInt(completionKey, 0) == 0)
                return i;
        }

        return 8;
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
            0 => "NPC.TaskComplete.0",
            3 => "NPC.TaskComplete.4",
            4 => BananaQuestionTaskCompleteKey,
            5 => "NPC.TaskComplete.7",
            6 => "NPC.TaskComplete.8",
            7 => "NPC.TaskComplete.9",
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

    private void EnsureBananaQuestionTaskBanana()
    {
        if (bananaQuestionBananaSpawned)
            return;

        if (bananaPrefab == null)
        {
            if (!bananaPrefabWarningShown)
            {
                Debug.LogWarning("Assign a banana prefab on the NPC to spawn the banana-question task.");
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
            bananaQuestionBananaSpawned = true;
        }
    }

    private bool IsHoldingRedChair()
    {
        GameObject heldObject = HeldObject;
        return HasRedChairComponent(heldObject);
    }

    private static bool HasRedChairComponent(GameObject obj)
    {
        return obj != null &&
               (obj.GetComponent<RedChairQuest>() != null ||
                obj.GetComponentInChildren<RedChairQuest>() != null ||
                obj.GetComponentInParent<RedChairQuest>() != null ||
                obj.GetComponent<REDCHAIR>() != null ||
                obj.GetComponentInChildren<REDCHAIR>() != null ||
                obj.GetComponentInParent<REDCHAIR>() != null);
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
        currentTaskIndex++;
        if (currentTaskIndex < tasks.Length)
        {
            ResetCurrentTaskFlags();
            UpdateDialogueText();
            Debug.Log("Next task: " + tasks[currentTaskIndex].objective);
        }
        else
        {
            currentTaskIndex = tasks.Length;
            currentDialogue = "You finished all tasks!";
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
        if (tasks != null && tasks.Length == 10)
        {
            tasks = new[] { tasks[0], tasks[1], tasks[2], tasks[4], tasks[6], tasks[7], tasks[8], tasks[9] };
        }
        else if (tasks == null || tasks.Length != 8)
        {
            tasks = new TaskStep[8];
        }

        if (testCompleteTasks != null && testCompleteTasks.Length == 10)
        {
            testCompleteTasks = new[]
            {
                testCompleteTasks[0], testCompleteTasks[1], testCompleteTasks[2], testCompleteTasks[4],
                testCompleteTasks[6], testCompleteTasks[7], testCompleteTasks[8], testCompleteTasks[9]
            };
        }
        else if (testCompleteTasks == null || testCompleteTasks.Length != 8)
        {
            testCompleteTasks = new bool[8];
        }

        string[] defaultStories =
        {
            "I feel like having a banana.",
            "GO OUTSIDE!",
            "You know my ex Pena? He is near our company well at the moment.",
            "There is a red chair that you need to go apologize to.",
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
            "Objective: Find the red chair you threw at the wall and apologize to it.",
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
            "You apologized to the chair. It says nothing, but the moment feels sincere.",
            "The banana remains silent. Somehow, that feels like an answer.",
            "You touched grass. It was grass.",
            "The detector has led you back to the first banana. Your collection is reunited.",
            "You quit. Steve is still missing, but that is no longer your problem."
        };

        for (int i = 0; i < tasks.Length; i++)
        {
            if (tasks[i] == null)
                tasks[i] = new TaskStep
                {
                    story = defaultStories[i],
                    objective = defaultObjectives[i],
                    completionText = defaultCompletion[i]
                };

            if (string.IsNullOrWhiteSpace(tasks[i].story))
                tasks[i].story = defaultStories[i];
            if (string.IsNullOrWhiteSpace(tasks[i].objective))
                tasks[i].objective = defaultObjectives[i];
            if (string.IsNullOrWhiteSpace(tasks[i].completionText))
                tasks[i].completionText = defaultCompletion[i];

            tasks[i].storyShown = false;
            tasks[i].objectiveShown = false;
        }
    }

    private void OnGUI()
    {
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
