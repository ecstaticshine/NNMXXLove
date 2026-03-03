using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class UnitIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private Image backboard;
    [SerializeField] private GameObject bossMark;

    [Header("Ally Frames (Love Series)")]
    public Sprite[] allyFrames;  // L, PL, TL, EL 순서대로

    [Header("Enemy Frames (Level Series)")]
    public Sprite[] enemyFrames; // L, PL, TL, EL 순서대로

    [Header("Growth UI (Optional)")]
    public Slider expSlider;       // 결과창에서만 사용
    public GameObject levelUpBadge; // 결과창에서만 사용

    private UnitData currentUnitData;
    private CharacterInfo characterInfo;

    private bool isPlaced = false; // 배치에 사용

    public UnitData GetUnitData()
    {
        // 이미 선언되어 있는 currentUnitData를 반환합니다.
        return currentUnitData;
    }

    public void SetUnitIcon(UnitData data, CharacterInfo info)
    {
        int totalPt = info.TotalPoint;
        Rarity currentRarity;

        if (totalPt >= 21) currentRarity = Rarity.EL;
        else if (totalPt >= 14) currentRarity = Rarity.TL;
        else if (totalPt >= 7) currentRarity = Rarity.PL;
        else currentRarity = Rarity.L;

        int rarityIndex = (int)currentRarity;

        this.currentUnitData = data;
        this.characterInfo = info;

        // SO에서 초상화 가져오기
        unitIcon.sprite = data.unitPortrait;

        // 레벨 표시
        levelText.text = $"{info.currentLevel}";

        // 등급(Rarity)에 따른 연출 처리
        UpdateRarityUI(data.isEnemy, currentRarity);
    }

    public bool IsPlaced()
    {
        return this.isPlaced;
    }

    public void SetPlaced(bool placed)
    {
        isPlaced = placed;

        // 이미 배치된 아이콘은 반투명한 회색으로 만듦
        if (backboard != null)
        {
            backboard.color = placed ? new Color(0.3f, 0.3f, 0.3f, 0.5f) : Color.white;
        }
        // 글자나 아이콘도 투명도 조절 가능
        unitIcon.color = placed ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
    }

    public void SetUnitIcon(UnitData data, int level, int breakthrough = 0)
    {
        if (data == null) return;

        this.currentUnitData = data;
        // 전투 중에는 DB의 CharacterInfo 대신 Unit이 가진 레벨 정보를 바로 사용
        this.unitIcon.sprite = data.unitPortrait;
        this.levelText.text = $"{level}";

        int baseOffset = data.rarity switch
        {
            Rarity.L => 0,
            Rarity.PL => 7,
            Rarity.TL => 14,
            Rarity.EL => 21,
            _ => 0
        };

        int totalPt = baseOffset + breakthrough;
        Rarity currentRarity;

        if (totalPt >= 21) currentRarity = Rarity.EL;
        else if (totalPt >= 14) currentRarity = Rarity.TL;
        else if (totalPt >= 7) currentRarity = Rarity.PL;
        else currentRarity = Rarity.L;

        // 등급 UI 업데이트 (isEnemy 정보 포함)
        UpdateRarityUI(data.isEnemy, currentRarity);
    }

    private void UpdateRarityUI(bool isEnemy, Rarity rarity)
    {
        // 보스 마크는 EL(Elite Level)일 때만 활성화
        if (bossMark != null)
        {
            bossMark.SetActive(rarity == Rarity.EL);
        }

        // 프레임 스프라이트 바꾸기
        int rarityIndex = (int)rarity;

        Sprite[] targetFrames = isEnemy ? enemyFrames : allyFrames;

        if (targetFrames != null && rarityIndex < targetFrames.Length)
        {
            rarityFrame.sprite = targetFrames[rarityIndex];
        }

        // 백보드 색상 적용
        if (backboard != null)
        {
            backboard.color = GetBoardColor(isEnemy, rarity);
        }
    }

    private Color GetBoardColor(bool isEnemy, Rarity rarity)
    {
        string hex;

        if (isEnemy)
        {
            hex = rarity switch
            {
                Rarity.L => "#5F6267",
                Rarity.PL => "#546071",
                Rarity.TL => "#533960",
                Rarity.EL => "#37363E",
                _ => "#5F6267"
            };

        }
        else
        {
            hex = rarity switch
            {
                Rarity.L => "#93A9BD",
                Rarity.PL => "#B399D4",
                Rarity.TL => "#D4AF37",
                Rarity.EL => "#F5F5F5",
                _ => "#5F6267"
            };
        }

        if (ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
        return Color.gray; // 예외 시 기본 회색
    }

    public void SetExpUI(float currentExp, float maxExp, bool isLevelUp)
    {
        if (expSlider != null)
        {
            expSlider.gameObject.SetActive(true);
            expSlider.maxValue = maxExp;
            // DOTween으로 부드럽게 차오르는 연출
            expSlider.DOValue(currentExp, 1f).SetEase(Ease.OutCubic);
        }

        if (levelUpBadge != null)
        {
            levelUpBadge.SetActive(isLevelUp);
            if (isLevelUp) levelUpBadge.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentUnitData == null) return;
        if (DataManager.Instance == null || TooltipManager.Instance == null) return;

        // 위 조건을 통과했을 때만 툴팁 로직 실행
        string translatedName = DataManager.Instance.GetLocalizedText(currentUnitData.unitNameKey);
        string translatedDesc = DataManager.Instance.GetLocalizedText(currentUnitData.descriptionKey);

        TooltipManager.Instance.ShowTooltip(translatedName, translatedDesc);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 인스턴스가 존재하고, 실제 게임 오브젝트가 파괴되지 않았을 때만 호출
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    private GameObject ghostIcon; // 드래그 시 마우스를 따라다닐 가짜 이미지

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 데이터가 없거나 배치된 유닛이면 드래그 불가
        if (currentUnitData == null || isPlaced)
        {
            return;
        }

        // 1. 고스트 아이콘 생성 및 캔버스 설정
        ghostIcon = new GameObject("GhostIcon");

        // 2. 드래그할 때는 제일 상위 캔버스 앞으로 오게 해야지 드래그해서 드롭할 때까지 남음.
        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null)
        {
            // 좌표 안 튀게
            ghostIcon.transform.SetParent(parentCanvas.transform, false);
            // 해당 캔버스 안에서 가장 앞으로 오게 하고 드롭 끝나면 어차피 사라질 운명
            ghostIcon.transform.SetAsLastSibling();
        }

        // 3. UI 렌더링을 위한 필수 컴포넌트 추가
        ghostIcon.AddComponent<CanvasRenderer>(); // 이미지 렌더링 필수 컴포넌트
        Image ghostImg = ghostIcon.AddComponent<Image>();

        // 4. 이미지 할당 및 투명도 조절
        ghostImg.sprite = currentUnitData.unitBattleSD;
        ghostImg.color = new Color(1, 1, 1, 0.7f); // 70% 투명도
        ghostImg.raycastTarget = false;

        // 5. RectTransform 크기 및 위치 강제 초기화
        RectTransform rect = ghostIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 150); // 배틀 SD 비율에 맞춰 조정
        rect.localPosition = Vector3.zero;

        ghostIcon.transform.position = eventData.position;

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null)
        {
            // 가짜 이미지가 마우스 위치를 따라가게 함
            ghostIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostIcon != null)
        {
            // 드래그 종료 시 가짜 이미지 파괴
            Destroy(ghostIcon);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 데이터가 없으면 무시
        if (currentUnitData == null) return;
        if (GlobalUIManager.Instance.currentState == SceneState.CharacterList || GlobalUIManager.Instance.currentState == SceneState.Placement)
        {
            DataManager.Instance.SetSelectedCharacter(currentUnitData, characterInfo);
        }
        else
        {
            Unit battleUnit = FindBattleUnit();

            if (battleUnit != null)
                DetailInfoPopup.Instance.OpenUnitBattleDetail(battleUnit);
            else
                DetailInfoPopup.Instance.OpenUnitStatDetail(currentUnitData);
        }

        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }

    private Unit FindBattleUnit()
    {
        // BattleManager의 딕셔너리에서 이 아이콘의 UnitData와 일치하는 Unit을 찾음
        foreach (var unit in BattleManager.instance.playerTurnOrder)
        {
            if (unit.data == currentUnitData) return unit;
        }
        foreach (var unit in BattleManager.instance.enemyTurnOrder)
        {
            if (unit.data == currentUnitData) return unit;
        }
        return null;
    }

    private void ShowUnitDetailPopup(UnitData unitData)
    {
        DetailInfoPopup.Instance.Setup(unitData);
    }

    private void ShowUnitDetailWithStats(Unit unit)
    {
        if (unit == null || unit.data == null) return;

        // 1. 이름 및 기본 정보
        string unitName = DataManager.Instance.GetLocalizedText(unit.data.unitNameKey);

        // 2. 실시간 스탯 계산 (BattleManager 로직 활용)
        // 기본값과 현재 적용된 최종값의 차이를 버프 수치로 계산
        int currentAtk = unit.GetCurrentAttack();
        int baseAtk = unit.data.baseAttack; // 또는 성장치가 반영된 기본 공격력
        int atkBuff = currentAtk - baseAtk;

        int currentSpd = unit.GetCurrentSpeed();
        int baseSpd = unit.data.baseSpeed;
        int spdBuff = currentSpd - baseSpd;

        // 3. 현재 체력 상태 (HP는 최대치 대비 현재치 표시가 중요)
        int curHp = unit.GetCurrentHP();
        int maxHp = unit.GetMaxHP();

        // 4. 리치 텍스트 구성
        // 팁: 가독성을 위해 항목별로 컬러를 지정하면 좋습니다.
        string hpString = $"<color=#FF5555>HP</color> : {curHp} / {maxHp}";

        string atkString = $"<color=#FFCC00>ATK</color> : {baseAtk}";
        if (atkBuff > 0) atkString += $" <color=#00FF00>(+{atkBuff})</color>";
        else if (atkBuff < 0) atkString += $" <color=#FF0000>({atkBuff})</color>";

        string spdString = $"<color=#55CCFF>SPD</color> : {baseSpd}";
        if (spdBuff > 0) spdString += $" <color=#00FF00>(+{spdBuff})</color>";

        // 5. 시너지 및 설명 합치기
        string description = DataManager.Instance.GetLocalizedText(unit.data.descriptionKey);
        string fullContent = $"{hpString}\n{atkString}\n{spdString}\n\n{description}";

        // 6. 팝업 표시
        // 이미 DetailInfoPopup에 OpenUnitBattleDetail을 만드셨다면 그걸 호출하는 게 가장 좋습니다.
        DetailInfoPopup.Instance.OpenUnitBattleDetail(unit);
    }
}
