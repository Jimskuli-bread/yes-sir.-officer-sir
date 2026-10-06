
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    const string soundVolumeKey = "SoundVolume";
    const string musicVolumeKey = "MusicVolume";

    public static AudioManager instance;

    public AudioClip[] sounds;
    public string[] soundNames;

    public AudioClip[] musicTracks;
    public string[] musicNames;
    public TMP_Text soundVolumeLabel;
    public TMP_Text musicVolumeLabel;

    AudioSource soundSource;
    AudioSource musicSource;
    AudioClip buttonClickClip;
    readonly System.Collections.Generic.HashSet<Button> wiredButtons = new();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AudioSource[] audioSources = GetComponents<AudioSource>();
        musicSource = audioSources.Length > 0 ? audioSources[0] : gameObject.AddComponent<AudioSource>();
        soundSource = audioSources.Length > 1 ? audioSources[1] : gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        SetSoundVolume(PlayerPrefs.GetFloat(soundVolumeKey, 1f));
        SetMusicVolume(PlayerPrefs.GetFloat(musicVolumeKey, 1f));
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            if (!button.gameObject.scene.IsValid() || button.gameObject.scene != scene)
            {
                continue;
            }

            AudioSource buttonAudioSource = button.GetComponent<AudioSource>();
            if (buttonAudioSource != null && buttonAudioSource.clip != null)
            {
                buttonAudioSource.playOnAwake = false;
                buttonAudioSource.Stop();
                buttonClickClip ??= buttonAudioSource.clip;
            }
        }

        if (buttonClickClip == null)
        {
            return;
        }

        foreach (Button button in buttons)
        {
            if (button.gameObject.scene == scene && wiredButtons.Add(button))
            {
                button.onClick.AddListener(PlayButtonClick);
            }
        }
    }

    void PlayButtonClick()
    {
        PlaySound(buttonClickClip);
    }

    public void PlaySound(string soundName)
    {
        if (sounds == null || soundNames == null || soundName == null || soundSource == null)
        {
            return;
        }

        for (int i = 0; i < soundNames.Length; i++)
        {
            if (soundNames[i] == soundName && i < sounds.Length && sounds[i] != null)
            {
                soundSource.PlayOneShot(sounds[i]);
                return;
            }
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && soundSource != null)
        {
            soundSource.PlayOneShot(clip);
        }
    }

    public void PlayMusic(string musicName)
    {
        if (musicTracks == null || musicNames == null || musicName == null || musicSource == null)
        {
            return;
        }

        for (int i = 0; i < musicNames.Length; i++)
        {
            if (musicNames[i] == musicName && i < musicTracks.Length && musicTracks[i] != null)
            {
                if (musicSource.clip == musicTracks[i] && musicSource.isPlaying)
                {
                    return;
                }

                musicSource.Stop();
                musicSource.clip = musicTracks[i];
                musicSource.Play();
                return;
            }
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    public void SetSoundVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (soundSource != null)
        {
            soundSource.volume = volume;
        }

        PlayerPrefs.SetFloat(soundVolumeKey, volume);
        if (soundVolumeLabel != null)
        {
            soundVolumeLabel.text = $"SOUND {Mathf.RoundToInt(volume * 100f)}%";
        }
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }

        PlayerPrefs.SetFloat(musicVolumeKey, volume);
        if (musicVolumeLabel != null)
        {
            musicVolumeLabel.text = $"Music {Mathf.RoundToInt(volume * 100f)}%";
        }
    }
}



