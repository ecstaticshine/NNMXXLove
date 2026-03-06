using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBTPanel : MonoBehaviour
{
    [Header("UI References")]
    public ItemIcon pieceIcon;         // 캐릭터 조각 아이콘
    public TMP_Text tierText;          // "현재 등급 : PL"
    public TMP_Text btStatusText;      // "현재 돌파 : 3 / 7"
    public TMP_Text totalInfoText;     // "총 획득 조각 : 15개"
    public Button upgradeButton;
    public TMP_Text upgradeButtonText;     // "강화하기"

    private CharacterInfo currentSelectedInfo;

    private string labelTier;
    private string labelBT;
    private string labelPiece;
    private string labelAmount;

    // 패널이 켜질 때 호출될 함수
    public void Init(CharacterInfo character)
    {
        currentSelectedInfo = character;
        RefreshUI();
    }

    private void Awake()
    {
        labelTier = DataManager.Instance.GetLocalizedText("Character_Rarity"); // 예: "등급" 또는 "Rarity"
        labelBT = DataManager.Instance.GetLocalizedText("Character_BT");     // 예: "돌파 단계"
        labelPiece = DataManager.Instance.GetLocalizedText("Character_Piece"); // 예: "보유 조각"
        labelAmount = DataManager.Instance.GetLocalizedText("Character_Amount"); // 예: "개수"
        upgradeButtonText.text = DataManager.Instance.GetLocalizedText("Character_Upgrade_Btn"); // 강화하기
    }

    public void RefreshUI()
    {
        if (currentSelectedInfo == null) return;
        int totalPt = currentSelectedInfo.TotalPoint; // 예: TL(14) + 0돌파 = 14점

        // 3. 점수에 따른 현재 등급 및 단계 계산
        Rarity displayRarity;
        int displayStep;

        if (totalPt >= 21)
        {
            displayRarity = Rarity.EL;
            displayStep = 7; // MAX
        }
        else if (totalPt >= 14)
        {
            displayRarity = Rarity.TL;
            displayStep = totalPt - 14;
        }
        else if (totalPt >= 7)
        {
            displayRarity = Rarity.PL;
            displayStep = totalPt - 7;
        }
        else
        {
            displayRarity = Rarity.L;
            displayStep = totalPt;
        }

        //데이터 동기화
        currentSelectedInfo.currentRarity = displayRarity;
        currentSelectedInfo.currentBreakthrough = displayStep;


        // 4. UI 반영 (자간 보정은 로컬라이징 키값에 포함하거나 space 태그 활용)
        if (tierText != null)
        {
            string color = GetTierColor(displayRarity);
            tierText.text = $"{labelTier} : <color={color}>{displayRarity}</color>";
        }

        if (btStatusText != null)
        {
            string stepStr = (displayRarity == Rarity.EL) ? "MAX" : $"{displayStep} / 7";
            btStatusText.text = $"{labelBT} : {stepStr}";
        }

        // 보유 조각은 '강화 재료'로서의 개수만 표시
        int pieceID = currentSelectedInfo.unitID + 5000;
        int ownedCount = DataManager.Instance.GetOwnedItem(pieceID)?.count ?? 0;

        if (totalInfoText != null)
            totalInfoText.text = $"{labelPiece} : {ownedCount}";

        if (pieceIcon != null)
            pieceIcon.Setup(DataManager.Instance.GetItemData(pieceID), ownedCount);

        if (upgradeButton != null) // 버튼 컴포넌트 연결 필요
        {
            bool canUpgrade = (currentSelectedInfo.TotalPoint < 21) && (ownedCount > 0);
            upgradeButton.interactable = canUpgrade;
        }

    }

    public void OnClickUpgrade()
    {
        if (currentSelectedInfo == null) return;

        if (currentSelectedInfo.TotalPoint >= 21)
        {
            Debug.Log("이미 최대 등급입니다.");
            return;
        }

        // 1. 강화 전 데이터 기억 (연출용)
        Rarity prevRarity = currentSelectedInfo.currentRarity;
        int prevStep = currentSelectedInfo.currentBreakthrough;
        int prevOwned = DataManager.Instance.GetOwnedItem(currentSelectedInfo.unitID + 5000)?.count ?? 0;

        // 2. 실제 데이터 강화 실행
        DataManager.Instance.BreakthroughCharacter(currentSelectedInfo.unitID);

        // 3. 연출 시작
        StartCoroutine(PlayUpgradeEffect_Co(prevRarity, prevStep, prevOwned));
    }

    private string GetTierColor(Rarity currentRarity)
    {
        switch (currentRarity)
        {
            case Rarity.PL: return "#9b59b6"; // 보라
            case Rarity.TL:return "#f1c40f"; // 금색
            case Rarity.EL:return "#ffffff"; // 흰색
            default: return "#3498db"; // 파랑
        }
    }

    // 태생 등급별 기본 점수 반환
    private int GetTierOffset(Rarity currentRarity)
    {
        switch (currentRarity)
        {
            case Rarity.PL: return 7;
            case Rarity.TL: return 14;
            case Rarity.EL: return 21;
            default: return 0; // L
        }
    }


    private IEnumerator PlayUpgradeEffect_Co(Rarity oldRarity, int oldStep, int oldOwned)
    {
        // 강화 후 데이터 가져오기
        int newStep = currentSelectedInfo.currentBreakthrough;
        Rarity newRarity = currentSelectedInfo.currentRarity;
        int newOwned = DataManager.Instance.GetOwnedItem(currentSelectedInfo.unitID + 5000)?.count ?? 0;

        // [연출 1] 등급 텍스트: PL -> <color=gold>TL</color> (TL만 반짝)
        if (tierText != null)
        {
            string colorTag = GetTierColor(newRarity);
            if (oldRarity != newRarity)
                tierText.text = $"{labelTier} : {oldRarity} -> <color={colorTag}><b>{newRarity}</b></color>";
            else
                tierText.text = $"{labelTier} : <color={colorTag}>{newRarity}</color>";
        }

        // [연출 2] 돌파 단계: 3 -> <color=green>4</color>
        if (btStatusText != null)
        {
            btStatusText.text = $"{labelBT} : {oldStep} -> <color=#00FF00><b>{newStep}</b></color> / 7";
        }

        // [연출 3] 보유 조각: 15 -> <color=red>14</color>
        if (totalInfoText != null)
        {
            totalInfoText.text = $"{labelPiece} : {oldOwned} -> <color=#FF0000><b>{newOwned}</b></color>{labelAmount}";
        }

        // 1초 동안 반짝거리는 느낌 (폰트 크기를 키웠다 줄였다 하거나, 텍스트를 깜빡이게 할 수 있음)
        float elapsed = 0f;
        while (elapsed < 1.0f)
        {
            float alpha = Mathf.PingPong(Time.time * 10, 1.0f); // 빠르게 깜빡임
                                                                // 여기서 특정 컬러의 알파값을 조절하거나 느낌만 줌
            yield return null;
            elapsed += Time.deltaTime;
        }

        // 최종 UI로 깔끔하게 정리
        RefreshUI();
    }
}
