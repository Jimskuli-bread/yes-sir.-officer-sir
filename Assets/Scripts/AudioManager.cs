
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioClip[] sounds;
    public string[] soundNames;

    public AudioClip[] musicTracks;
    public string[] musicNames;

    AudioSource soundSource;
    AudioSource musicSource;

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

        soundSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
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
        if (soundSource != null)
        {
            soundSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }
}



