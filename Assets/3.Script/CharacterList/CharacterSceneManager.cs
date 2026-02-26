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
    public GameObject tagPanel;
    public GameObject breakthroughPanel;

    [Header("Character Info")]
    [SerializeField] private TMP_Text nameText;  // 캐릭터 이름
    [SerializeField] private TMP_Text levelText; // 레벨
    [SerializeField] private Image detailImage;  // 왼쪽 큰 이미지
    [SerializeField] private Image typeIcon;     // Dealer, Healer, Buffer 아이콘
    [SerializeField] private Image tagIcon;      // Dot, Splash, Direct 아이콘

    [Header("Rarity")]
    [SerializeField] private Image background;   // 레어리티 들어갈 뒷배경
    [SerializeField] private Image rarityIcon;   // 레어리티 아이콘
    [SerializeField] private Image frameImage;  // 레어리티 프레임

    [Header("Icon Sprites")]
    public Sprite[] typeIcons;
    public Sprite[] tagIcons;
    public Sprite[] rarityBackGroundImages;
    public Sprite[] rarityFrameImages;
    public Sprite[] rarityIconImages;
    public GameObject[] breakThrough;   // 돌파에 따라 하트 표시

    public enum CharacterPanelState { Default, Upgrade, Tag, Breakthrough }

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
        // 1. 유저가 보유한 캐릭터 리스트를 가져오기
        List<CharacterSaveData> ownedCharacters = DataManager.Instance.userData.ownedCharacters;

        if (DataManager.Instance.userInventory != null && DataManager.Instance.userInventory.Count > 0)
        {
            // 2. 리스트의 맨 첫 번째 캐릭터 정보를 가져옵니다.
            // (보통 0번 인덱스가 유저의 메인 캐릭터나 가장 먼저 얻은 캐릭터입니다.)
            CharacterInfo firstInfo = DataManager.Instance.userInventory[0];
            // 3. UnitData(스프라이트 등)를 DataManager에서 찾아옵니다.
            UnitData firstData = DataManager.Instance.GetPlayerData(firstInfo.unitID);

            // 4. UpdateDetailUI를 직접 호출해서 화면을 채워줍니다!
            // CharacterInfo로 변환해서 넣어줘야 한다면 형식을 맞춰주세요.
            UpdateDetailUI(firstData, firstInfo);

            Debug.Log($"[자동 선택 완료] {firstData.name} (ID: {firstInfo.unitID})");

        }
    }


    private void UpdateDetailUI(UnitData data, CharacterInfo info)
    {
        if (data == null) return;

        // 중요: 나중에 패널을 열 때 쓰기 위해 현재 선택된 정보를 담아둡니다.
        currentSelectedData = data;
        currentSelectedInfo = info;

        // 1. 기본 정보 및 로컬라이징
        detailImage.sprite = data.unitFullIllust;
        detailImage.SetNativeSize();

        nameText.text = DataManager.Instance.GetLocalizedText(data.unitNameKey);
        levelText.text = $"Lv. {info.currentLevel}";

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

        rarityIcon.sprite = data.rarity switch
        {
            Rarity.L => rarityIconImages[0],
            Rarity.PL => rarityIconImages[1],
            Rarity.TL => rarityIconImages[2],
            Rarity.EL => rarityIconImages[3],
            _ => null
        };

        background.sprite = data.rarity switch
        {
            Rarity.L => rarityBackGroundImages[0],
            Rarity.PL => rarityBackGroundImages[1],
            Rarity.TL => rarityBackGroundImages[2],
            Rarity.EL => rarityBackGroundImages[3],
            _ => null
        };

        frameImage.sprite = data.rarity switch
        {
            Rarity.L => rarityFrameImages[0],
            Rarity.PL => rarityFrameImages[1],
            Rarity.TL => rarityFrameImages[2],
            Rarity.EL => rarityFrameImages[3],
            _ => null
        };

        // 3. 레어리티에 따른 배경색 변경
        background.color = GetRarityColor(data.rarity);

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

        // (선택) 배경이 바뀔 때 살짝 페이드 연출을 주면 더 고급스럽습니다.
        detailImage.transform.DOKill();
        detailImage.transform.localPosition = new Vector3(-50f, 0, 0); // 살짝 왼쪽에서
        detailImage.transform.DOLocalMoveX(0f, 0.4f).SetEase(Ease.OutCubic); // 슥 들어오기
        detailImage.DOFade(0f, 0f); // 순식간에 투명하게
        detailImage.DOFade(1f, 0.4f); // 페이드 인
    }

    private Color GetRarityColor(Rarity rarity)
    {
        // 아까 UnitIcon에서 썼던 컬러셋을 그대로 가져오거나 
        // 상세창용으로 더 밝은/화려한 컬러를 써보세요.
        string hex = rarity switch
        {
            Rarity.L => "#8CCBF3",
            Rarity.PL => "#C5AEE1",
            Rarity.TL => "#F9E985",
            Rarity.EL => "#E9B9D2",
            _ => "#5F6267"
        };

        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }





    // 버튼들에 연결할 함수
    public void OnClickUpgrade() => SwitchPanel(CharacterPanelState.Upgrade);
    public void OnClickTag() => SwitchPanel(CharacterPanelState.Tag);
    public void OnClickBreakthrough() => SwitchPanel(CharacterPanelState.Breakthrough);

    private void SwitchPanel(CharacterPanelState target)
    {
        // 이미 열려있는 걸 다시 누르면 닫거나 유지
        if (currentPanel == target) return;

        // 1. 모든 패널 끄기
        upgradePanel.gameObject.SetActive(false);
        tagPanel.SetActive(false);
        breakthroughPanel.SetActive(false);

        // 2. 선택한 패널만 켜기
        switch (target)
        {
            case CharacterPanelState.Upgrade:
                upgradePanel.gameObject.SetActive(true);
                if (currentSelectedInfo != null) upgradePanel.Init(currentSelectedInfo);
                break;
            case CharacterPanelState.Tag:
                tagPanel.SetActive(true);
                break;

            case CharacterPanelState.Breakthrough:
                breakthroughPanel.SetActive(true);
                break;
        }

        currentPanel = target;

        // 3. 패널 내용 갱신 (현재 선택된 캐릭터 정보 전달)
        RefreshCurrentPanel();
    }

    private void RefreshCurrentPanel()
    {
        // 여기서 현재 보고 있는 캐릭터(이그니 등)의 데이터를 
        // 켜진 패널의 스크립트에게 전달합니다.
    }

    public void OnClickUpgradeButton()
    {
        // 1. 패널을 먼저 활성화
        upgradePanel.gameObject.SetActive(true);

        // 2. 패널의 Init을 호출하면서 현재 선택된 캐릭터 데이터를 전달!
        // selectedCharacter는 현재 재우 님이 보고 있는 그 캐릭터 데이터여야 합니다.
        upgradePanel.Init(currentSelectedInfo);
    }

    private void RefreshUI()
    {
        if (currentSelectedInfo == null) return;

        // 레벨 텍스트 갱신
        levelText.text = $"Lv. {currentSelectedInfo.currentLevel}";

        // (선택) 레벨업 시 텍스트가 커졌다 작아지는 효과 (DOTween)
        levelText.transform.DOKill();
        levelText.transform.DOScale(1.2f, 0.1f).OnComplete(() =>
        {
            levelText.transform.DOScale(1f, 0.1f);
        });
    }

    public void ReturnToList()
    {
        // 1. 현재 어떤 패널이 열려있든 모두 비활성화
        upgradePanel.gameObject.SetActive(false);
        tagPanel.SetActive(false);
        breakthroughPanel.SetActive(false);

        // 2. 기본 상태(보통 캐릭터 리스트만 보이거나 안내 문구가 있는 패널) 활성화
        if (defaultPanel != null) defaultPanel.SetActive(true);

        // 3. 현재 상태를 Default로 변경
        currentPanel = CharacterPanelState.Default;

        Debug.Log("리스트 화면으로 돌아갑니다.");
    }
}
