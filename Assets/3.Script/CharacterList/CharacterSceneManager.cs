using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSceneManager : MonoBehaviour
{
    // 현재 씬에서 관리하는 "선택된 캐릭터"
    private UnitData currentSelectedData;
    private CharacterInfo currentSelectedInfo;

    [Header("Right Panels")]
    public GameObject defaultPanel;
    public CharacterUpgradePanel upgradePanel;
    public CharacterTagPanel tagPanel;
    public CharacterBTPanel breakthroughPanel;
    public DetailPanel detailPanel;

    [Header("Character Info")]
    [SerializeField] private TMP_Text nameText;  // 캐릭터 이름
    [SerializeField] private TMP_Text levelText; // 레벨
    [SerializeField] private TMP_Text hpText; // HP
    [SerializeField] private TMP_Text atkText; // ATK
    [SerializeField] private TMP_Text spdText; // SPD
    [SerializeField] private Slider expSlider; // EXP Slider
    [SerializeField] private TMP_Text expText; // EXP Text;

    [SerializeField] private Image detailImage;  // 왼쪽 큰 이미지
    [SerializeField] private Image typeIcon;     // Dealer, Healer, Buffer 아이콘
    [SerializeField] private Image tagIcon;      // Dot, Splash, Direct 아이콘

    [Header("Rarity")]
    [SerializeField] private Image background;   // 레어리티 들어갈 뒷배경
    [SerializeField] private Image rarityIcon;   // 레어리티 아이콘
    [SerializeField] private Image frameImage;  // 레어리티 프레임

    [Header("BackButton")]
    public GameObject backButton;

    [Header("Tab Button Sprites")]
    // 각 버튼의 Image 컴포넌트
    public Image upgradeBtnImg;
    public Image tagBtnImg;
    public Image breakthroughBtnImg;
    public Image detailBtnImg;

    [Header("Tab Text Components")]
    public TMP_Text upgradeBtnText;
    public TMP_Text tagBtnText;
    public TMP_Text breakthroughBtnText;
    public TMP_Text detailBtnText;

    [Header("Tab Button Colors")]
    public Sprite activeTabSprite;    // 선택된 탭 배경 (밝거나 무늬가 있음)
    public Sprite inactiveTabSprite;  // 선택 안 된 탭 배경 (어둡거나 평범함)

    [Header("Tab Text Colors")]
    public Color activeTextColor = new Color(0.41f, 0.31f, 0.24f); // #6A4F3E (짙은 갈색)
    public Color inactiveTextColor= new Color(0.898f, 0.871f, 0.682f);

    [Header("Icon Sprites")]
    public Sprite[] typeIcons;
    public Sprite[] tagIcons;
    public Sprite[] rarityBackGroundImages;
    public Sprite[] rarityFrameImages;
    public Sprite[] rarityIconImages;
    public GameObject[] breakThrough;   // 돌파에 따라 하트 표시

    public enum CharacterPanelState { Default, Upgrade, Tag, Breakthrough, Detail }

    private CharacterPanelState currentPanel = CharacterPanelState.Default;

    private void OnEnable()
    {
        // 구독 시작
        DataManager.OnCharacterSelected += UpdateDetailUI;

        DataManager.OnUserDataChanged += RefreshUI;
    }

    private void OnDisable()
    {
        // 씬 나갈 때 구독 해제 (메모리 정리)
        DataManager.OnCharacterSelected -= UpdateDetailUI;

        DataManager.OnUserDataChanged -= RefreshUI;
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("so_sweet");
        }

        // 1. 유저가 보유한 캐릭터 리스트를 가져오기
        List<CharacterSaveData> ownedCharacters = DataManager.Instance.userData.ownedCharacters;

        if (DataManager.Instance.userInventory != null && DataManager.Instance.userInventory.Count > 0)
        {
            // 2. 리스트의 맨 첫 번째 캐릭터 정보를 가져옵니다.
            // (보통 0번 인덱스가 유저의 메인 캐릭터나 가장 먼저 얻은 캐릭터입니다.)
            CharacterInfo firstInfo = DataManager.Instance.userInventory[0];
            // 3. UnitData(스프라이트 등)를 DataManager에서 찾아옵니다.
            UnitData firstData = DataManager.Instance.GetPlayerData(firstInfo.unitID);

            firstInfo.InitializePoint(firstData, firstInfo.currentBreakthrough);

            // 4. UpdateDetailUI를 직접 호출해서 화면을 채워줍니다!
            // CharacterInfo로 변환해서 넣어줘야 한다면 형식을 맞춰주세요.
            UpdateDetailUI(firstData, firstInfo);

            Debug.Log($"[자동 선택 완료] {firstData.name} (ID: {firstInfo.unitID})");

        }

        upgradeBtnText.text = DataManager.Instance.GetLocalizedText("Character_Upgrade");
        tagBtnText.text = DataManager.Instance.GetLocalizedText("Character_Tag");
        breakthroughBtnText.text = DataManager.Instance.GetLocalizedText("Character_BT");
        detailBtnText.text = DataManager.Instance.GetLocalizedText("Character_Detail");

    }

    private void UpdateStatTexts(UnitData data, CharacterInfo info)
    {
        if (data is CharacterData charData)
        {
            // 1. 레벨에 따른 기본 성장치 계산 (캐릭터 로직과 동일하게)
            float rarityWeight = GetRarityWeight(charData.rarity);
            float hpGain = (0.05f + charData.hpGrowth) * rarityWeight;
            float atkGain = (0.05f + charData.attackGrowth) * rarityWeight;

            int baseHp = Mathf.RoundToInt(charData.baseHp * (1f + (hpGain * (info.currentLevel - 1))));
            int baseAtk = Mathf.RoundToInt(charData.baseAttack * (1f + (atkGain * (info.currentLevel - 1))));
            int baseSpd = charData.baseSpeed;

            // 2. 태그 보너스 가져오기 (DataManager 경유)
            var tagBonus = DataManager.Instance.GetTotalTagStats(info.unitID);

            // 3. 텍스트 업데이트 (확밀아 스타일)
            // 만약 보너스가 있다면 초록색으로 표시하거나 합산해서 표시
            hpText.text = $"{baseHp + tagBonus.hp}";
            atkText.text = $"{baseAtk + tagBonus.atk}";
            spdText.text = $"{baseSpd + tagBonus.spd}";

            // (팁) 보너스 수치만 따로 강조하고 싶다면 RichText 사용 가능
            // atkText.text = $"{baseAtk} <color=#00FF00>(+{tagBonus.atk})</color>";
        }
    }

    private void UpdateDetailUI(UnitData data, CharacterInfo info)
    {
        if (data == null) return;

        // 중요: 나중에 패널을 열 때 쓰기 위해 현재 선택된 정보를 담아두기
        currentSelectedData = data;
        currentSelectedInfo = info;

        // 레어도 표시
        Rarity currentRarity = DataManager.Instance.CalculateRarity(info.totalPoint);
        int rarityIndex = (int)currentRarity;


        //스텟 계산 및 표시
        UpdateStatTexts(data, info);

        // 1. 기본 정보 및 로컬라이징
        detailImage.sprite = data.unitFullIllust;
        detailImage.SetNativeSize();

        nameText.text = DataManager.Instance.GetLocalizedText(data.unitNameKey);
        levelText.text = $"{info.currentLevel}";

        // 2. 유닛 타입 아이콘 설정
        typeIcon.sprite = data.unitType switch
        {
            UnitType.Dealer => typeIcons[0],
            UnitType.Healer => typeIcons[1],
            UnitType.Buffer => typeIcons[2],
            _ => null
        };

        tagIcon.sprite = data.defaultTag switch
        {
            "Dot" => tagIcons[0],
            "Splash" => tagIcons[1],
            "Direct" => tagIcons[2],
            _ => null
        };

        rarityIcon.sprite = rarityIconImages[rarityIndex];

        background.sprite = rarityBackGroundImages[rarityIndex];

        frameImage.sprite = rarityFrameImages[rarityIndex];

        // 3. 레어리티에 따른 배경색 변경
        background.color = DataManager.Instance.GetRarityColor(data.rarity);

        //4. 돌파 UI 설정 (오른쪽 하트 표시)
        bool isMaxRarity = (data.rarity == Rarity.EL);

        for (int i = 0; i < breakThrough.Length; i++)
        {
            if (isMaxRarity)
            {
                // EL 등급일 때는 하트를 모두 비활성화 (혹은 필요에 따라 변경)
                breakThrough[i].SetActive(false);
            }
            else
            {
                // 현재 돌파 수(info.breakthroughCount)보다 작은 인덱스의 하트만 활성화
                breakThrough[i].SetActive(i < info.currentBreakthrough);
            }
        }

        int requiredExp = DataManager.Instance.GetRequiredExp(info.currentLevel);
        float expRatio = requiredExp > 0 ? (float)info.currentExp / requiredExp : 0f;
        expSlider.value = expRatio;
        expText.text = $"{info.currentExp} / {requiredExp}";
        levelText.text = $"{info.currentLevel}";

        // (선택) 배경이 바뀔 때 살짝 페이드 연출을 주면 더 고급스럽습니다.
        detailImage.transform.DOKill();
        detailImage.transform.localPosition = new Vector3(-50f, 0, 0); // 살짝 왼쪽에서
        detailImage.transform.DOLocalMoveX(0f, 0.4f).SetEase(Ease.OutCubic); // 슥 들어오기
        detailImage.DOFade(0f, 0f); // 순식간에 투명하게
        detailImage.DOFade(1f, 0.4f); // 페이드 인
    }

    // 버튼들에 연결할 함수
    public void OnClickBack() => SwitchPanel(CharacterPanelState.Default);
    public void OnClickUpgrade() => SwitchPanel(CharacterPanelState.Upgrade);
    public void OnClickTag() => SwitchPanel(CharacterPanelState.Tag);
    public void OnClickBreakthrough() => SwitchPanel(CharacterPanelState.Breakthrough);
    public void OnClickDetail() => SwitchPanel(CharacterPanelState.Detail);

    private void SwitchPanel(CharacterPanelState target)
    {
        // 이미 열려있는 걸 다시 누르면 닫거나 유지
        if (currentPanel == target) return;

        AudioManager.Instance.PlaySE("UI_Tab_Switch");

        // 1. 모든 패널 끄기
        upgradePanel.gameObject.SetActive(false);
        tagPanel.gameObject.SetActive(false);
        breakthroughPanel.gameObject.SetActive(false);


        ResetButtonColor();


        // 2. 선택한 패널만 켜기
        switch (target)
        {
            case CharacterPanelState.Default:
                if (defaultPanel != null) defaultPanel.SetActive(true);
                GlobalUIManager.Instance.ChangeState(SceneState.CharacterList, true);
                backButton.SetActive(false);
                break;
            case CharacterPanelState.Upgrade:
                upgradePanel.gameObject.SetActive(true);
                upgradeBtnText.color = activeTextColor;
                upgradeBtnImg.sprite = activeTabSprite;
                if (currentSelectedInfo != null) upgradePanel.Init(currentSelectedInfo);
                GlobalUIManager.Instance.ChangeState(SceneState.CharacterUpgrade, true);
                backButton.SetActive(true);
                break;
            case CharacterPanelState.Tag:
                tagPanel.gameObject.SetActive(true);
                tagBtnText.color = activeTextColor;
                tagBtnImg.sprite = activeTabSprite;
                GlobalUIManager.Instance.ChangeState(SceneState.CharacterCustomTag, true);
                backButton.SetActive(true);
                break;

            case CharacterPanelState.Breakthrough:
                breakthroughPanel.gameObject.SetActive(true);
                breakthroughBtnText.color = activeTextColor;
                breakthroughBtnImg.sprite = activeTabSprite;
                GlobalUIManager.Instance.ChangeState(SceneState.CharacterBreakThrough, true);
                backButton.SetActive(true);
                break;
            case CharacterPanelState.Detail:
                detailPanel.gameObject.SetActive(true);
                detailBtnText.color = activeTextColor;
                detailBtnImg.sprite = activeTabSprite;
                GlobalUIManager.Instance.ChangeState(SceneState.Detail, true);
                backButton.SetActive(true);
                break;

        }

        currentPanel = target;

        // 3. 패널 내용 갱신 (현재 선택된 캐릭터 정보 전달)
        RefreshCurrentPanel();
    }

    private void RefreshCurrentPanel()
    {
        if (currentSelectedInfo == null) return;

        switch (currentPanel)
        {
            case CharacterPanelState.Upgrade:
                upgradePanel.Init(currentSelectedInfo);
                upgradePanel.RefreshList();
                break;
            case CharacterPanelState.Tag:
                tagPanel.Init(currentSelectedInfo); // TagPanel에도 Init이 있다고 가정
                break;
            case CharacterPanelState.Breakthrough:
                breakthroughPanel.Init(currentSelectedInfo); // BTPanel에도 Init이 있다고 가정
                break;
        }
    }

    private void RefreshUI()
    {
        if (currentSelectedInfo == null) return;

        int previousLevel = int.Parse(levelText.text);
        int currentLevel = currentSelectedInfo.currentLevel;

        //레어도 가지고 오기
        Rarity currentRarity = DataManager.Instance.CalculateRarity(currentSelectedInfo.totalPoint);
        int rarityIndex = (int)currentRarity;

        // 만약 Enum 순서와 배열 순서가 다르다면 switch 사용
        rarityIcon.sprite = rarityIconImages[rarityIndex];
        background.sprite = rarityBackGroundImages[rarityIndex];
        frameImage.sprite = rarityFrameImages[rarityIndex];
        background.color = DataManager.Instance.GetRarityColor(currentRarity);

        // 3. 돌파 하트(breakThrough) UI 갱신
        bool isMaxRarity = (currentRarity == Rarity.EL);
        for (int i = 0; i < breakThrough.Length; i++)
        {
            if (isMaxRarity) breakThrough[i].SetActive(false);
            else breakThrough[i].SetActive(i < currentSelectedInfo.currentBreakthrough);
        }

        // 레벨 텍스트 갱신
        levelText.text = $"{currentSelectedInfo.currentLevel}";

        Debug.Log(currentSelectedInfo.currentExp);
        Debug.Log(DataManager.Instance.GetRequiredExp(currentSelectedInfo.currentLevel));

 

        float targetValue = (float)currentSelectedInfo.currentExp / DataManager.Instance.GetRequiredExp(currentSelectedInfo.currentLevel);
        expSlider.DOValue(targetValue, 0.5f).SetEase(Ease.OutCubic);

        expText.text = $"{currentSelectedInfo.currentExp} / {DataManager.Instance.GetRequiredExp(currentSelectedInfo.currentLevel)}";

        if (currentLevel > previousLevel)
        {
            AudioManager.Instance.PlaySE("Level_Up_Jingle");

            // (선택) 레벨업 시 텍스트가 커졌다 작아지는 효과 (DOTween)
            levelText.transform.DOKill();
        levelText.transform.DOScale(1.2f, 0.1f).OnComplete(() =>
        {
            levelText.transform.DOScale(1f, 0.1f);
        });
        }
        else
        {
            AudioManager.Instance.PlaySE("EXP_Bar_Fill");
        }

        // 강화나 태그 교체 후 변화된 스탯을 왼쪽 UI에 다시 반영
        UpdateStatTexts(currentSelectedData, currentSelectedInfo);

        // 리프레쉬
        RefreshCurrentPanel();
    }

    private void ResetButtonColor()
    {
        // 탭 버튼 이미지 비활성화
        upgradeBtnImg.sprite = inactiveTabSprite;
        tagBtnImg.sprite = inactiveTabSprite;
        breakthroughBtnImg.sprite = inactiveTabSprite;

        // 탭 버튼 텍스트 비활성화
        upgradeBtnText.color = inactiveTextColor;
        tagBtnText.color = inactiveTextColor;
        breakthroughBtnText.color = inactiveTextColor;
    }

    private float GetRarityWeight(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.L => 1.0f,
            Rarity.PL => 1.2f,
            Rarity.TL => 1.4f,
            Rarity.EL => 1.6f,
            _ => 1.0f
        };
    }
}
