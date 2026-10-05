using UnityEngine;

public class FlightCheatCode : MonoBehaviour
{
    [Header("Cheat Code Settings")]
    public string cheatCode = "FLY";
    public float cheatResetTime = 2f;

    [Header("Flight Settings")]
    public float flightSpeed = 10f;
    public float verticalSpeed = 8f;
    public float doubleTapTime = 0.3f;
    public FPSMovement movementScript;


    private CharacterController controller;
    private bool cheatEnabled = false;
    public bool isFlying = false;   // <-- IMPORTANT: other scripts can read this

    private int cheatIndex = 0;
    private float lastCheatTime = 0f;
    private float lastSpacePress = -1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleCheatCodeInput();
        DetectDoubleSpace();
        HandleFlightMovement();
    }

    // -----------------------------
    //  TYPE "FLY" TO ENABLE CHEAT
    // -----------------------------
    void HandleCheatCodeInput()
    {
        if (Time.time - lastCheatTime > cheatResetTime)
            cheatIndex = 0;

        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    string keyStr = key.ToString().ToUpper();

                    if (keyStr.Length == 1 && char.IsLetter(keyStr[0]))
                    {
                        char expected = cheatCode[cheatIndex];

                        if (keyStr[0] == expected)
                        {
                            cheatIndex++;
                            lastCheatTime = Time.time;

                            if (cheatIndex >= cheatCode.Length)
                            {
                                cheatIndex = 0;
                                cheatEnabled = !cheatEnabled;
                                Debug.Log(cheatEnabled ? "Cheat code active" : "Cheat code deactivated");
                            }
                        }
                        else
                        {
                            cheatIndex = 0;
                        }

                        break;
                    }
                }
            }
        }
    }

    // -----------------------------
    //  DOUBLE SPACE TO TOGGLE FLIGHT
    // -----------------------------
    void DetectDoubleSpace()
    {
        if (!cheatEnabled) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastSpacePress <= doubleTapTime)
                ToggleFlight();

            lastSpacePress = Time.time;
        }
    }

    void ToggleFlight()
    {
        isFlying = !isFlying;
        Debug.Log(isFlying ? "Flying enabled" : "Flying disabled");

        // Disable FPS movement while flying
        if (movementScript != null)
            movementScript.enabled = !isFlying;
    }


    // -----------------------------
    //  FLIGHT MOVEMENT (OVERRIDES GRAVITY)
    // -----------------------------
    void HandleFlightMovement()
    {
        if (!isFlying || controller == null) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = (transform.forward * v + transform.right * h) * flightSpeed;

        // Vertical flight control
        if (Input.GetKey(KeyCode.Space))
            move.y = verticalSpeed;
        else if (Input.GetKey(KeyCode.LeftShift))
            move.y = -verticalSpeed;
        else
            move.y = 0f;

        controller.Move(move * Time.deltaTime);
    }
}
