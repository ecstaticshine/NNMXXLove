using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer Settings")]
    public AudioMixer mainMixer;
    public string bgmParamName = "BGMVolume"; // 믹서에서 노출한 파라미터 이름
    public string seParamName = "SEVolume"; // 믹서에서 노출한 파라미터 이름
    public string voiceParamName = "VoiceVolume"; // 믹서에서 노출한 파라미터 이름

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource seSource;
    public AudioSource voiceSource;

    private Coroutine duckingCoroutine;
    private float defaultBGMVolume = 0f; // 믹서의 기본 볼륨 (0dB)
    private float duckedBGMVolume = -15f; // 덕킹 시 볼륨 (-15dB 정도로 낮춤)

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // --- BGM 재생 ---
    public void PlayBGM(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Sounds/BGM/{clipName}");
        if (clip != null && bgmSource.clip != clip)
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // --- SE 재생 (중첩 가능) ---
    public void PlaySE(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Sounds/SE/{clipName}");
        if (clip != null) seSource.PlayOneShot(clip);
    }

    // --- Voice 재생 (덕킹 포함) ---
    public void PlayVoice(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Voices/{clipName}");
        if (clip != null)
        {
            if (voiceSource.isPlaying) voiceSource.Stop();

            voiceSource.clip = clip;
            voiceSource.Play();

            // 보이스 재생 시 BGM 볼륨 낮추기
            StartDucking(clip.length);
        }
    }

    private void StartDucking(float duration)
    {
        if (duckingCoroutine != null) StopCoroutine(duckingCoroutine);
        duckingCoroutine = StartCoroutine(DuckingRoutine(duration));
    }

    IEnumerator DuckingRoutine(float voiceDuration)
    {
        // 1. 볼륨 서서히 낮추기
        yield return StartCoroutine(LerpMixerVolume(bgmParamName, duckedBGMVolume, 0.2f));

        // 2. 목소리 나오는 동안 대기
        yield return new WaitForSeconds(voiceDuration);

        // 3. 목소리 끝나고 약간의 여운(0.3초) 후 다시 키우기
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(LerpMixerVolume(bgmParamName, defaultBGMVolume, 0.5f));
    }

    // 믹서 파라미터를 부드럽게 보간하는 헬퍼 함수
    IEnumerator LerpMixerVolume(string paramName, float targetValue, float duration)
    {
        float currentTime = 0;
        mainMixer.GetFloat(paramName, out float startValue);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, currentTime / duration);
            mainMixer.SetFloat(paramName, newValue);
            yield return null;
        }
        mainMixer.SetFloat(paramName, targetValue);
    }
}