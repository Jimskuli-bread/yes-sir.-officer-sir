using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    private bool isPaused;
    private bool cursorUnlockedForUi;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private Sprite pauseBackgroundGrey;
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

        float panelWidth = Mathf.Min(760f, Screen.width - 32f);
        float panelHeight = Mathf.Min(460f, Screen.height - 32f);
        Rect panel = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUI.Box(panel, GUIContent.none, panelStyle);
        float actionsX = panel.x + panel.width * 0.55f;
        float actionsWidth = panel.xMax - actionsX - 24f;
        float buttonHeight = 48f;
        float buttonSpacing = 10f;
        float buttonsY = panel.y + 124f;

        Rect resumeRect = new Rect(actionsX, buttonsY, actionsWidth, buttonHeight);
        Rect restartRect = new Rect(actionsX, buttonsY + buttonHeight + buttonSpacing, actionsWidth, buttonHeight);
        Rect menuRect = new Rect(actionsX, buttonsY + (buttonHeight + buttonSpacing) * 2f, actionsWidth, buttonHeight);
        Sprite background = pauseBackgroundColor != null ? pauseBackgroundColor : pauseBackgroundGrey;

        if (background != null)
        {
            GUI.DrawTexture(panel, background.texture, ScaleMode.ScaleToFit, true);
        }

        GUI.Label(new Rect(actionsX, panel.y + 48f, actionsWidth, 54f), "PAUSED", titleStyle);

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
        if (panelStyle != null)
        {
            return;
        }

        pauseBackgroundGrey = Resources.Load<Sprite>("Pause/PauseGreyLabel");
        pauseBackgroundColor = Resources.Load<Sprite>("Pause/PauseColorLabel");

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0.08f, 0.07f, 0.07f, 0.98f)) },
            border = new RectOffset(0, 0, 0, 0)
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.91f, 0.76f, 0.43f) }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                background = MakeTexture(new Color(0.48f, 0.08f, 0.1f)),
                textColor = Color.white
            },
            hover =
            {
                background = MakeTexture(new Color(0.83f, 0.68f, 0.39f)),
                textColor = new Color(0.14f, 0.1f, 0.08f)
            },
            active =
            {
                background = MakeTexture(new Color(0.83f, 0.68f, 0.39f)),
                textColor = new Color(0.14f, 0.1f, 0.08f)
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
