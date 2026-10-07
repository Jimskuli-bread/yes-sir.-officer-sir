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
    private int pickableLayer;
    private static PlayerPickup persistentPlayer;
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
        EnsureSingleAudioListener();
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
        if (WasPickupPressed())
        {
            if (heldObject == null)
                TryPickup();
            else
            {
                NPC.TryDeliverRedChair(heldObject);
                DropObject();
            }
        }

        if (heldObject != null && WasThrowPressed())
        {
            ThrowHeldObject();
        }
    }

    private void LateUpdate()
    {
        if (heldObject == null || holdPoint == null)
            return;

        Transform heldTransform = heldObject.transform;
        if (heldTransform.parent != holdPoint)
            heldTransform.SetParent(holdPoint, false);

        heldTransform.localPosition = Vector3.zero;
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
        if (Keyboard.current == null)
            return false;

        Key key = throwKey switch
        {
            KeyCode.E => Key.E,
            KeyCode.Q => Key.Q,
            KeyCode.F => Key.F,
            KeyCode.Space => Key.Space,
            _ => Key.F
        };

        return Keyboard.current[key].wasPressedThisFrame;
#else
        return Input.GetKeyDown(throwKey);
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
            if (banana == null && !isPickableLayer)
                continue;

            if (banana != null)
                candidate = banana.gameObject;

            float distance = (hit.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObject = candidate;
            }
        }

        if (closestObject != null)
            PickupObject(closestObject);
    }

    private void PickupObject(GameObject obj)
    {
        heldObject = obj;
        playerColliders = GetComponentsInChildren<Collider>(true);
        heldObjectColliders = obj.GetComponentsInChildren<Collider>(true);
        SetHeldObjectCollisionIgnored(true);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        obj.transform.SetParent(holdPoint, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        BananaPickup banana = obj.GetComponent<BananaPickup>();
        if (banana != null)
            banana.RegisterPickup();
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

        RedChairQuest redChair = heldObject.GetComponent<RedChairQuest>();
        if (redChair != null)
        {
            redChair.MarkThrown();
        }

        heldObject.transform.SetParent(null, true);
        SetHeldObjectCollisionIgnored(false);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Rigidbody playerRigidbody = GetComponent<Rigidbody>();
            rb.linearVelocity = playerRigidbody != null ? playerRigidbody.linearVelocity : Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;

            PlayerMovement playerMovement = GetComponent<PlayerMovement>();
            Transform directionSource = playerMovement != null && playerMovement.cameraTransform != null
                ? playerMovement.cameraTransform
                : aimCamera != null ? aimCamera : Camera.main != null ? Camera.main.transform : holdPoint;

            rb.AddForce(directionSource.forward.normalized * throwForce, ForceMode.Impulse);
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
