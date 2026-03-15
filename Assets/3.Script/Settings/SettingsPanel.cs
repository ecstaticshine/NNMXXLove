using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject darkArea; // 클릭하면 세팅 패널이 꺼짐.
    public GameObject settingPanel; // 세팅 패널

    [Header("탭")]
    public GameObject audioTabContent;      // 오디오 UI 전체를 묶은 오브젝트
    public GameObject accountTabContent;    // 계정 UI 전체를 묶은 오브젝트
    public Button audioTabButton;
    public Button accountTabButton;

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

    [Header("계정 연동")]
    public TextMeshProUGUI loginStatusText;  // 로그인 상태 표시 텍스트
    public Button guestLoginButton;          // 게스트 로그인 버튼
    public Button logoutButton;             // 로그아웃 버튼
    public Button googleLoginButton;        // 구글 로그인 버튼 (비활성)

    [Header("이름 변경")]
    public TMP_InputField playerNameInput;
    public Button confirmNameButton;

    [Header("언어 변경 팝업")]
    public Button restartConfirmButton;  // 네 버튼
    public Button restartCancelButton;   // 아니오 버튼

    [Header("언어 표시")]
    public TextMeshProUGUI currentLanguageText;

    [Header("리셋 팝업")]
    public GameObject resetPopup;         // "정말 초기화하시겠습니까?" 팝업
    public Button resetConfirmButton;     // 확인 버튼
    public Button resetCancelButton;      // 취소 버튼

    private DataManager.Language pendingLanguage;

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

        // 패널 열릴 때 항상 오디오 탭 먼저
        ShowTab(0);

        // 계정 상태 갱신 이벤트 등록
        AuthManager.OnAuthStateChanged += RefreshAccountUI;
        RefreshAccountUI();
        RefreshLanguageUI();

        if (playerNameInput != null && DataManager.Instance?.userData != null)
            playerNameInput.text = DataManager.Instance.userData.playerName;

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

    private void OnDisable()
    {
        AuthManager.OnAuthStateChanged -= RefreshAccountUI;
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

        //계정 이름 변경
        confirmNameButton.onClick.AddListener(OnConfirmNameClicked);

        // 탭 버튼
        audioTabButton.onClick.AddListener(() => ShowTab(0));
        accountTabButton.onClick.AddListener(() => ShowTab(1));

        //언어 버튼 설정
        btnKorean.onClick.AddListener(OnClickKorean);
        btnJapanese.onClick.AddListener(OnClickJapanese);

        // 언어 변경 확인 설정
        restartConfirmButton.onClick.AddListener(() =>
        {
            restartPopup.SetActive(false);
            PlayerPrefs.SetInt("Language", (int)pendingLanguage);
            PlayerPrefs.Save();
            DataManager.Instance.ChangeLanguage(pendingLanguage);
            AuthManager.Instance.RestartToTitleForLanguage();
        });

        restartCancelButton.onClick.AddListener(() =>
        {
            restartPopup.SetActive(false);
            RefreshLanguageUI(); // 버튼 상태 원복
        });

        // 계정 버튼
        guestLoginButton.onClick.AddListener(OnGuestLoginClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);

        // 계정 리셋
        resetConfirmButton.onClick.AddListener(OnResetConfirmed);
        resetCancelButton.onClick.AddListener(() => resetPopup.SetActive(false));

        if (googleLoginButton != null)
        {
            googleLoginButton.interactable = false;
            googleLoginButton.GetComponentInChildren<TextMeshProUGUI>().text = "구글 로그인 (준비중)";
        }
    }


    // ── 탭 ──────────────────────────────────────────
    private void ShowTab(int index)
    {
        audioTabContent.SetActive(index == 0);
        accountTabContent.SetActive(index == 1);
    }

    // ── 계정 ──────────────────────────────────────────

    private void RefreshAccountUI()
    {
        if (AuthManager.Instance == null) return;

        loginStatusText.text = AuthManager.Instance.GetLoginStatusText();

        bool isLoggedIn = AuthManager.Instance.IsLoggedIn;
        guestLoginButton.gameObject.SetActive(!isLoggedIn);
        logoutButton.gameObject.SetActive(isLoggedIn);
    }

    private async void OnGuestLoginClicked()
    {
        guestLoginButton.interactable = false;
        await AuthManager.Instance.LoginAsGuest();
        guestLoginButton.interactable = true;
    }

    private void OnLogoutClicked()
    {
        resetPopup.SetActive(true);
    }

    private async void OnResetConfirmed()
    {
        resetPopup.SetActive(false);
        await AuthManager.Instance.DeleteAndReset();
    }

    private async void OnConfirmNameClicked()
    {
        if (string.IsNullOrWhiteSpace(playerNameInput.text)) return;

        DataManager.Instance.userData.playerName = playerNameInput.text;
        DataManager.Instance.SaveData();
        await AuthManager.Instance.SaveUserDataToFirestore();

        // 홈 UI에 이름 즉시 반영
        DataManager.Instance.OnDataChanged?.Invoke();
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
    }

    // ── 언어 ──────────────────────────────────────────

    private void RefreshLanguageUI()
    {
        bool isKorean = DataManager.Instance.currentLanguage == DataManager.Language.KO;
        btnKorean.interactable = !isKorean;
        btnJapanese.interactable = isKorean;

        if (currentLanguageText != null)
            currentLanguageText.text = DataManager.Instance.GetLocalizedText(
                isKorean ? "UI_LANGUAGE_CURRENT_KO" : "UI_LANGUAGE_CURRENT_JP");
    }

    public void OnClickKorean()
    {
        pendingLanguage = DataManager.Language.KO;
        RefreshLanguageUI();
        restartPopup.SetActive(true);
    }

    public void OnClickJapanese()
    {
        pendingLanguage = DataManager.Language.JP;
        RefreshLanguageUI();
        restartPopup.SetActive(true);
    }

    // ── 닫기 ──────────────────────────────────────────

    public void OnClickClose()
    {
        PlayerPrefs.Save(); // 볼륨 최종 저장
        gameObject.SetActive(false);
    }


}