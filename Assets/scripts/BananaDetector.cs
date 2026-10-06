using UnityEngine;
using UnityEngine.InputSystem;

public class BananaDetector : MonoBehaviour
{
    [SerializeField] private KeyCode detectKey = KeyCode.B;
    [SerializeField] private float detectionRadius = 20f;

    private void Update()
    {
        if (!BananaPickup.HasBanana || !NPC.IsTaskActive(8) || !WasDetectPressed())
            return;

        BananaPickup[] bananas = FindObjectsByType<BananaPickup>(FindObjectsSortMode.None);
        foreach (BananaPickup banana in bananas)
        {
            if (banana == null || banana == BananaPickup.FirstBanana)
                continue;

            float distance = Vector3.Distance(transform.position, banana.transform.position);
            if (distance > detectionRadius)
                continue;

            NPC.CompleteTaskInLoadedScenes(8);
            Destroy(banana.gameObject);
            Debug.Log("Banana detector found the first banana.");
            return;
        }
    }

    private bool WasDetectPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(detectKey);
#endif
    }
}