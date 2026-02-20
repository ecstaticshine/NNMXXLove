using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class UnitIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    public UnitData GetUnitData()
    {
        // 이미 선언되어 있는 currentUnitData를 반환합니다.
        return currentUnitData;
    }

    public void SetUnitIcon(UnitData data, int level)
    {
        if (data == null) return;

        currentUnitData = data;

        // SO에서 초상화 가져오기
        unitIcon.sprite = data.unitPortrait;

        // 레벨 표시
        levelText.text = $"{level}";

        // 등급(Rarity)에 따른 연출 처리
        UpdateRarityUI(data.isEnemy, data.rarity);
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
                Rarity.L => "#7A6B6B", 
                Rarity.PL => "#6B5471",
                Rarity.TL => "#854552",
                Rarity.EL => "#4A363E",
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
        if (currentUnitData != null)
        {
            // 유닛 이름과 특이사항(예: 등급 설명 등) 표시
            string translatedName = DataManager.Instance.GetLocalizedText(currentUnitData.unitNameKey);
            string translatedDesc = DataManager.Instance.GetLocalizedText(currentUnitData.descriptionKey);

            TooltipManager.Instance.ShowTooltip(translatedName, translatedDesc);
        }
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
        // 드래그 시작 시 마우스를 따라갈 가짜 이미지 생성
        ghostIcon = new GameObject("GhostIcon");
        ghostIcon.transform.SetParent(transform.root);
        Image ghostImg = ghostIcon.AddComponent<Image>();
        ghostImg.sprite = currentUnitData.unitPortrait; // SO의 초상화 사용
        ghostImg.raycastTarget = false; // 드롭 감지 방해 방지

        // 크기 조절
        RectTransform rect = ghostIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 150);

    }

    public void OnDrag(PointerEventData eventData)
    {
        // 가짜 이미지가 마우스 위치를 따라가게 함
        ghostIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 종료 시 가짜 이미지 파괴
        Destroy(ghostIcon);
    }

}
