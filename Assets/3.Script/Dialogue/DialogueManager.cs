using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private Queue<string> sentences = new Queue<string>();
    private StoryData currentStoryData;
    private bool isTyping = false;
    private TMP_Text currentActiveText; // 현재 글자가 써지고 있는 텍스트 컴포넌트




    void Start()
    {
        if (DataManager.Instance != null && DataManager.Instance.selectedStoryData != null)
            StartStory(DataManager.Instance.selectedStoryData);
    }

    // 화면 클릭 시 호출 (Unity UI Event 등으로 연결)
    public void OnClickDialogue()
    {
        if (isTyping)
        {
            // 타이핑 중이면 즉시 완성 (생략 가능)
            StopAllCoroutines();
            isTyping = false;
            // 줄바꿈 처리 포함하여 출력
            string finalContent = sentences.Peek().Split(',')[COL_KO + (int)DataManager.Instance.currentLanguage - 1];
            currentActiveText.text = finalContent.Replace("\\n", "\n").Trim();
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
        foreach (string line in lines)
        {
            sentences.Enqueue(line);
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
        string[] parts = fullLine.Split(',');

        // 1. 데이터 파싱
        string speakerKey = parts[COL_SPEAKER].Trim();
        string emotion = parts[COL_EMOTION].Trim();
        string voiceKey = parts[COL_VOICE].Trim();
        string soundKey = parts[COL_SOUND].Trim();
        string bgKey = parts[COL_BG].Trim();
        string posKey = parts[COL_POS].Trim();

        // 2. 언어별 텍스트 추출 (KO=1, JP=2 기준)
        int langOffset = (int)DataManager.Instance.currentLanguage == 1 ? COL_KO : COL_JP;
        string content = parts.Length > langOffset ? parts[langOffset].Trim() : "";

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
        }
        else
        {
            string translatedName = DataManager.Instance.GetLocalizedText(speakerKey);
            ShowSpeakerDialogue(translatedName, content);
            // 여기서 posKey(left, Center 등)를 이용해 패널 위치를 조절할 수 있습니다.
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

        // 2. 사운드 처리 (AudioManager 연동)
        if (AudioManager.Instance != null)
        {
            if (sound.StartsWith("bgm_")) AudioManager.Instance.PlayBGM(sound);
            else if (sound.StartsWith("se_")) AudioManager.Instance.PlaySE(sound);

            if (voice != "None" && !string.IsNullOrEmpty(voice))
                AudioManager.Instance.PlayVoice(voice);
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
        textUI.text = "";

        // CSV의 \n 문자를 실제 줄바꿈으로 변환
        string processedSentence = sentence.Replace("\\n", "\n");

        foreach (char letter in processedSentence.ToCharArray())
        {
            textUI.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
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
    }

}