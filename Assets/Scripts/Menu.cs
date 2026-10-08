using System.Collections;

using System.Collections.Generic;

using TMPro;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;



public class GameMenu : MonoBehaviour

{

    [SerializeField] AudioClip clickSound;
    int lastClickFrame = -1;

    void Start()
    {
        EnsureMenuInput();
        if (AudioManager.instance != null)
        {
            AudioManager.instance.BindMenuVolumeControls();
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(PlayClickSound);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null && buttonText.text.Trim() == "Quit" && !HasQuitListener(button))
            {
                button.onClick.AddListener(QuitGame);
            }
        }
    }

    private static bool HasQuitListener(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == nameof(QuitGame))
            {
                return true;
            }
        }

        return false;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private static void EnsureMenuInput()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputModule.AssignDefaultActions();
    }

    void PlayClickSound()
    {
        if (clickSound == null || lastClickFrame == Time.frameCount)
        {
            return;
        }

        lastClickFrame = Time.frameCount;
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySound(clickSound);
        }
        else
        {
            Vector3 audioPosition = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clickSound, audioPosition);
        }
    }

    void QuitApplication()
    {
        Debug.Log("Lopetit pelin.");
        Application.Quit();
    }

    public void PlayGame()

    {

        PlayClickSound();
        SceneManager.LoadScene("Office");

    }
public void QuitGame()

    {

        PlayClickSound();

        if (clickSound != null)
        {
            StartCoroutine(QuitAfterClickSound());
            return;
        }

        QuitApplication();

    }

    private IEnumerator QuitAfterClickSound()
    {
        yield return new WaitForSecondsRealtime(clickSound.length);
        QuitApplication();
    }
}