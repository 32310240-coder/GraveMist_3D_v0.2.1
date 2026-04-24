using System;
using UnityEngine;

[Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource seSource;
    public SoundEntry[] sounds;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySE(string key)
    {
        if (seSource == null) return;

        foreach (SoundEntry sound in sounds)
        {
            if (sound.key == key && sound.clip != null)
            {
                seSource.PlayOneShot(sound.clip);
                return;
            }
        }

        Debug.LogWarning($"SE not found: {key}");
    }
    void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        AudioManager.Instance.PlaySE("grave_toss");
    }
}
}