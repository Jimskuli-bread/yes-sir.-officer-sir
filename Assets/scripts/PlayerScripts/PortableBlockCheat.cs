using UnityEngine;

public class PortableBlockCheat : MonoBehaviour
{
    [Header("Cheat Code Settings")]
    public string cheatCode = "BLOCK";
    public float cheatResetTime = 2f;

    [Header("Block Settings")]
    public GameObject blockPrefab;
    public float followSpeed = 6f;
    public float verticalOffset = 1.5f;

    private bool cheatEnabled = false;
    private GameObject activeBlock;

    private int cheatIndex = 0;
    private float lastCheatTime = 0f;

    // Reference to the flight cheat script
    private FlightCheatCode flightCheat;

    void Start()
    {
        flightCheat = GetComponent<FlightCheatCode>();
    }

    void Update()
    {
        HandleCheatCodeInput();
        UpdateBlockMovement();
        EnforceSingleCheatRule();
    }

    // -----------------------------------------
    // TYPE "BLOCK" TO TOGGLE THE PORTABLE BLOCK
    // -----------------------------------------
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
                                ToggleCheat();
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

    void ToggleCheat()
    {
        cheatEnabled = !cheatEnabled;

        if (cheatEnabled)
        {
            // Disable flight cheat if active
            if (flightCheat != null && flightCheat.isFlying)
            {
                flightCheat.isFlying = false;
                Debug.Log("Flight cheat disabled due to BLOCK cheat activation");
            }

            SpawnBlock();
        }
        else
        {
            if (activeBlock != null)
                Destroy(activeBlock);
        }

        Debug.Log(cheatEnabled ? "Portable Block Cheat Enabled" : "Portable Block Cheat Disabled");
    }

    // -----------------------------------------
    // ONLY ONE CHEAT CAN BE ACTIVE AT A TIME
    // -----------------------------------------
    void EnforceSingleCheatRule()
    {
        if (!cheatEnabled) return;

        // If flight cheat activates, disable block cheat
        if (flightCheat != null && flightCheat.isFlying)
        {
            cheatEnabled = false;
            if (activeBlock != null)
                Destroy(activeBlock);

            Debug.Log("BLOCK cheat disabled because FLY cheat activated");
        }
    }

    // -----------------------------------------
    // SPAWN BLOCK UNDER PLAYER
    // -----------------------------------------
    void SpawnBlock()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("PortableBlockCheat ERROR: No blockPrefab assigned!");
            return;
        }

        Vector3 spawnPos = transform.position - new Vector3(0, verticalOffset, 0);
        activeBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity);
    }

    // -----------------------------------------
    // PLATFORM‑LIKE FOLLOW BEHAVIOR
    // -----------------------------------------
    void UpdateBlockMovement()
    {
        if (!cheatEnabled || activeBlock == null) return;

        Vector3 targetPos = activeBlock.transform.position;

        // Horizontal follow
        targetPos.x = Mathf.Lerp(activeBlock.transform.position.x, transform.position.x, followSpeed * Time.deltaTime);
        targetPos.z = Mathf.Lerp(activeBlock.transform.position.z, transform.position.z, followSpeed * Time.deltaTime);

        // Vertical behavior
        float desiredY = transform.position.y - verticalOffset;

        if (desiredY > activeBlock.transform.position.y)
        {
            targetPos.y = Mathf.Lerp(activeBlock.transform.position.y, desiredY, followSpeed * Time.deltaTime);
        }

        activeBlock.transform.position = targetPos;
    }
}
