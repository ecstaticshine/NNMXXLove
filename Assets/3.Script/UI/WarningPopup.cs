using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarningPopup : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform popupTransform;
    public TMP_Text messageText;

    public Image iconImage; // 말풍선 내부 아이콘
    public Sprite infoSprite, warningSprite, errorSprite;

    // 싱글톤처럼 어디서든 편하게 부르고 싶다면 추가
    public static WarningPopup Instance;

    private void Awake()
    {
        Instance = this;
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void Show(string message, Sprite typeIcon = null)
    {
        messageText.text = message;
        if (typeIcon != null) iconImage.sprite = typeIcon;

        // 이미 떠있는 연출이 있다면 취소하고 새로 시작
        CancelInvoke("Hide");

        gameObject.SetActive(true);
        messageText.text = message;

        canvasGroup.DOKill();
        popupTransform.DOKill();

        canvasGroup.DOFade(1f, 0.2f);
        popupTransform.localScale = Vector3.one * 0.8f;
        popupTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        Invoke("Hide", 2.0f); // 메시지를 읽을 시간을 위해 조금 더 늘림 (2초)
    }

    public void Hide()
    {
        canvasGroup.DOFade(0f, 0.3f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
}
