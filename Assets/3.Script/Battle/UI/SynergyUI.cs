using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SynergyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject directUI; // 빨강
    [SerializeField] private GameObject splashUI; // 파랑
    [SerializeField] private GameObject dotUI;    // 초록

    public static SynergyUI instance = null;

    // 현재 시너지 카운트 저장
    private int currentDirect, currentSplash, currentDot;

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

    private void Start()
    {
        AddClickEvent(directUI, () => OnClickSynergy("Direct", currentDirect));
        AddClickEvent(splashUI, () => OnClickSynergy("Splash", currentSplash));
        AddClickEvent(dotUI, () => OnClickSynergy("Dot", currentDot));
    }

    private void AddClickEvent(GameObject uiObject, Action onClick)
    {
        if (uiObject == null) return;
        Button btn = uiObject.GetComponent<Button>();
        if (btn == null) btn = uiObject.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());
    }

    public void UpdateUI(int direct, int splash, int dot)
    {
        currentDirect = direct;
        currentSplash = splash;
        currentDot = dot;

        SetSynergyLevel(directUI, direct);
        SetSynergyLevel(splashUI, splash);
        SetSynergyLevel(dotUI, dot);
    }

    private void OnClickSynergy(string synergyType, int count)
    {
        int level = GetLevel(count);
        if (level == 0) return; // 비활성화 시너지는 클릭 무시

        // 시너지별 정보 구성
        string titleKey = $"synergy_title_{synergyType.ToLower()}"; // 로컬라이징 키
        string descKey = $"synergy_desc_{synergyType.ToLower()}_{level}"; // 단계별 설명

        // DetailInfoPopup에 띄우기
        DetailInfoPopup.Instance.SetupCustom(titleKey, descKey);
    }

    private int GetLevel(int count)
    {
        if (count >= 9) return 3;
        if (count >= 6) return 2;
        if (count >= 3) return 1;
        return 0;
    }

    private void SetSynergyLevel(GameObject uiObject, int count)
    {
        if (uiObject == null) return;

        // 시너지 단계 계산 (9명->3, 6명->2, 3명->1, 그 외 0)
        int level = GetLevel(count);

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
