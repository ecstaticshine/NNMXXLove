using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SynergyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject directUI; // 빨강
    [SerializeField] private GameObject splashUI; // 파랑
    [SerializeField] private GameObject dotUI;    // 초록

    public static SynergyUI instance = null;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void UpdateUI(int direct, int splash, int dot)
    {
        SetSynergyLevel(directUI, direct);
        SetSynergyLevel(splashUI, splash);
        SetSynergyLevel(dotUI, dot);
    }

    private void SetSynergyLevel(GameObject uiObject, int count)
    {
        if (uiObject == null) return;

        // 시너지 단계 계산 (9명->3, 6명->2, 3명->1, 그 외 0)
        int level;
        if (count >= 9) level = 3;
        else if (count >= 6) level = 2;
        else if (count >= 3) level = 1;
        else level = 0;

        // 자식에 있는 TMP_Text 찾기
        TMP_Text levelText = uiObject.GetComponentInChildren<TMP_Text>();

        if (levelText != null)
        {
            levelText.text = level.ToString();

            // 레벨이 1이라도 있으면 활성화
            if (level > 0)
            {
                uiObject.SetActive(true); // 시너지가 활성화되면 켬
                levelText.color = Color.white;
            }
            else
            {
                uiObject.SetActive(false); 
            }
        }
    }
}
