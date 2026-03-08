using DG.Tweening;
using System.Collections.Generic;
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
    [SerializeField] private GameObject descArea;
    [SerializeField] private TMP_Text descText;

    [Header("Stats (Battle Mode)")]
    [SerializeField] private GameObject stats;
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text spdText;
    [SerializeField] private GameObject shieldArea;
    [SerializeField] private TMP_Text shieldInfoText;
    [SerializeField] private GameObject synergyArea;
    [SerializeField] private TMP_Text synergyText;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void ResetUI()
    {
        nameText.text = string.Empty;
        descText.text = string.Empty;
        synergyText.text = string.Empty;

        if (synergyArea != null) synergyArea.SetActive(false);

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
        closeArea.SetActive(true);
        popupArea.SetActive(true);

        // PopupArea만 살짝 커지면서 나타나는 연출
        if (popupArea != null)
        {
            popupArea.transform.localScale = Vector3.one * 0.8f;
            popupArea.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    // 1. 아이템 정보를 위한 Setup
    public void Setup(ItemData itemData)
    {
        ResetUI();

        if (descArea != null) descArea.SetActive(true);
        if (stats != null) stats.SetActive(false);

        nameText.text = DataManager.Instance.GetLocalizedText(itemData.itemNameKey);
        descText.text = DataManager.Instance.GetLocalizedText(itemData.descriptionKey);
        if (itemData.itemID >= 4000)
        {
            descText.text += $"\n{itemData.tagAbilityName}";
        }

        mainIcon.sprite = itemData.itemIcon;
        mainIcon.color = Color.white;

        OpenAnimation();
    }
    // 2, 스테미나 툴팁
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

        stats.SetActive(false);

        OpenAnimation();
    }


    public void OpenUnitBattleDetail(Unit unit)
    {
        if (unit == null || unit.data == null) return;
        // 배틀 매니저가 로딩 중이거나 없을 경우 대비
        if (BattleManager.instance == null) return;

        ResetUI(); // 기존 데이터 지우기

        // 1. 유닛 이름 및 초상화 설정
        descArea.SetActive(false);
        synergyArea.SetActive(true);
        stats.SetActive(true);

        nameText.text = DataManager.Instance.GetLocalizedText(unit.data.unitNameKey);
        mainIcon.sprite = unit.data.unitPortrait;
        mainIcon.color = Color.white;

        // 2. 공격력 계산 (기본값 vs 보너스 분리)
        int totalAtk = unit.GetCurrentAttack();
        int bonusAtk = totalAtk - unit.data.baseAttack;

        atkText.text = bonusAtk > 0
            ? $"{unit.data.baseAttack} <color=#00FF00>(+{bonusAtk})</color>"
            : $"{unit.data.baseAttack}";
        hpText.text = $"<color=#FF5555>{unit.GetCurrentHP()}</color> / {unit.GetMaxHP()}";
        spdText.text = $"{unit.GetCurrentSpeed()}";

        // 3. 쉴드 정보 (특수 UI)
        if (unit.shieldCount > 0)
        {
            shieldArea.SetActive(true);
            string shieldFormat = DataManager.Instance.GetLocalizedText("shield_info_format");
            shieldInfoText.text = string.Format(shieldFormat, unit.shieldCount, unit.shieldAmount);
        }
        else
        {
            shieldArea.SetActive(false);
        }

        UpdateSynergyText(unit);

        // 팝업 활성화 및 애니메이션
        stats.SetActive(true);
        OpenAnimation();
    }

    private void UpdateSynergyText(Unit unit)
    {
        string synergyContent = "";
        var eff = unit.data.isEnemy
            ? BattleManager.instance.enemySynergy.currentEffect
            : BattleManager.instance.playerSynergy.currentEffect;

        List<string> myTags = unit.GetSynergyTags();

        //Direct
        if (myTags.Contains("Direct") && eff.directDamageMult > 0)
        {
            string format = DataManager.Instance.GetLocalizedText("synergy_direct");
            synergyContent += string.Format(format, eff.directDamageMult * 100) + "\n";
        }

        //Dot
        if (myTags.Contains("Dot") && eff.dotExtraHits > 0)
        {
            string format = DataManager.Instance.GetLocalizedText("synergy_dot");
            synergyContent += string.Format(format, eff.dotExtraHits) + "\n";
        }

        // Splash
        if (myTags.Contains("Splash") && eff.splashBonus > 0)
        {
            string format = DataManager.Instance.GetLocalizedText("synergy_splash");
            synergyContent += string.Format(format, eff.splashBonus * 100) + "\n";
        }


        if (string.IsNullOrEmpty(synergyContent))
        {
            synergyText.text = DataManager.Instance.GetLocalizedText("synergy_none");
            // 값 예시: "활성화된 시너지 없음"
        }
        else
        {
            synergyText.text = synergyContent;
        }
    }


    public void OpenUnitStatDetail(UnitData data, CharacterInfo info = null)
    {
        ResetUI();

        descArea.SetActive(true);
        if (synergyArea != null) synergyArea.SetActive(false);
        stats.SetActive(true);

        nameText.text = DataManager.Instance.GetLocalizedText(data.unitNameKey);
        descText.text = DataManager.Instance.GetLocalizedText(data.descriptionKey);
        mainIcon.sprite = data.unitPortrait;
        mainIcon.color = Color.white;

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
