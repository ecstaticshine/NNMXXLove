using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarUI : MonoBehaviour
{

    [Header("Player  Info")]
    public TMP_Text playerNameText;

    [Header("Player Level")]
    public TMP_Text levelText;
    public Slider expSlider;

    [Header("Stamina")]
    public TMP_Text staminaText;
    public Slider staminaSlider;
    public Button addStaminaButton;

    [Header("Currency")]
    public TMP_Text coinText;
    public Button addCoinButton;

    public TMP_Text diamondText;
    public Button addDiamondButton;

    void OnEnable()
    {
        // 데이터가 변경될 때마다 UI를 갱신하도록 이벤트 구독
        if (DataManager.Instance != null)
            DataManager.Instance.OnDataChanged += RefreshUI;

        RefreshUI();
    }

    void OnDisable()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.OnDataChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        UserData data = DataManager.Instance.userData;

        // 1. 레벨 & 경험치 (현재 UserData에 레벨/경험치가 있다면 연결)
        // levelText.text = data.currentLevel.ToString();
        // expSlider.value = (float)data.currentExp / DataManager.Instance.GetRequiredExp(data.currentLevel);

        // 2. 스태미나 (현재/최대값)
        staminaText.text = $"{data.stamina} / 120"; // 120은 최대치 예시
        staminaSlider.value = data.stamina / 120f;

        // 3. 재화
        coinText.text = data.gold.ToString("N0"); // N0: 천단위 콤마
        diamondText.text = data.diamond.ToString("N0");
    }

    // 버튼 연결용 (인스펙터에서 버튼 클릭 이벤트에 할당)
    public void OnClickAddStamina() { /* 결제나 아이템 사용 팝업 띄우기 */ }
    public void OnClickAddGold() { /* 상점 연결 */ }

    public void OnClickAddDiamond()
    {
        //결제창
    }
}
