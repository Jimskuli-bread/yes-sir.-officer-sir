using UnityEngine;

public class DestroyPlayerOnLoad : MonoBehaviour
{
    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            Destroy(player);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
