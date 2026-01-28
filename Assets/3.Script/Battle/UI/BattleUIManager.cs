using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class BattleUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject turnPanel;
    public CanvasGroup playerPhasePanel;
    public CanvasGroup enemyPhasePanel;
    public CanvasGroup resultPanel;
    public TMP_Text resultText;

    public float fadeDuration = 0.5f;

    public void OnPhaseChanged(BattlePhase battlePhase)
    {
        FadeOut(playerPhasePanel);
        FadeOut(enemyPhasePanel);
        FadeOut(resultPanel);

        switch (battlePhase)
        {
            case BattlePhase.PlayerPhase:
                FadeIn(playerPhasePanel);
                break;
            case BattlePhase.EnemyPhase:
                FadeIn(enemyPhasePanel);
                break;
            case BattlePhase.BattleEnd:
                FadeIn(resultPanel);
                break;
        }
    }

    public void FadeIn(CanvasGroup canvasGroup)
    {
        canvasGroup.DOKill();

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0;

        canvasGroup.DOFade(1f, 0.5f);
    }

    public void FadeOut(CanvasGroup canvasGroup)
    {
        canvasGroup.DOKill();

        // 뚝 끊길 수 있어서 완전히 끝나고 나서 끄는 
        canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
        {
            canvasGroup.gameObject.SetActive(false);
        });
    }


    public void ShowResult(bool isVictory)
    {

        if (isVictory)
        {
            resultText.text = "Victory";
            resultText.color = Color.yellow;
        }
        else
        {
            resultText.text = "Defeated";
            resultText.color = Color.red;
        }
        FadeIn(resultPanel);
    }
}
