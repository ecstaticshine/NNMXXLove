using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DetailInfoPopup : MonoBehaviour
{
    public static DetailInfoPopup Instance = null;

    [Header("UI Components")]
    [SerializeField] private GameObject closeArea;
    [SerializeField] private GameObject popupArea;
    [SerializeField] private Image mainIcon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text spdText;
    [SerializeField] private GameObject shieldArea;
    [SerializeField] private TMP_Text shieldInfoText;
    [SerializeField] private TMP_Text synergyText;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    // 1. 아이템 정보를 위한 Setup
    public void Setup(ItemData itemData)
    {
        ResetUI();
        nameText.text = DataManager.Instance.GetLocalizedText(itemData.itemNameKey);
        descText.text = DataManager.Instance.GetLocalizedText(itemData.descriptionKey);
        mainIcon.sprite = itemData.itemIcon;
        mainIcon.color = Color.white;
        closeArea.SetActive(true);
        popupArea.SetActive(true);
        OpenAnimation();
    }

    // 2. 유닛 정보를 위한 Setup
    public void Setup(UnitData unitData)
    {
        ResetUI();
        nameText.text = DataManager.Instance.GetLocalizedText(unitData.unitNameKey);
        descText.text = DataManager.Instance.GetLocalizedText(unitData.descriptionKey);
        mainIcon.sprite = unitData.unitPortrait;
        mainIcon.color = Color.white;
        closeArea.SetActive(true);
        popupArea.SetActive(true);
        OpenAnimation();
    }

    public void SetupCustom(string title, string desc, Sprite customIcon = null)
    {
        ResetUI();
        nameText.text = DataManager.Instance.GetLocalizedText(title);
        descText.text = DataManager.Instance.GetLocalizedText(desc);

        if (customIcon != null)
        {
            mainIcon.gameObject.SetActive(true); // 아이콘 오브젝트 활성화
            mainIcon.sprite = customIcon;
            mainIcon.color = Color.white;
        }
        else
        {
            // 아이콘이 없으면 이미지 컴포넌트나 오브젝트를 끕니다.
            mainIcon.gameObject.SetActive(false);
        }

        closeArea.SetActive(true);
        popupArea.SetActive(true);
        OpenAnimation();
    }

    private void ResetUI()
    {
        nameText.text = string.Empty;
        descText.text = string.Empty;

        if (mainIcon != null)
        {
            mainIcon.sprite = null;
            mainIcon.color = new Color(1, 1, 1, 0); // 투명하게
        }
    }

    public void Close()
    {

        closeArea.SetActive(false);
        popupArea.SetActive(false);

    }

    private void OpenAnimation()
    {
        popupArea.SetActive(true);

        // PopupArea만 살짝 커지면서 나타나는 연출
        if (popupArea != null)
        {
            popupArea.transform.localScale = Vector3.one * 0.8f;
            popupArea.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack);
        }
    }
    public void OpenUnitBattleDetail(Unit unit)
    {
        if (unit == null || unit.data == null) return;

        // 배틀 매니저가 로딩 중이거나 없을 경우 대비
        if (BattleManager.instance == null) return;

        ResetUI(); // 기존 데이터 지우기

        // 1. 유닛 이름 및 초상화 설정
        nameText.text = DataManager.Instance.GetLocalizedText(unit.data.unitNameKey);
        mainIcon.sprite = unit.data.unitPortrait;

        // 2. 공격력 계산 (기본값 vs 보너스 분리)
        int baseAtk = unit.data.baseAttack; // 레벨업 반영된 기본치
        int bonusAtk = unit.GetCurrentAttack() - baseAtk;

        atkText.text = $"{baseAtk} <color=#00FF00>+ {bonusAtk}</color>";
        hpText.text = $"{unit.GetCurrentHP()} / {unit.GetMaxHP()}";
        spdText.text = $"{unit.GetCurrentSpeed()}";

        // 3. 쉴드 정보 (특수 UI)
        if (unit.shieldCount > 0)
        {
            shieldArea.SetActive(true);
            shieldInfoText.text = $"남은 횟수: {unit.shieldCount}회\n방어 상한: {unit.shieldAmount}";
        }
        else
        {
            shieldArea.SetActive(false);
        }

        // 4. 적용 중인 시너지/버프 텍스트 생성
        string synergyDesc = "";
        var eff = unit.data.isEnemy ? BattleManager.instance.enemySynergy.currentEffect : BattleManager.instance.playerSynergy.currentEffect;

        if (unit.data.defaultTag == "Direct" && eff.directDamageMult > 0)
            synergyDesc += $"[시너지] 직접 공격 피해 {eff.directDamageMult * 100}% 증가\n";
        if (unit.data.defaultTag == "Dot" && eff.dotExtraHits > 0)
            synergyDesc += $"[시너지] 공격 시 {eff.dotExtraHits}회 추가 타격\n";

        synergyText.text = synergyDesc;

        OpenAnimation();
    }

    public void OpenUnitStatDetail(UnitData data, CharacterInfo info = null)
    {
        ResetUI();
        nameText.text = DataManager.Instance.GetLocalizedText(data.unitNameKey);
        mainIcon.sprite = data.unitPortrait;

        // CharacterInfo가 있다면 레벨 반영, 없으면 데이터 시트의 기본값
        int lv = (info != null) ? info.currentLevel : 1;
        atkText.text = $"{data.baseAttack}"; // 기본 공격력만 표시
        hpText.text = $"{data.baseHp}";
        spdText.text = $"{data.baseSpeed}";

        shieldArea.SetActive(false);
        descText.text = DataManager.Instance.GetLocalizedText(data.descriptionKey);

        OpenAnimation();
    }
}
