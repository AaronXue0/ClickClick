using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioSystem;

namespace AudioSystem
{
    public enum PlaySoundMode
    {
        ReplayIfExisted,
        IgnoreIfExisted,
        AllowMultiple
    }
}

[RequireComponent(typeof(AudioSource))]
public class AudioManager : SingletonManager<AudioManager>
{
    [SerializeField]
    private ClipAssetLibrary asset;

    [SerializeField]
    private AudioSource bgmSource;
    private List<Sound> soundList = new List<Sound>();
    private AudioData audioSettings = new AudioData();

    public float bgmVolume = 1f;

    private bool IsMuteBgm
    {
        get { return audioSettings.isMuteBgm; }
    }
    private bool IsMuteSound
    {
        get { return audioSettings.isMuteSound; }
    }
    public float MusicVolume
    {
        get { return audioSettings.musicVolume; }
    }
    public float SoundVolume
    {
        get { return audioSettings.soundVolume; }
    }

    protected override void Awake()
    {
        base.Awake();
        LoadAudioSettings();
        UpdateAudioSettings();
        StartCoroutine(FinishDetect());
    }

    public static float GetBGMTime
    {
        get { return Instance.bgmSource.time; }
    }

    public static void SetAudioSettings(AudioData audioData)
    {
        Instance.audioSettings = audioData;
        Instance.UpdateAudioSettings();
    }

    public static void UpdateMusicVolume(float volume)
    {
        Instance.audioSettings.musicVolume = volume;
        Instance.UpdateAudioSettings();

        // DataManager.MusicVolume = volume;
        // DataManager.SaveSettingData();
    }

    public static void UpdateSoundVolume(float volume)
    {
        Instance.audioSettings.soundVolume = volume;
        Instance.UpdateAudioSettings();

        // DataManager.SoundVolume = volume;
        // DataManager.SaveSettingData();
    }

    private void LoadAudioSettings()
    {
        audioSettings.musicVolume = bgmVolume;//DataManager.MusicVolume;
        audioSettings.soundVolume = 1f;//DataManager.SoundVolume;
    }

    private void UpdateAudioSettings()
    {
        bgmSource.mute = IsMuteBgm;
        bgmSource.volume = MusicVolume;
        for (int i = 0; i < soundList.Count; i++)
        {
            soundList[i].Mute = IsMuteSound;
            soundList[i].Volume = SoundVolume;
        }
    }

    #region Sound
    public static void PlaySound(
        string groupName,
        string clipName,
        bool loop = false,
        PlaySoundMode playMode = PlaySoundMode.ReplayIfExisted,
        float timing = 0,
        bool rndPitch = false,
        Vector2 rndRange = new Vector2()
    )
    {
        ClipAsset clipAsset = Instance.asset[groupName][clipName];
        Instance.PlaySound(clipAsset.clipName, clipAsset.clip, loop, playMode, timing, rndPitch);
    }

    public static void PlaySound(
        AudioClip clip,
        bool loop = false,
        PlaySoundMode playMode = PlaySoundMode.ReplayIfExisted,
        float timing = 0,
        bool rndPitch = false,
        Vector2 rndRange = new Vector2()
    )
    {
        Instance.PlaySound(clip.name, clip, loop, playMode, timing, rndPitch, rndRange);
    }

