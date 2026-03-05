using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using TMP_Ruby;

public class DialogueManager : MonoBehaviour
{
    // CSV 컬럼 인덱스 정의 (작성하신 표 순서 기준)
    const int COL_SPEAKER = 0;
    const int COL_EMOTION = 1;
    const int COL_VOICE = 2;
    const int COL_SOUND = 3;
    const int COL_BG = 4;
    const int COL_POS = 5;
    const int COL_KO = 6;
    const int COL_JP = 7;

    [Header("UI Objects")]
    public GameObject dialoguePanel;
    public GameObject speakerPanel;   // 하단 대화창
    public GameObject narrativePanel; // 중앙 나레이션창
    public Image backgroundImage;     // 배경 이미지 전용

    [Header("Texts")]
    public TMP_Text speakerNameText;
    public TextMeshProRuby speakerContentText;
    public TextMeshProRuby narrativeContentText;

    [Header("Character Images")]
    public Image leftCharacterImage;   // 왼쪽 캐릭터 슬롯
    public Image leftCharacterImage2;   // 왼쪽 캐릭터 슬롯
    public Image rightCharacterImage;  // 오른쪽 캐릭터 슬롯
    public Image rightCharacterImage2;  // 오른쪽 캐릭터 슬롯
    public Image centerCharacterImage; // 중앙 캐릭터 슬롯

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    public float moveDuration = 0.5f;

    [Header("Automation Settings")]
    public bool isAutoMode = false;
    public bool isSkipMode = false;
    public float autoDelay = 1.5f; // 대사 종료 후 대기 시간
    public float skipSpeedMultiplier = 5f; // 스킵 시 타이핑 속도 배율

    [Header("Log UI")]
    public GameObject logPanel;          // 로그 전체 패널
    public TMP_Text logTextContents;    // Scroll View -> Content 안에 있는 그 텍스트
    public ScrollRect logScrollRect;    // 자동 스크롤 조절용

    [Header("Summary UI")]
    public GameObject summaryPanel;      // 줄거리 패널
    public TMP_Text summaryTitleText;
    public TMP_Text summaryContentText;  // 줄거리 내용 텍스트

    private Queue<string> sentences = new Queue<string>();
    private StoryData currentStoryData;
    private bool isTyping = false;
    private TextMeshProRuby currentActiveText; // 현재 글자가 써지고 있는 텍스트 컴포넌트

    private string currentFullContent; // 현재 출력 중인 문장 전체

    private List<string> dialogueLog = new List<string>(); // 로그 저장용

    private Dictionary<string, Vector2> posAnchors = new Dictionary<string, Vector2>();


    void Start()
    {
        SaveAnchorPositions();

        LoadSettings();

        if (DataManager.Instance != null && DataManager.Instance.selectedStoryData != null)
            StartStory(DataManager.Instance.selectedStoryData);
    }

    public void LoadSettings()
    {
        // DataManager나 PlayerPrefs에서 유저가 설정한 속도를 가져옵니다.
        // 예: PlayerPrefs.GetFloat("TypingSpeed", 0.05f);
        if (DataManager.Instance != null)
        {
            // DataManager에 관련 변수가 있다면 여기서 연동
            typingSpeed = DataManager.Instance.userData.textSpeed; 
        }
    }

    // 타이핑 속도 실시간 변경 (슬라이더 등에 연결 가능)
    public void SetTypingSpeed(float newSpeed)
    {
        typingSpeed = newSpeed;
        PlayerPrefs.SetFloat("TextSpeed", newSpeed);
    }

    private void SaveAnchorPositions()
    {
        if (leftCharacterImage != null) posAnchors["left"] = leftCharacterImage.rectTransform.anchoredPosition;
        if (leftCharacterImage2 != null) posAnchors["left2"] = leftCharacterImage2.rectTransform.anchoredPosition;
        if (rightCharacterImage != null) posAnchors["right"] = rightCharacterImage.rectTransform.anchoredPosition;
        if (rightCharacterImage2 != null) posAnchors["right2"] = rightCharacterImage2.rectTransform.anchoredPosition;
        if (centerCharacterImage != null) posAnchors["center"] = centerCharacterImage.rectTransform.anchoredPosition;
    }

