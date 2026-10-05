using UnityEngine;
using UnityEngine.InputSystem;

public class NPC : MonoBehaviour
{
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

    private int currentTaskIndex = 0;
    private bool playerNearby;
    private string currentDialogue = "";

    private void Awake()
    {
        CreateDefaultTasks();
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

        if (currentTaskIndex == 0 && BananaPickup.HasBanana)
        {
            if (!currentTask.isComplete)
            {
                CompleteCurrentTask();
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

    public void CompleteCurrentTask()
    {
        if (currentTaskIndex >= tasks.Length)
            return;

        tasks[currentTaskIndex].isComplete = true;
        currentDialogue = tasks[currentTaskIndex].completionText;
        Debug.Log(currentDialogue);
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
            "The market is short on fruit today, and my stomach is growling. I was hoping for a banana before I start my long day.",
            "The old woods keep a secret herb that only grows in the shade. The village healer says it can cure a fever.",
            "A merchant is waiting for his wagon to roll again, but the wheels are broken and the road is getting crowded.",
            "The town's package courier never returned from the square, and the mayor is worried the goods will spoil.",
            "The cave has become dangerous. The townsfolk whisper that slimes are gathering near the tunnel mouth.",
            "The tower watch is tired and the wall needs more stones before the next storm rolls in.",
            "A farmer says a child is trapped beneath the collapsed wall near the east field. We need help now.",
            "The village well has run dry, and the people are desperate for a fresh supply of water.",
            "A traveler lost a key near the bridge before sunset. The whole village is searching for it.",
            "You have become a true helper of this town, and the people are counting on your courage and kindness."
        };

        string[] defaultObjectives =
        {
            "Objective: Go get me a banana.",
            "Objective: Collect 3 herbs from the forest.",
            "Objective: Repair the broken wagon wheel.",
            "Objective: Deliver the package to the market.",
            "Objective: Defeat 2 slimes in the cave.",
            "Objective: Gather 5 stones for the tower.",
            "Objective: Rescue the trapped villager.",
            "Objective: Fetch water from the well.",
            "Objective: Pick up the lost key near the bridge.",
            "Objective: Return to the NPC and report your success."
        };

        string[] defaultCompletion =
        {
            "Thank you! I can finally eat something before the day gets worse.",
            "Wonderful. These herbs will help the healer and keep the village strong.",
            "Excellent. The wagon can finally move again and the merchant can continue on his route.",
            "Perfect timing. The package is safe, and the market will be grateful.",
            "You did it. The cave is safer now and the people can travel without fear.",
            "The tower is stronger already. The watch will be ready for the next storm.",
            "You saved them. The farmer is grateful, and the whole village will remember this kindness.",
            "You brought clean water to the people. The well is alive again with hope.",
            "You found the key. The traveler can finally leave in peace.",
            "You completed every task. The town honors your courage and kindness."
        };

        for (int i = 0; i < tasks.Length; i++)
        {
            if (tasks[i] == null)
                tasks[i] = new TaskStep();

            tasks[i].story = defaultStories[i];
            tasks[i].objective = defaultObjectives[i];
            tasks[i].completionText = defaultCompletion[i];
            tasks[i].isComplete = false;
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
