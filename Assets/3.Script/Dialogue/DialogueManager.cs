using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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
    public TMP_Text speakerContentText;
    public TMP_Text narrativeContentText;

    [Header("Character Images")]
    public Image leftCharacterImage;   // 왼쪽 캐릭터 슬롯
    public Image leftCharacterImage2;   // 왼쪽 캐릭터 슬롯
    public Image rightCharacterImage;  // 오른쪽 캐릭터 슬롯
    public Image rightCharacterImage2;  // 오른쪽 캐릭터 슬롯
    public Image centerCharacterImage; // 중앙 캐릭터 슬롯

    [Header("Settings")]
    public float typingSpeed = 0.05f;

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
    private TMP_Text currentActiveText; // 현재 글자가 써지고 있는 텍스트 컴포넌트

    private string currentFullContent; // 현재 출력 중인 문장 전체

    private List<string> dialogueLog = new List<string>(); // 로그 저장용


    void Start()
    {
        if (DataManager.Instance != null && DataManager.Instance.selectedStoryData != null)
            StartStory(DataManager.Instance.selectedStoryData);
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
            currentActiveText.text = currentFullContent;

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
            DisplayNextSentence();
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
        // 1. 배경 변경
        if (bg != "None" && !string.IsNullOrEmpty(bg) && backgroundImage != null)
        {
            Sprite newBG = Resources.Load<Sprite>($"Backgrounds/{bg}");
            if (newBG != null) backgroundImage.sprite = newBG;
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



    private void UpdateCharacterImage(string speaker, string emotion, string pos)
    {
        string fileName = $"{speaker}_{emotion}";
        Sprite characterSprite = Resources.Load<Sprite>($"Characters/{speaker}/{fileName}");

        if (characterSprite == null)
        {
            Debug.LogWarning($"{fileName} 이미지를 찾을 수 없습니다.");
            return;
        }

        // 위치(pos) 값에 따라 어떤 Image 슬롯을 쓸지 결정
        Image targetImage = null;

        switch (pos.ToLower())
        {
            case "left": targetImage = leftCharacterImage; break;
            case "left2": targetImage = leftCharacterImage2; break;
            case "right": targetImage = rightCharacterImage; break;
            case "right2": targetImage = rightCharacterImage2; break;
            case "center": targetImage = centerCharacterImage; break;
        }

        if (targetImage != null)
        {
            targetImage.sprite = characterSprite;
            targetImage.gameObject.SetActive(true);

            // (팁) 만약 새로운 캐릭터가 등장할 때 기존 이미지를 지우고 싶다면 
            // 여기서 다른 슬롯들을 돌며 비워주는 로직을 추가할 수 있습니다.
        }
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

    IEnumerator TypeSentence(string sentence, TMP_Text textUI)
    {
        isTyping = true;

        // 1. 정제된 문장을 전역 변수에 저장 (클릭 시 즉시 완성용)
        currentFullContent = sentence.Trim().Trim('"')
                                     .Replace("\"\"", "\"")
                                     .Replace("\\n", "\n");

        dialogueLog.Add(currentFullContent);

        textUI.text = "";

        // 스킵 모드일 때는 속도를 조절하거나 즉시 완성
        float currentSpeed = isSkipMode ? typingSpeed / skipSpeedMultiplier : typingSpeed;

        // 2. 한 글자씩 출력
        foreach (char letter in currentFullContent.ToCharArray())
        {
            textUI.text += letter;
            yield return new WaitForSeconds(isSkipMode ? typingSpeed / skipSpeedMultiplier : typingSpeed);
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