    // 화면 클릭 시 호출 (Unity UI Event 등으로 연결)
    public void OnClickDialogue()
    {
        // 스킵 중 클릭하면 스킵 중단
        if (isSkipMode)
        {
            isSkipMode = false;
            return;
        }

        if (isTyping)
        {
            // 1. 타이핑 코루틴 중단
            StopAllCoroutines();
            isTyping = false;

            // 2. 미리 저장해둔 현재 문장 전체를 즉시 출력
            TMP_Text textMesh = currentActiveText.GetComponent<TMP_Text>();
            textMesh.maxVisibleCharacters = 999;

            //즉시 완성 후 Auto 모드라면 대기 시작
            if (isAutoMode) StartCoroutine(AutoNextRoutine());
        }
        else
        {
            DisplayNextSentence();
        }
    }

    public void StartStory(StoryData data)
    {
        currentStoryData = data;
        dialoguePanel.SetActive(true);
        sentences.Clear();

        // CSV 파일 파싱 (줄바꿈 기준)
        string[] lines = data.storyCsv.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            sentences.Enqueue(lines[i]);
        }

        DisplayNextSentence();
    }
    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndStory();
            return;
        }

        string fullLine = sentences.Dequeue();
        string[] parts = Regex.Split(fullLine, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

        // 1. 데이터 파싱
        string speakerKey = parts[COL_SPEAKER].Trim().Trim('"');
        string emotion = parts[COL_EMOTION].Trim().Trim('"');
        string voiceKey = parts[COL_VOICE].Trim().Trim('"');
        string soundKey = parts[COL_SOUND].Trim().Trim('"');
        string bgKey = parts[COL_BG].Trim().Trim('"');
        string posKey = parts[COL_POS].Trim().Trim('"');

        // 2. 언어별 텍스트 추출 (KO=1, JP=2 기준)
        int langOffset = (int)DataManager.Instance.currentLanguage == 1 ? COL_KO : COL_JP;
        string content = parts.Length > langOffset ? parts[langOffset].Trim() : "";
        content = content.Replace("\"", "").Trim();

        // 3. 연출 적용 (배경, 사운드)
        ApplyVisualAndAudio(bgKey, soundKey, voiceKey, speakerKey, emotion, posKey);

        // 4. 대사가 비어있으면 (연출용 줄) 바로 다음 줄로
        if (string.IsNullOrEmpty(content) || content == "None")
        {
            ShowNarrative("");
            return;
        }

        // 5. UI 분기 (나레이션 vs 대화)
        if (speakerKey.ToLower() == "none" || string.IsNullOrEmpty(speakerKey))
        {
            ShowNarrative(content);
            AddToLog("narration", content); // 나레이션도 로그에 남김
        }
        else
        {
            string translatedName = DataManager.Instance.GetLocalizedText(speakerKey);
            ShowSpeakerDialogue(translatedName, content);
            AddToLog(translatedName, content); // 대화 로그 추가
        }
    }

    private void ApplyVisualAndAudio(string bg, string sound, string voice, string speaker, string emotion, string pos)
    {
        // 1. 배경 변경 시 캐릭터들 자동 퇴장 (주인공 잔상 방지)
        if (bg != "None" && !string.IsNullOrEmpty(bg))
        {
            ClearAllCharacterImages();
            Sprite newBG = Resources.Load<Sprite>($"Backgrounds/{bg}");
            if (newBG != null && backgroundImage != null) backgroundImage.sprite = newBG;
        }

        // 2. 사운드 처리
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sound) && sound != "None")
        {

            string[] soundCommands = sound.Split(',');

            foreach (string cmd in soundCommands)
            {
                // 공백 제거 (매우 중요!)
                string trimmedCmd = cmd.Trim();

                if (trimmedCmd.StartsWith("bgm_"))
                {
                    string bgmName = trimmedCmd.Substring(4);
                    if (bgmName.ToLower() == "stop" || bgmName.ToLower() == "none")
                    {
                        AudioManager.Instance.StopBGM();
                    }
                    else
                    {
                        AudioManager.Instance.PlayBGM(bgmName);
                    }
                }
                else if (trimmedCmd.StartsWith("se_"))
                {
                    // "se_"를 제외한 파일명 추출
                    string seName = trimmedCmd.Substring(3);
                    AudioManager.Instance.PlaySE(seName);
                }
            }
        }
        // 3. 캐릭터 이미지 처리
        // 나레이션(None)이 아닐 때만 이미지를 업데이트합니다.
        if (!string.IsNullOrEmpty(speaker) && speaker.ToLower() != "none")
        {
            UpdateCharacterImage(speaker, emotion, pos);
        }
    }

    IEnumerator HandleMovementCommand(string speaker, string emotion, string posCommand)
    {
        string toKey = posCommand.ToLower().Replace("to", "").Trim();

        Image fromSlot = FindCurrentCharacterImage(speaker);

        Image toSlot = GetImageByPos(toKey);

        if (fromSlot == null || toSlot == null || fromSlot == toSlot)
        {
            UpdateCharacterImage(speaker, emotion, toKey); // 예외 상황 시 즉시 배치
            yield break;
        }

        GameObject dummyObj = new GameObject("MovementDummy");
        dummyObj.transform.SetParent(fromSlot.transform.parent, false);
        Image dummyImage = dummyObj.AddComponent<Image>();
        dummyImage.sprite = fromSlot.sprite; // 현재 캐릭터 모습 복사
        dummyImage.rectTransform.sizeDelta = fromSlot.rectTransform.sizeDelta;
        dummyImage.rectTransform.anchoredPosition = fromSlot.rectTransform.anchoredPosition;

        // 원래 있던 슬롯은 일단 끕니다 (이동하는 것처럼 보이게)
        fromSlot.gameObject.SetActive(false);

        // 4. Lerp 이동
        Vector2 startPos = fromSlot.rectTransform.anchoredPosition;
        Vector2 endPos = posAnchors[toKey];

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            dummyImage.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / moveDuration);
            yield return null;
        }

        // 5. 도착 후 처리
        // 목적지 슬롯에 캐릭터 정보 입력하고 활성화
        string fileName = $"{speaker}_{emotion}";
        toSlot.sprite = Resources.Load<Sprite>($"Characters/{speaker}/{fileName}");
        toSlot.gameObject.SetActive(true);
        // 목적지 슬롯 위치는 고정된 앵커 좌표로 리셋 (혹시 모르니)
        toSlot.rectTransform.anchoredPosition = posAnchors[toKey];

        // 연출용 더미 삭제
        Destroy(dummyObj);
    }

    private Image FindCurrentCharacterImage(string speaker)
    {
        Image[] allSlots = { leftCharacterImage, leftCharacterImage2, rightCharacterImage, rightCharacterImage2, centerCharacterImage };
        foreach (var slot in allSlots)
        {
            // 슬롯이 켜져 있고, 스프라이트 이름에 캐릭터 이름이 포함되어 있다면 해당 슬롯 반환
            if (slot.gameObject.activeSelf && slot.sprite != null && slot.sprite.name.Contains(speaker))
                return slot;
        }
        return null;
    }

    private Image GetImageByPos(string pos)
    {
        switch (pos.ToLower())
        {
            case "left": return leftCharacterImage;
            case "left2": return leftCharacterImage2;
            case "right": return rightCharacterImage;
            case "right2": return rightCharacterImage2;
            case "center": return centerCharacterImage;
            default: return null;
        }
    }

    private void UpdateCharacterImage(string speaker, string emotion, string pos)
    {
        string posLower = pos.ToLower();
        string targetPos = posLower;

        // 1. "to" 명령 처리 (순간이동)
        if (posLower.StartsWith("to"))
        {
            targetPos = posLower.Replace("to", "").Trim();

            // 현재 이 캐릭터가 활성화된 슬롯을 찾아서 꺼줍니다.
            Image currentSlot = FindCurrentCharacterImage(speaker);
            if (currentSlot != null)
            {
                currentSlot.gameObject.SetActive(false);
                currentSlot.sprite = null; // 잔상 방지
            }
        }

        // 2. 목적지 슬롯에 이미지 배치
        Image targetSlot = GetImageByPos(targetPos);

        if (targetSlot != null)
        {
            // 만약 이동 명령이 아니더라도, 다른 곳에 이미 켜져 있다면 꺼줌 (중복 방지)
            Image oldSlot = FindCurrentCharacterImage(speaker);
            if (oldSlot != null && oldSlot != targetSlot)
            {
                oldSlot.gameObject.SetActive(false);
            }

            string fileName = $"{speaker}_{emotion}";
            Sprite characterSprite = Resources.Load<Sprite>($"Characters/{speaker}/{fileName}");

            if (characterSprite != null)
            {
                targetSlot.sprite = characterSprite;
                targetSlot.gameObject.SetActive(true);

                // 중요: 슬롯 자체의 위치는 건드리지 않고, 
                // 혹시라도 틀어져 있을 경우를 대비해 저장된 앵커 위치로만 고정합니다.
                if (posAnchors.ContainsKey(targetPos))
                {
                    targetSlot.rectTransform.anchoredPosition = posAnchors[targetPos];
                }
            }
        }

        ApplySpeakerFocus(speaker);

    }

    private void ApplySpeakerFocus(string currentSpeaker)
    {
        Image[] allSlots = { leftCharacterImage, leftCharacterImage2, rightCharacterImage, rightCharacterImage2, centerCharacterImage };

        foreach (var slot in allSlots)
        {
            if (slot.gameObject.activeSelf && slot.sprite != null)
            {
                // 슬롯의 스프라이트 이름에 화자 이름이 포함되어 있는지 확인
                if (slot.sprite.name.Contains(currentSpeaker))
                {
                    // 말하는 사람은 원래 밝기(Color.white)
                    slot.color = Color.white;
                    // 만약 더 돋보이게 하고 싶다면 크기를 살짝 키울 수도 있습니다.
                    slot.rectTransform.localScale = new Vector3(1.05f, 1.05f, 1f);
                }
                else
                {
                    // 말하지 않는 사람은 어둡게/회색으로 (Color.gray)
                    slot.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    slot.rectTransform.localScale = Vector3.one;
                }
            }
        }
    }

    private void ClearAllCharacterImages()
    {
        leftCharacterImage.gameObject.SetActive(false);
        leftCharacterImage2.gameObject.SetActive(false);
        rightCharacterImage.gameObject.SetActive(false);
        rightCharacterImage2.gameObject.SetActive(false);
        centerCharacterImage.gameObject.SetActive(false);
    }

    public void ShowNarrative(string content)
    {
        speakerPanel.SetActive(false);
        narrativePanel.SetActive(true);
        currentActiveText = narrativeContentText;

        StartCoroutine(TypeSentence(content, narrativeContentText));
    }

    public void ShowSpeakerDialogue(string name, string content)
    {
        narrativePanel.SetActive(false);
        speakerPanel.SetActive(true);
        speakerNameText.text = name;
        currentActiveText = speakerContentText;

        StartCoroutine(TypeSentence(content, speakerContentText));
    }

    IEnumerator TypeSentence(string sentence, TextMeshProRuby rubyComponent)
    {
        isTyping = true;

        // 1. 정제된 문장을 전역 변수에 저장 (클릭 시 즉시 완성용)
        currentFullContent = sentence.Trim().Trim('"')
                                     .Replace("\"\"", "\"")
                                     .Replace("\\n", "\n");

        dialogueLog.Add(currentFullContent);

        rubyComponent.Text = currentFullContent;
        rubyComponent.Apply();

        // 핵심: 일단 글자를 모두 안 보이게 설정
        TMP_Text textMesh = rubyComponent.GetComponent<TMP_Text>();

        // 일단 글자를 모두 안 보이게 설정
        textMesh.maxVisibleCharacters = 0;

        // 2. 한 프레임 대기 (TMP가 텍스트 정보를 갱신할 시간을 줍니다)
        yield return null;

        // 스킵 모드일 때는 속도를 조절하거나 즉시 완성
        float currentSpeed = isSkipMode ? typingSpeed / skipSpeedMultiplier : typingSpeed;

        int totalVisibleCharacters = textMesh.textInfo.characterCount;

        // 2. 한 글자씩 출력
        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            textMesh.maxVisibleCharacters = i;
            yield return new WaitForSeconds(currentSpeed);
        }

        isTyping = false;

        // 대사가 끝난 후 자동 처리
        if (isSkipMode)
        {
            yield return new WaitForSeconds(0.1f); // 아주 잠깐 대기 후 다음
            DisplayNextSentence();
        }
        else if (isAutoMode)
        {
            yield return new WaitForSeconds(autoDelay); // 설정된 시간만큼 대기
            if (isAutoMode) DisplayNextSentence();
        }
    }

    // Auto 모드 대기 루틴 (별도 분리하여 관리 용이)
    IEnumerator AutoNextRoutine()
    {
        yield return new WaitForSeconds(autoDelay);
        if (isAutoMode && !isTyping) DisplayNextSentence();
    }

    private void EndStory()
    {
        dialoguePanel.SetActive(false);
        OnStoryComplete(currentStoryData);
    }

    public void OnStoryComplete(StoryData data)
    {
        if (DataManager.Instance == null) return;
        UserData user = DataManager.Instance.userData;
        StageHistory history = user.stageHistory.Find(x => x.stageID == data.storyID);

        if (history == null)
        {
            history = new StageHistory { stageID = data.storyID };
            user.stageHistory.Add(history);
        }

        if (!history.isStoryRead)
        {
            history.isStoryRead = true;
            DataManager.Instance.GiveStoryReward(data.rewardItemID, data.rewardCount);
            DataManager.Instance.SaveData();
        }

        if (GlobalUIManager.Instance != null)
        {
            // 스토리 선택 화면으로 상태 변경 (내부에서 LoadScene("StorySelectScene") 실행됨)
            GlobalUIManager.Instance.ChangeState(SceneState.StorySelect, true);
        }
        else
        {
            // 만약 테스트 용도로 Manager 없이 실행 중일 때를 대비한 예외 처리
            SceneManager.LoadScene("StorySelectScene");
        }
    }

    public void ToggleAutoMode()
    {
        isAutoMode = !isAutoMode;
        isSkipMode = false; // 스킵과 오토는 보통 하나만 활성화
        if (isAutoMode && !isTyping) DisplayNextSentence();
    }

    public void ShowLog()
    {
        if (logPanel == null) return;

        logPanel.SetActive(true);
        Time.timeScale = 0f;

        // 핵심: 텍스트가 적용된 직후 레이아웃을 강제로 재계산하게 함
        StartCoroutine(ForceUpdateLogScroll());
    }

    IEnumerator ForceUpdateLogScroll()
    {
        // 한 프레임 대기하여 TMP가 텍스트 높이를 계산할 시간을 줌
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (logScrollRect != null)
        {
            // 0f는 맨 아래, 1f는 맨 위입니다.
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 로그를 초기화하거나 새 대사를 추가하는 함수
    public void AddToLog(string speaker, string content)
    {
        if (logTextContents == null) return;

        // 화자 이름에 색상을 넣어 가독성을 높입니다 (Rich Text 활용)
        string speakerColor = "#FFD700"; // 금색 예시
        string formattedLog = $"<color={speakerColor}><b>[{speaker}]</b></color> {content}\n\n";

        // 기존 텍스트에 계속 덧붙임
        logTextContents.text += formattedLog;
    }


    public void CloseLog()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(false);
            // 로그창 닫으면 다시 시간 흐르게 하기
            Time.timeScale = 1f;
        }
    }

    public void ShowSummaryAndSkip()
    {
        // 1. 진행 중인 모든 연출 중단
        StopAllCoroutines();
        isTyping = false;
        Time.timeScale = 0f;

        // 2. 제목 세팅 (storyTitle 활용)
        if (summaryTitleText != null)
        {
            // 예: [시작의 해변] 줄거리
            string localizedTitle = DataManager.Instance.GetLocalizedText(currentStoryData.storyTitle);
            summaryTitleText.text = $"[{localizedTitle}] {DataManager.Instance.GetLocalizedText("UI_SUMMARY_LABEL")}";
        }

        // 3. 내용 세팅 (로컬라이제이션 키 활용)
        if (summaryContentText != null)
        {
            // StoryData에 추가한 summaryLogKey를 사용하여 다국어 텍스트를 가져옵니다.
            summaryContentText.text = DataManager.Instance.GetLocalizedText(currentStoryData.summaryLogKey);
        }
        summaryPanel.SetActive(true);
    }

    // 줄거리 패널의 '확인' 버튼에 연결
    public void ConfirmSkip()
    {
        summaryPanel.SetActive(false);

        // 4. 모든 대사를 소모한 것으로 처리하고 종료
        sentences.Clear();
        EndStory();
    }
    public void CancelSummarySkip()
    {
        // 1. 시간 다시 흐르게 하기
        Time.timeScale = 1f;

        // 2. 패널 비활성화
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }

        isTyping = false;
    }
}