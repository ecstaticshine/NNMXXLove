using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    public GameObject tooltipPopup;
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    private RectTransform rectTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (tooltipPopup != null)
        {
            rectTransform = tooltipPopup.GetComponent<RectTransform>();
            HideTooltip();
        }
    }

    private void Update()
    {
        if (tooltipPopup != null)
        {
            if (tooltipPopup.activeSelf)
            {
                // 마우스 위치를 따라다니게 설정 (약간의 오프셋 추가)
                Vector2 mousePos = Mouse.current.position.ReadValue();

                tooltipPopup.transform.position = mousePos;

                rectTransform.anchoredPosition += new Vector2(20f, -20f);
            }
        }
    }

    public void ShowTooltip(string name, string desc)
    {
        if (this == null || tooltipPopup == null)
        {
            Debug.LogWarning("TooltipManager 혹은 Popup이 파괴되어 표시할 수 없습니다.");
            return;
        }

        Debug.Log(name);
        Debug.Log(desc);


        nameText.text = name;
        descriptionText.text = desc;
        tooltipPopup.SetActive(true);

        // [연출] DOTween 사용 시 - 타겟이 살아있는지 한 번 더 확인
        if (tooltipPopup.transform != null)
        {
            tooltipPopup.transform.DOKill();
            tooltipPopup.transform.localScale = Vector3.one * 0.8f;
            tooltipPopup.transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
        }

    }

    public void HideTooltip()
    {
        // 오브젝트가 파괴되었는지(null), 혹은 살아있는지 체크
        if (this == null || tooltipPopup == null) return;

        tooltipPopup.SetActive(false);
    }
}
