using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class GameMenu : MonoBehaviour

{

    [SerializeField] AudioClip clickSound;
    int lastClickFrame = -1;

    void Start()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(PlayClickSound);
        }
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
        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopMusic();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    }
public void QuitGame()

    {

        PlayClickSound();

        if (clickSound != null)
        {
            Invoke(nameof(QuitApplication), clickSound.length);
            return;
        }

        QuitApplication();

    }
}