using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    private bool isPaused;
    private bool cursorUnlockedForUi;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private Sprite pauseBackgroundColor;
    private static Pause instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterPauseMenu()
    {
        instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePauseMenuForStartingScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            OnSceneLoaded(scene, LoadSceneMode.Single);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
        {
            if (instance != null)
            {
                instance.SetPaused(false);
                Destroy(instance.gameObject);
                instance = null;
            }

            return;
        }

        if (instance == null)
        {
            new GameObject("Pause Menu").AddComponent<Pause>();
        }
        else
        {
            instance.SetPaused(false);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (InspectaBanana.IsEnding)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPaused(!isPaused);
            return;
        }

        if (!isPaused && Input.GetKeyDown(KeyCode.Tab))
        {
            cursorUnlockedForUi = !cursorUnlockedForUi;
            Cursor.lockState = cursorUnlockedForUi ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = cursorUnlockedForUi;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (!isPaused)
        {
            return;
        }

        Color previousGuiColor = GUI.color;
        Color previousBackgroundColor = GUI.backgroundColor;
        GUI.color = Color.white;
        GUI.backgroundColor = Color.white;

        float panelWidth = Mathf.Min(760f, Screen.width - 32f);
        float panelHeight = Mathf.Min(460f, Screen.height - 32f);
        Rect panel = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUI.color = new Color(0.16f, 0.16f, 0.16f, 1f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
        GUI.color = Color.white;
        float actionsWidth = Mathf.Min(360f, panel.width - 48f);
        float actionsX = panel.x + (panel.width - actionsWidth) * 0.5f;
        float buttonHeight = 48f;
        float buttonSpacing = 10f;
        float buttonsY = panel.y + 124f;

        Rect resumeRect = new Rect(actionsX, buttonsY, actionsWidth, buttonHeight);
        Rect restartRect = new Rect(actionsX, buttonsY + buttonHeight + buttonSpacing, actionsWidth, buttonHeight);
        Rect menuRect = new Rect(actionsX, buttonsY + (buttonHeight + buttonSpacing) * 2f, actionsWidth, buttonHeight);
        if (pauseBackgroundColor != null)
        {
            GUI.DrawTexture(panel, pauseBackgroundColor.texture, ScaleMode.ScaleToFit, true);
        }

        GUI.Label(new Rect(panel.x, panel.y + 48f, panel.width, 54f), "PAUSED", titleStyle);

        if (GUI.Button(resumeRect, "Resume", buttonStyle))
        {
            SetPaused(false);
        }

        if (GUI.Button(restartRect, "Restart level", buttonStyle))
        {
            SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (GUI.Button(menuRect, "Main menu", buttonStyle))
        {
            SetPaused(false);
            SceneManager.LoadScene("Menu");
        }

        GUI.color = previousGuiColor;
        GUI.backgroundColor = previousBackgroundColor;
    }

    private void OnDisable()
    {
        SetPaused(false);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        cursorUnlockedForUi = false;
        Time.timeScale = paused ? 0f : 1f;
        bool cursorUnlocked = paused || SceneManager.GetActiveScene().name == "Menu";
        Cursor.lockState = cursorUnlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = cursorUnlocked;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null && buttonStyle != null)
        {
            return;
        }

        pauseBackgroundColor = Resources.Load<Sprite>("Pause/PauseColorLabel");
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        Texture2D buttonTexture = MakeTexture(new Color(0.3f, 0.3f, 0.3f, 1f));
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                background = buttonTexture,
                textColor = Color.white
            },
            hover =
            {
                background = buttonTexture,
                textColor = Color.white
            },
            active =
            {
                background = buttonTexture,
                textColor = Color.white
            },
            focused =
            {
                background = buttonTexture,
                textColor = Color.white
            },
            onNormal =
            {
                background = buttonTexture,
                textColor = Color.white
            },
            onHover =
            {
                background = buttonTexture,
                textColor = Color.white
            },
            onActive =
            {
                background = buttonTexture,
                textColor = Color.white
            },
            onFocused =
            {
                background = buttonTexture,
                textColor = Color.white
            }
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
