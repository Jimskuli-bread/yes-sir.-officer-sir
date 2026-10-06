using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class REDCHAIR : MonoBehaviour
{
    private const float DialogueDuration = 9f;

    public float interactionRange = 4f;
    public float initialThrowForce = 25f;
    public float repeatThrowForceIncrease = 15f;
    public int apologiesRequired = 3;
    public string nextSceneName = "JAM";
    public bool HasBeenThrown => wasThrown;
    public bool CanBePickedUp => !wasThrown || apologizedThisThrow;
    public bool HasCompletedApologies => wasThrown && apologizedThisThrow && apologiesCompleted >= apologiesRequired;

    private Transform player;
    private bool wasThrown;
    private bool apologizedThisThrow;
    private bool playerIsHoldingChair;
    private int throwCount;
    private int apologiesCompleted;
    private string dialogue = "";
    private float dialogueUntil;

    private void Awake()
    {
        int pickableLayer = LayerMask.NameToLayer("Pickable");
        if (pickableLayer >= 0)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = pickableLayer;
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            Vector3 localSize = transform.InverseTransformVector(bounds.size);
            boxCollider.center = transform.InverseTransformPoint(bounds.center);
            boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        Rigidbody body = GetComponent<Rigidbody>();
        body.mass = 4f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void MarkThrown(Transform thrower)
    {
        player = thrower;
        wasThrown = true;
        apologizedThisThrow = false;
        playerIsHoldingChair = false;
        throwCount++;

        string insult = throwCount switch
        {
            1 => "Chair: Ouch! You absolute klutz. Press Q and apologize properly.",
            2 => "Chair: You again. Damn it! Put me down, you idiot.",
            3 => "Chair: You keep throwing me like a clumsy bastard. Drop me!",
            _ => "Chair: Bloody hell. Again. You're the worst owner I've ever had!"
        };

        ShowDialogue("YEET!!!\n" + insult);
    }

    public float GetThrowForce()
    {
        return initialThrowForce + Mathf.Max(0, throwCount - 1) * repeatThrowForceIncrease;
    }

    public void MarkPickedUp(Transform carrier)
    {
        player = carrier;
        wasThrown = false;
        apologizedThisThrow = false;
        playerIsHoldingChair = true;
        dialogueUntil = Time.time;
    }

    public void UpdatePlayerProximity(Transform interactor, bool isHoldingChair)
    {
        if (Vector3.Distance(transform.position, interactor.position) > interactionRange)
        {
            if (player == interactor && Time.time >= dialogueUntil)
                player = null;
            return;
        }

        player = interactor;
        playerIsHoldingChair = isHoldingChair;
    }

    public void Apologize(Transform interactor)
    {
        if (Vector3.Distance(transform.position, interactor.position) > interactionRange)
            return;

        player = interactor;
        if (!wasThrown)
        {
            ShowDialogue("Chair: Don't apologize yet. You haven't even thrown me.");
            return;
        }

        if (apologizedThisThrow)
        {
            ShowDialogue("Chair: I heard you. Pick me up and throw me again.");
            return;
        }

        apologizedThisThrow = true;
        apologiesCompleted++;
        string chairResponse = apologiesCompleted >= apologiesRequired
            ? "Chair: Fine. That's enough. Pick me up and get moving."
            : "Chair: Apology accepted. Pick me up and throw me again, you idiot.";
        ShowDialogue($"You: I'm sorry I threw you.\n{chairResponse}");
    }

    public void RefusePickup(Transform interactor)
    {
        player = interactor;
        ShowDialogue("Chair: Apologize before you try to pick me up.");
    }

    private void ShowDialogue(string message)
    {
        dialogue = message;
        dialogueUntil = Time.time + DialogueDuration;
        Debug.Log(message);
    }

    private void OnGUI()
    {
        bool showingDialogue = Time.time < dialogueUntil;
        if (player == null || (!showingDialogue && Vector3.Distance(transform.position, player.position) > interactionRange))
            return;

        string message;
        if (showingDialogue)
            message = dialogue;
        else if (playerIsHoldingChair)
            message = "Press E or Left Mouse to throw the chair";
        else if (!wasThrown)
            message = "Press E to pick up the chair";
        else if (!apologizedThisThrow)
            message = "Press Q to apologize to the chair";
        else if (apologiesCompleted < apologiesRequired)
            message = "Press E to pick up and throw again";
        else
            message = "Press E to pick up the chair and continue";

        float boxWidth = Mathf.Min(Screen.width - 32f, 760f);
        float boxHeight = showingDialogue ? 150f : 58f;
        float boxX = (Screen.width - boxWidth) * 0.5f;
        float boxY = Screen.height - boxHeight - 32f;
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = showingDialogue ? 26 : 18,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), message, style);
    }
}