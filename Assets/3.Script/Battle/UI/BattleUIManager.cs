using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Pool;

public class BattleUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject turnPanel;
    public CanvasGroup playerPhasePanel;
    public CanvasGroup enemyPhasePanel;
    public CanvasGroup resultPanel;
    public TMP_Text resultText;

    [Header("TimeLine UI")]
    public Transform timelineContainer;
    public GameObject unitIconPrefab;

    private IObjectPool<GameObject> _timelinePool;
    private List<GameObject> _activeIcons = new List<GameObject>();

    public float fadeDuration = 0.5f;

    private void Awake()
    {
        _timelinePool = new ObjectPool<GameObject>(
            createFunc: CreateIcon,           // 새로 만들어야 할 때
            actionOnGet: OnGetIcon,           // 꺼내 쓸 때
            actionOnRelease: OnReleaseIcon,   // 다시 넣어둘 때
            actionOnDestroy: OnDestroyIcon,   // 풀이 넘쳐서 파괴할 때
            defaultCapacity: 15,              // 기본 크기
            maxSize: 25                       // 최대 크기
            );
    }


    private GameObject CreateIcon()
    {
        return Instantiate(unitIconPrefab, timelineContainer);
    }

    private void OnGetIcon(GameObject obj)
    {
        obj.SetActive(true);
    }
    private void OnReleaseIcon(GameObject obj)
    {
        obj.SetActive(false);
    }
    private void OnDestroyIcon(GameObject obj)
    {
        Destroy(obj);
    }
public void RefreshTimeline(List<Unit> turnOrder)
{
    // 아이콘 풀로 보내기
    foreach (var icon in _activeIcons)
    {
        _timelinePool.Release(icon);
    }
    _activeIcons.Clear();

    //재배치
    //foreach (Character unit in turnOrder)
    //{
    //    // 풀에서 하나 빌려오기!
    //    GameObject iconObj = _timelinePool.Get();



    //    _activeIcons.Add(iconObj);
    //}
}


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

        canvasGroup.DOFade(1f, fadeDuration);
        canvasGroup.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);

    }

    public void FadeOut(CanvasGroup canvasGroup)
    {
        canvasGroup.DOKill();

        // 뚝 끊길 수 있어서 완전히 끝나고 나서 끄는 
        canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
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
