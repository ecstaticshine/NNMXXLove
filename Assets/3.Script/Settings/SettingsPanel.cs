using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject darkArea; // 클릭하면 세팅 패널이 꺼짐.
    public GameObject settingPanel; // 세팅 패널

    [Header("Volume Sliders")]
    public Slider bgmSlider;
    public Slider seSlider;
    public Slider voiceSlider;
    public Slider masterSlider;

    [Header("Mute Toggles")]
    public Toggle masterMuteToggle;
    public Toggle bgmMuteToggle;
    public Toggle seMuteToggle;
    public Toggle voiceMuteToggle;

    [Header("Language Buttons")]
    public Button btnKorean;
    public Button btnJapanese;

    [Header("Restart Popup")]
    public GameObject restartPopup; // "재시작 후 적용됩니다" 팝업

    private const string KEY_BGM = "Vol_BGM";
    private const string KEY_SE = "Vol_SE";
    private const string KEY_VOICE = "Vol_Voice";
    private const string KEY_MASTER = "Vol_Master";
    private const string KEY_MUTE_MASTER = "Mute_Master";
    private const string KEY_MUTE_BGM = "Mute_BGM";
    private const string KEY_MUTE_SE = "Mute_SE";
    private const string KEY_MUTE_VOICE = "Mute_Voice";

    private void OnEnable()
    {
        LoadVolumeSettings();
        LoadMuteSettings();

        // DarkArea 클릭 시 닫기 연결
        if (darkArea != null)
        {
            Button darkBtn = darkArea.GetComponent<Button>();
            if (darkBtn != null)
            {
                darkBtn.onClick.RemoveAllListeners();
                darkBtn.onClick.AddListener(OnClickClose);
            }
        }
    }

    private void Start()
    {
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        seSlider.onValueChanged.AddListener(OnSEChanged);
        voiceSlider.onValueChanged.AddListener(OnVoiceChanged);
        masterSlider.onValueChanged.AddListener(OnMasterChanged);

        masterMuteToggle.onValueChanged.AddListener(val => OnMuteChanged(val, AudioManager.Instance.masterParamName, KEY_MUTE_MASTER));
        bgmMuteToggle.onValueChanged.AddListener(val => OnMuteChanged(val, AudioManager.Instance.bgmParamName, KEY_MUTE_BGM));
        seMuteToggle.onValueChanged.AddListener(val => OnMuteChanged(val, AudioManager.Instance.seParamName, KEY_MUTE_SE));
        voiceMuteToggle.onValueChanged.AddListener(val => OnMuteChanged(val, AudioManager.Instance.voiceParamName, KEY_MUTE_VOICE));
    }

    // ── 볼륨 ──────────────────────────────────────────

    private void LoadVolumeSettings()
    {
        float bgm = PlayerPrefs.GetFloat(KEY_BGM, 0.8f);
        float se = PlayerPrefs.GetFloat(KEY_SE, 0.8f);
        float voice = PlayerPrefs.GetFloat(KEY_VOICE, 0.8f);
        float master = PlayerPrefs.GetFloat(KEY_MASTER, 0.8f);

        bgmSlider.SetValueWithoutNotify(bgm);
        seSlider.SetValueWithoutNotify(se);
        voiceSlider.SetValueWithoutNotify(voice);
        masterSlider.SetValueWithoutNotify(master);

        ApplyVolume(bgm, se, voice, master);
    }

    private void OnMuteChanged(bool isMuted, string paramName, string prefKey)
    {
        ApplyMute(paramName, isMuted);
        PlayerPrefs.SetInt(prefKey, isMuted ? 1 : 0);
    }

    private void LoadMuteSettings()
    {
        masterMuteToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KEY_MUTE_MASTER, 0) == 1);
        bgmMuteToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KEY_MUTE_BGM, 0) == 1);
        seMuteToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KEY_MUTE_SE, 0) == 1);
        voiceMuteToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KEY_MUTE_VOICE, 0) == 1);

        ApplyMute(AudioManager.Instance.masterParamName, masterMuteToggle.isOn);
        ApplyMute(AudioManager.Instance.bgmParamName, bgmMuteToggle.isOn);
        ApplyMute(AudioManager.Instance.seParamName, seMuteToggle.isOn);
        ApplyMute(AudioManager.Instance.voiceParamName, voiceMuteToggle.isOn);
    }

    private void ApplyVolume(float bgm, float se, float voice, float master)
    {
        // 슬라이더 0~1 → 믹서 dB 변환 (-40 ~ 0)
        AudioManager.Instance.mainMixer.SetFloat(
            AudioManager.Instance.bgmParamName, Mathf.Log10(Mathf.Max(bgm, 0.0001f)) * 20f);
        AudioManager.Instance.mainMixer.SetFloat(
            AudioManager.Instance.seParamName, Mathf.Log10(Mathf.Max(se, 0.0001f)) * 20f);
        AudioManager.Instance.mainMixer.SetFloat(
            AudioManager.Instance.voiceParamName, Mathf.Log10(Mathf.Max(voice, 0.0001f)) * 20f);
        AudioManager.Instance.mainMixer.SetFloat(
            AudioManager.Instance.masterParamName, Mathf.Log10(Mathf.Max(master, 0.0001f)) * 20f);
    }

    private void ApplyMute(string paramName, bool isMuted)
    {
        // 뮤트 시 -80dB (사실상 무음), 해제 시 원래 슬라이더 값으로 복원
        AudioManager.Instance.mainMixer.SetFloat(paramName, isMuted ? -80f : GetCurrentDb(paramName));
    }

    private float GetCurrentDb(string paramName)
    {
        // 파라미터 이름으로 현재 슬라이더 값을 찾아서 dB로 변환
        float sliderVal = 0.8f;
        if (paramName == AudioManager.Instance.masterParamName) sliderVal = masterSlider.value;
        else if (paramName == AudioManager.Instance.bgmParamName) sliderVal = bgmSlider.value;
        else if (paramName == AudioManager.Instance.seParamName) sliderVal = seSlider.value;
        else if (paramName == AudioManager.Instance.voiceParamName) sliderVal = voiceSlider.value;

        return Mathf.Log10(Mathf.Max(sliderVal, 0.0001f)) * 20f;
    }

    private void OnBGMChanged(float val)
    {
        AudioManager.Instance.mainMixer.SetFloat(
            AudioManager.Instance.bgmParamName, Mathf.Log10(Mathf.Max(val, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat(KEY_BGM, val);
    }

    private void OnSEChanged(float val)
    {
        AudioManager.Instance.mainMixer.SetFloat(
            AudioManager.Instance.seParamName, Mathf.Log10(Mathf.Max(val, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat(KEY_SE, val);
    }

    private void OnVoiceChanged(float val)
    {
        AudioManager.Instance.mainMixer.SetFloat(
            AudioManager.Instance.voiceParamName, Mathf.Log10(Mathf.Max(val, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat(KEY_VOICE, val);
    }

    private void OnMasterChanged(float val)
    {
        AudioManager.Instance.mainMixer.SetFloat(
                AudioManager.Instance.masterParamName,
                Mathf.Log10(Mathf.Max(val, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat(KEY_MASTER, val);

        // 시각적으로 비례해서 슬라이더 움직이기
        bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_BGM, 0.8f) * val);
        seSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_SE, 0.8f) * val);
        voiceSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_VOICE, 0.8f) * val);
    }

    // ── 언어 ──────────────────────────────────────────

    public void OnClickKorean()
    {
        PlayerPrefs.SetInt("Language", (int)DataManager.Language.KO);
        PlayerPrefs.Save();
        restartPopup.SetActive(true);
    }

    public void OnClickJapanese()
    {
        PlayerPrefs.SetInt("Language", (int)DataManager.Language.JP);
        PlayerPrefs.Save();
        restartPopup.SetActive(true);
    }

    // ── 닫기 ──────────────────────────────────────────

    public void OnClickClose()
    {
        PlayerPrefs.Save(); // 볼륨 최종 저장
        gameObject.SetActive(false);
    }


}