using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    [Header("BGM")]
    public AudioSource bgmSource;

    public AudioClip bgmFanfare;
    public AudioClip bgmMainMenu;
    public AudioClip bgmBattleIntro;
    public AudioClip bgmBattleMain;

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
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }
    public void PlayBattleBGM()
    {
        StopAllCoroutines();
        StartCoroutine(PlayBattleSequence());
    }

    IEnumerator PlayBattleSequence()
    {
        if (bgmSource == null) yield break;

        // ① Intro再生（ループなし）
        bgmSource.clip = bgmBattleIntro;
        bgmSource.loop = false;
        bgmSource.Play();

        // ② Intro終わるまで待つ
        yield return new WaitForSeconds(bgmBattleIntro.length);

        // ③ Mainループ開始
        bgmSource.clip = bgmBattleMain;
        bgmSource.loop = true;
        bgmSource.Play();
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
    public void PlaySEClip(AudioClip clip)
    {
        if (seSource == null || clip == null) return;
        seSource.PlayOneShot(clip);
    }
    void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        AudioManager.Instance.PlaySE("grave_toss");
    }
}
}