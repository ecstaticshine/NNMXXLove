using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemIcon : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public Image frameImage;
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

    /// <summary>
    /// 슬롯에 데이터를 채웁니다.
    /// </summary>
    public void Setup(ItemData data, int count)
    {
        if (data == null) return;

        // 1. 아이콘 설정
        iconImage.sprite = data.itemIcon;

        // 2. 개수 표시 (재화나 재료는 개수를 표시, 태그나 스킨은 1개면 생략 가능)
        if (count > 1) countText.text = count.ToString();
        else countText.text = "";

        // 3. 등급에 따른 테두리 색상 변경
        int colorIdx = Mathf.Clamp(data.grade - 1, 0, gradeColors.Length - 1);
        frameImage.color = gradeColors[colorIdx];

        // 4. 초기 상태는 획득 안 함으로 설정
        SetObtained(false);
    }

    /// <summary>
    /// 첫 클리어 보상을 이미 받았을 때 호출합니다.
    /// </summary>
    public void SetObtained(bool isObtained)
    {
        if (obtainedOverlay != null)
            obtainedOverlay.SetActive(isObtained);
    }

    public void SetChanceText(float chance)
    {
        if (chanceText == null) return;

        // chance가 1.0(100%)보다 작을 때만 표시하면 더 깔끔하겠죠?
        if (chance < 1.0f)
        {
            chanceText.gameObject.SetActive(true);
            // 0.3 -> 30% 이런 식으로 변환해요.
            chanceText.text = $"{(chance * 100f):0}%";
        }
        else
        {
            chanceText.gameObject.SetActive(false);
        }
    }
}