    private void PlaySound(
        string soundName,
        AudioClip clip,
        bool loop,
        PlaySoundMode playMode,
        float timing = 0,
        bool rndPitch = false,
        Vector2 rndRange = new Vector2()
    )
    {
        if (clip == null)
            return;

        switch (playMode)
        {
            case PlaySoundMode.ReplayIfExisted:
                RemoveSound(FindSoundIndex(soundName));
                break;
            case PlaySoundMode.IgnoreIfExisted:
                if (IsSoundExist(soundName))
                    return;
                break;
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.time = timing;
        if (rndPitch)
        {
            source.pitch = 1 + Random.Range(rndRange.x, rndRange.y);
        }
        else
        {
            source.pitch = 1;
        }

        soundList.Add(new Sound(soundName, clip, source, IsMuteSound, SoundVolume, loop));
    }

    public static void StopSound(string soundName)
    {
        Instance.RemoveSound(Instance.FindSoundIndex(soundName));
    }

    public static void StopSound(AudioClip clip)
    {
        Instance.RemoveSound(Instance.FindSoundIndex(clip.name));
    }

    public static void StopAllSound()
    {
        for (int i = Instance.soundList.Count - 1; i >= 0; i--)
            Instance.RemoveSound(i);
    }

    private int FindSoundIndex(string soundName)
    {
        return soundList.FindIndex(sound => sound.SoundName == soundName);
    }

    private bool IsSoundExist(string soundName)
    {
        return FindSoundIndex(soundName) != -1;
    }

    private void RemoveSound(int index)
    {
        if (index == -1)
            return;
        soundList[index].DestroySource();
        soundList.RemoveAt(index);
    }

    private void DetectFinishedSound()
    {
        for (int i = soundList.Count - 1; i >= 0; i--)
            if (!soundList[i].Playing)
                RemoveSound(i);
    }

    IEnumerator FinishDetect()
    {
        WaitForSeconds detectInterval = new WaitForSeconds(1);
        while (true)
        {
            yield return detectInterval;
            DetectFinishedSound();
        }
    }
    #endregion

    #region BGM
    public static AudioSource BGM
    {
        get { return Instance.bgmSource; }
    }

    public static void PlayBGM(string groupName, string clipName, bool loop = true)
    {
        Instance.PlayBGM(Instance.asset[groupName][clipName].clip, loop);
    }

    public static void PlayBGM(AudioClip clip, bool loop = true)
    {
        Instance.PlayBGM(clip, loop);
    }

    private void PlayBGM(AudioClip clip, bool loop, bool temp = false)
    {
        if (currentFading != null)
            StopCoroutine(currentFading);

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.mute = Instance.IsMuteBgm;
        bgmSource.volume = MusicVolume;
        bgmSource.Play();
    }

    public static void StopBGM()
    {
        Instance.bgmSource.Stop();
    }

    public static void PauseBGM()
    {
        if (Instance.bgmSource.isPlaying)
            Instance.bgmSource.Pause();
    }

    public static void ResumeBGM(bool fadeIn = true, float fadeInDuration = 1)
    {
        if (!Instance.bgmSource.isPlaying)
        {
            Instance.bgmSource.UnPause();
            if (fadeIn && fadeInDuration > 0)
                Instance.FadeInBgm(fadeInDuration);
        }
    }

    public static void FadeInBgm(float duration = 1)
    {
        if (duration > 0)
            Instance.FadeInBgm(duration);
    }

    public static void FadeOutBgm(float duration = 1)
    {
        if (duration > 0)
            Instance.FadeOutBgm(duration);
    }

    private IEnumerator currentFading = null;

    private void FadeInBgm(float duration, bool temp = false)
    {
        if (currentFading != null)
            StopCoroutine(currentFading);
        currentFading = FadeBgm(0, MusicVolume, duration);
        StartCoroutine(currentFading);
    }

    private void FadeOutBgm(float duration, bool temp = false)
    {
        if (currentFading != null)
            StopCoroutine(currentFading);
        currentFading = FadeBgm(bgmSource.volume, 0, duration);
        StartCoroutine(currentFading);
    }

    private const int updateTimes = 20;

    private IEnumerator FadeBgm(float beginVolume, float endVolume, float duration)
    {
        float volumeInterval = (endVolume - beginVolume) / (float)updateTimes;
        WaitForSeconds fadeInterval = new(duration / (float)updateTimes);

        bgmSource.volume = beginVolume;
        for (int i = 0; i < updateTimes; i++)
        {
            bgmSource.volume += volumeInterval;
            yield return fadeInterval;
        }
        bgmSource.volume = endVolume;
    }
    #endregion
}
