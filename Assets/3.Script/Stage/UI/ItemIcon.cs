using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image iconImage;
    public Image frameImage;
    public GameObject display;
    public TMP_Text countText;
    public TMP_Text chanceText;

    [Header("Obtained States")]
    public GameObject obtainedOverlay; // 어두운 패널 + 체크표시 묶음

    // 등급별 색상 (인스펙터에서 설정하거나 코드로 정의)
    private Color[] gradeColors = {
        Color.white,            // 1성
        Color.green,            // 2성
        new Color(0.2f, 0.6f, 1f), // 3성 (파랑)
        new Color(0.7f, 0.3f, 1f), // 4성 (보라)
        new Color(1f, 0.6f, 0f)    // 5성 (금색)
    };

    private ItemData currentItemData; // 현재 설정된 데이터 저장용

    /// <summary>
    /// 슬롯에 데이터를 채우기
    /// </summary>
    public void Setup(ItemData data, int count, float chance = 100f)
    {
        if (data == null) return;

        currentItemData = data;

        // 1. 아이콘 설정
        iconImage.sprite = data.itemIcon;

        // 초기 설정
        countText.gameObject.SetActive(false);
        chanceText.gameObject.SetActive(false);
        bool showDisplay = false;

        if (data.itemID == 1003)
        {
            showDisplay = true;
            countText.gameObject.SetActive(true);
            countText.text = $"+{count}";
        }else if (chance < 100f)
        {
            showDisplay = true;
            chanceText.gameObject.SetActive(true);
            chanceText.text = $"{chance:0}%";
        }

        // 2. 개수 표시 (재화나 재료는 개수를 표시, 태그나 스킨은 1개면 생략 가능)
        else if (count > 1)
        {
            showDisplay = true;
            countText.gameObject.SetActive(true);
            countText.text = count.ToString();
        }
        else
        {
            showDisplay = false;
        }

        // 3. 등급에 따른 테두리 색상 변경
        int colorIdx = Mathf.Clamp(data.grade - 1, 0, gradeColors.Length - 1);
        frameImage.color = gradeColors[colorIdx];

        // 4. 초기 상태는 획득 안 함으로 설정
        display.SetActive(showDisplay);
        SetObtained(false);
    }

    /// <summary>
    /// 첫 클리어 보상을 이미 받았을 때 호출
    /// </summary>
    public void SetObtained(bool isObtained)
    {
        if (obtainedOverlay != null)
            obtainedOverlay.SetActive(isObtained);
    }

    public void ShowChanceText(bool isVisible)
    {
        if (chanceText != null)
        {
            chanceText.gameObject.SetActive(isVisible);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItemData != null)
        {
            // DataManager를 통해 키값으로 실제 텍스트 추출
            string translatedName = DataManager.Instance.GetLocalizedText(currentItemData.itemNameKey);
            string translatedDesc = DataManager.Instance.GetLocalizedText(currentItemData.descriptionKey);

            Debug.Log(translatedName);
            Debug.Log(translatedDesc);

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
}
