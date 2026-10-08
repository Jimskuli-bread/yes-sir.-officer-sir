using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRange = 3f;

    [Header("Hold Position")]
    public Transform holdPoint;

    [Header("Throw Aim")]
    [SerializeField] private Transform aimCamera;

    [Header("Throw Settings")]
    public KeyCode throwKey = KeyCode.F;
    public float throwForce = 1.2f;

    private GameObject heldObject;
    private Collider[] playerColliders;
    private Collider[] heldObjectColliders;
    private Vector3 heldLocalPosition;
    private Quaternion heldLocalRotation = Quaternion.identity;
    private bool heldObjectCanRotate;
    private int pickableLayer;
    private static PlayerPickup persistentPlayer;
    private static readonly Vector3 officeSpawnPosition = new Vector3(1.13f, 20.84f, -1.977f);
    private static readonly Vector3 redChairSpawnPosition = new Vector3(651.8785f, 1.03f, 215.303f);
    private static Quaternion officeSpawnRotation;
    private static bool hasOfficeSpawn;
    private AudioListener playerAudioListener;

    public GameObject HeldObject => heldObject;

    public static PlayerPickup EnsureAttached(GameObject player, Transform viewTransform)
    {
        if (player == null)
            return null;

        PlayerPickup pickup = player.GetComponent<PlayerPickup>();
        if (pickup == null)
            pickup = player.AddComponent<PlayerPickup>();

        if (pickup.holdPoint == null)
        {
            Transform parent = viewTransform != null ? viewTransform : player.transform;
            pickup.holdPoint = parent.Find("HoldPoint");
            if (pickup.holdPoint == null)
            {
                GameObject holdPointObject = new GameObject("HoldPoint");
                holdPointObject.transform.SetParent(parent, false);
                holdPointObject.transform.localPosition = new Vector3(0.2f, -0.15f, 1.2f);
                pickup.holdPoint = holdPointObject.transform;
            }
        }

        return pickup;
    }

    private void Awake()
    {
        if (!hasOfficeSpawn && SceneManager.GetActiveScene().name == "Office")
        {
            officeSpawnRotation = transform.rotation;
            hasOfficeSpawn = true;
            PlaceAtOfficeSpawn();
        }
        else if (SceneManager.GetActiveScene().name == "Red Chair")
        {
            PlaceAtPosition(redChairSpawnPosition);
        }

        if (persistentPlayer != null && persistentPlayer != this)
        {
            Destroy(gameObject);
            return;
        }

        pickableLayer = LayerMask.NameToLayer("Pickable");
        if (pickableLayer < 0)
        {
            Debug.LogError("Create a layer named 'Pickable' and assign it to objects the player can pick up.");
        }

        if (aimCamera == null && Camera.main != null)
        {
            aimCamera = Camera.main.transform;
        }

        playerAudioListener = GetComponentInChildren<AudioListener>(true);
        EnsureSingleAudioListener();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Office" && hasOfficeSpawn)
            PlaceAtOfficeSpawn();
        else if (scene.name == "Red Chair")
            PlaceAtPosition(redChairSpawnPosition);

        EnsureSingleAudioListener();
    }

    private void PlaceAtOfficeSpawn()
    {
        transform.rotation = officeSpawnRotation;
        PlaceAtPosition(officeSpawnPosition);
    }

    private void PlaceAtPosition(Vector3 position)
    {
        transform.position = position;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            return;

        body.position = position;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void EnsureSingleAudioListener()
    {
        if (playerAudioListener == null)
            return;

        playerAudioListener.enabled = true;
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (AudioListener listener in listeners)
        {
            if (listener != playerAudioListener)
                listener.enabled = false;
        }
    }

    private void Update()
    {
        REDCHAIR nearbyChair = FindNearbyChair();
        if (nearbyChair != null)
            nearbyChair.UpdatePlayerProximity(transform, IsHoldingChair(nearbyChair));

        if (nearbyChair != null && WasApologyPressed())
            nearbyChair.Apologize(transform);

        bool pickupPressed = WasPickupPressed();
        if (heldObject != null && GetRedChair(heldObject) != null && pickupPressed)
        {
            ThrowHeldObject();
        }
        else if (pickupPressed)
        {
            if (heldObject != null)
            {
                NPC.TryDeliverRedChair(heldObject);
                DropObject();
            }
            else if (nearbyChair != null)
            {
                if (nearbyChair.CanBePickedUp)
                {
                    bool completedApologies = nearbyChair.HasCompletedApologies;
                    string nextSceneName = nearbyChair.nextSceneName;
                    PickupObject(nearbyChair.gameObject);
                    if (completedApologies)
                    {
                        persistentPlayer = this;
                        DontDestroyOnLoad(transform.root.gameObject);
                        SceneReturnTracker.LoadScene(nextSceneName);
                    }
                }
                else
                {
                    nearbyChair.RefusePickup(transform);
                }
            }
            else
            {
                TryPickup();
            }
        }

        if (heldObject != null && WasThrowPressed())
            ThrowHeldObject();
    }

    private REDCHAIR FindNearbyChair()
    {
        REDCHAIR[] chairs = FindObjectsByType<REDCHAIR>(FindObjectsSortMode.None);
        REDCHAIR closestChair = null;
        float closestDistance = float.MaxValue;

        foreach (REDCHAIR chair in chairs)
        {
            float distance = (chair.transform.position - transform.position).sqrMagnitude;
            if (distance <= chair.interactionRange * chair.interactionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestChair = chair;
            }
        }

        return closestChair;
    }

    private bool IsHoldingChair(REDCHAIR chair)
    {
        if (heldObject == null)
            return false;

        Transform heldTransform = heldObject.transform;
        Transform chairTransform = chair.transform;
        return heldTransform == chairTransform ||
               heldTransform.IsChildOf(chairTransform) ||
               chairTransform.IsChildOf(heldTransform);
    }

    private static REDCHAIR GetRedChair(GameObject obj)
    {
        return obj.GetComponent<REDCHAIR>() ??
               obj.GetComponentInChildren<REDCHAIR>() ??
               obj.GetComponentInParent<REDCHAIR>();
    }

    private bool WasApologyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    private void LateUpdate()
    {
        if (heldObject == null || holdPoint == null)
            return;

        Transform heldTransform = heldObject.transform;
        if (heldTransform.parent != holdPoint)
        {
            heldTransform.SetParent(holdPoint, true);
        }

        heldTransform.localPosition = heldLocalPosition;
        if (!heldObjectCanRotate)
        {
            heldTransform.localRotation = heldLocalRotation;
        }
    }

    private bool WasPickupPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        Key key = pickupKey switch
        {
            KeyCode.E => Key.E,
            KeyCode.Q => Key.Q,
            KeyCode.F => Key.F,
            KeyCode.Space => Key.Space,
            _ => Key.E
        };

        return Keyboard.current[key].wasPressedThisFrame;
#else
        return Input.GetKeyDown(pickupKey);
#endif
    }

    private bool WasThrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Key key = throwKey switch
        {
            KeyCode.E => Key.E,
            KeyCode.Q => Key.Q,
            KeyCode.F => Key.F,
            KeyCode.Space => Key.Space,
            _ => Key.F
        };

        return (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame) ||
               (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(throwKey) || Input.GetMouseButtonDown(0);
#endif
    }

    private void TryPickup()
    {
        if (holdPoint == null)
        {
            Debug.LogWarning("Assign a hold point before picking up objects.");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, ~0, QueryTriggerInteraction.Collide);
        GameObject closestObject = null;
        Transform closestGripPoint = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            GameObject candidate = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;
            BananaPickup banana = candidate.GetComponent<BananaPickup>();
            if (banana == null)
                banana = hit.GetComponentInParent<BananaPickup>();

            bool isPickableLayer = pickableLayer >= 0
                && (hit.gameObject.layer == pickableLayer || candidate.layer == pickableLayer);
            if (banana == null && GetRedChair(candidate) == null && !isPickableLayer)
                continue;

            if (banana != null)
                candidate = banana.gameObject;

            Transform gripPoint = candidate.GetComponent<BananaDetector>() != null
                ? hit.transform
                : null;
            float distance = (hit.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObject = candidate;
                closestGripPoint = gripPoint;
            }
        }

        if (closestObject != null)
        {
            bool keepPlayerAcrossScenes = closestObject.GetComponent<BananaPickup>() != null && NPC.IsTaskActive(0);
            PickupObject(closestObject, closestGripPoint);
            if (keepPlayerAcrossScenes)
            {
                persistentPlayer = this;
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }
    }

    private void PickupObject(GameObject obj, Transform gripPoint = null)
    {
        bool isRedChair = GetRedChair(obj) != null;
        heldObject = obj;
        heldObjectCanRotate = obj.GetComponent<BananaPickup>() != null;
        playerColliders = GetComponentsInChildren<Collider>(true);
        heldObjectColliders = obj.GetComponentsInChildren<Collider>(true);
        SetHeldObjectCollisionIgnored(true);

        Vector3 gripLocalPosition = Vector3.zero;
        Quaternion gripLocalRotation = Quaternion.identity;
        if (gripPoint != null)
        {
            gripLocalPosition = obj.transform.InverseTransformPoint(gripPoint.position);
            gripLocalRotation = Quaternion.Inverse(obj.transform.rotation) * gripPoint.rotation;
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        obj.transform.SetParent(holdPoint, true);
        heldLocalRotation = Quaternion.Inverse(gripLocalRotation);
        heldLocalPosition = -(heldLocalRotation * Vector3.Scale(gripLocalPosition, obj.transform.localScale));

        obj.transform.localPosition = heldLocalPosition;
        obj.transform.localRotation = heldLocalRotation;

        BananaPickup banana = obj.GetComponent<BananaPickup>();
        if (banana != null)
            banana.RegisterPickup();

        REDCHAIR redChair = GetRedChair(obj);
        if (redChair != null)
            redChair.MarkPickedUp(transform);
    }

    public bool TryPickupObject(GameObject obj, bool keepPlayerAcrossScenes = false)
    {
        if (obj == null || heldObject != null || holdPoint == null)
            return false;

        if (pickableLayer >= 0)
        {
            obj.layer = pickableLayer;
        }

        PickupObject(obj);
        if (keepPlayerAcrossScenes)
        {
            persistentPlayer = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }

        return true;
    }

    public bool ForcePickupObject(GameObject obj, bool keepPlayerAcrossScenes = false)
    {
        if (obj == null || holdPoint == null)
            return false;

        if (heldObject != null && heldObject != obj)
        {
            DropObject();
        }

        return TryPickupObject(obj, keepPlayerAcrossScenes);
    }

    private void DropObject()
    {
        if (heldObject == null) return;

        heldObject.transform.SetParent(null, true);
        SetHeldObjectCollisionIgnored(false);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            Rigidbody playerRigidbody = GetComponent<Rigidbody>();
            if (playerRigidbody != null)
                rb.linearVelocity = playerRigidbody.linearVelocity;
        }

        heldObject = null;
    }

    public void ReleaseBananaForQuestReset()
    {
        if (heldObject != null && heldObject.GetComponent<BananaPickup>() != null)
        {
            DropObject();
        }
    }

    public void ThrowHeldObject()
    {
        if (heldObject == null || holdPoint == null)
            return;

        RedChairQuest legacyChair = heldObject.GetComponent<RedChairQuest>();
        if (legacyChair != null)
            legacyChair.MarkThrown();

        REDCHAIR redChair = GetRedChair(heldObject);
        float appliedThrowForce = throwForce;
        if (redChair != null)
        {
            redChair.MarkThrown(transform);
            appliedThrowForce = redChair.GetThrowForce();
        }

        heldObject.transform.SetParent(null, true);
        SetHeldObjectCollisionIgnored(false);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Rigidbody playerRigidbody = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = playerRigidbody != null ? playerRigidbody.linearVelocity : Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            PlayerMovement playerMovement = GetComponent<PlayerMovement>();
            Transform directionSource = playerMovement != null && playerMovement.cameraTransform != null
                ? playerMovement.cameraTransform
                : aimCamera != null ? aimCamera : Camera.main != null ? Camera.main.transform : holdPoint;

            rb.AddForce(directionSource.forward.normalized * appliedThrowForce, ForceMode.Impulse);
        }

        heldObject = null;
    }

    private void SetHeldObjectCollisionIgnored(bool ignore)
    {
        if (playerColliders == null || heldObjectColliders == null)
            return;

        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null)
                continue;

            foreach (Collider heldCollider in heldObjectColliders)
            {
                if (heldCollider != null)
                    Physics.IgnoreCollision(playerCollider, heldCollider, ignore);
            }
        }

        if (!ignore)
        {
            playerColliders = null;
            heldObjectColliders = null;
        }
    }
}
