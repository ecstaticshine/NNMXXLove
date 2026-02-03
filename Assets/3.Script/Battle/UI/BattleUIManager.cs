using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public TMP_Text speedText;

    [Header("Button References")]
    public Image speedButtonImage; // 배속 버튼 이미지
    public Image autoButtonImage;  // 자동 버튼 이미지

    // 상태 확인용 변수
    private bool isSpeedActive = false;
    private bool isAutoActive = false;

    [Header("TimeLine UI")]
    public Transform timelineContainer;
    public GameObject unitIconPrefab;

    private readonly Color playerThemeColor = new Color(0.2f, 0.4f, 0.8f, 0.5f); // 반투명 파랑
    private readonly Color enemyThemeColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);  // 반투명 빨강


    private IObjectPool<GameObject> _timelinePool;
    private List<GameObject> _activeIcons = new List<GameObject>();

    public float fadeDuration = 0.5f;


    [Header("Button Visual Settings")]
    public Color activeColor = Color.white;      // 활성화됐을 때 (예: 밝은 색)
    public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

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
    foreach (Unit unit in turnOrder)
        {
            // 풀에서 하나 빌려오기!
            GameObject iconObj = _timelinePool.Get();

            UnitIcon unitIcon = iconObj.GetComponent<UnitIcon>();

            if (unitIcon != null)
            {
                unitIcon.SetUnitData(unit);
            }

            _activeIcons.Add(iconObj);
        }
    }


    public void OnPhaseChanged(BattlePhase battlePhase)
    {
        FadeOut(playerPhasePanel);
        FadeOut(enemyPhasePanel);
        FadeOut(resultPanel);

        switch (battlePhase)
        {
            case BattlePhase.PlayerSelectPhase:
                FadeIn(playerPhasePanel);
                ChangeTimelineColor(playerThemeColor);
                break;
            case BattlePhase.EnemyPhase:
                FadeIn(enemyPhasePanel);
                ChangeTimelineColor(enemyThemeColor);
                break;
            case BattlePhase.BattleEnd:
                FadeIn(resultPanel);
                break;
        }
    }

    private void ChangeTimelineColor(Color targetColor)
    {
        if (timelineContainer != null)
        {
            // DOTween을 사용하여 색상을 부드럽게 전환 (가시성 +1)
            timelineContainer.GetComponent<Image>().DOColor(targetColor, fadeDuration);
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

    public void UpdateTurnUI(int turn)
    {
        turnPanel.GetComponentInChildren<TMP_Text>().text = turn.ToString();
    }

    public void UpdateSpeedUI(float speed)
    {
        if (speedText != null)
        {
            // "x1", "x2", "x3"
            speedText.text = $"x {speed}";

            // 배속이 1보다 크면 '활성화' 상태로 간주
            isSpeedActive = speed > 1f;

            SetButtonState(speedButtonImage, isSpeedActive);

            // 시각적 효과: 배속이 바뀔 때 텍스트가 살짝 커졌다 작아지게 (DOTween 활용)
            speedText.transform.DOKill();
            speedText.transform.localScale = Vector3.one;
            speedText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }
    }

    public void SetButtonState(Image buttonImage, bool isActive)
    {
        buttonImage.transform.DOKill();

        if (isActive)
        {
            // 눌린 상태: 작아지고 어두워짐 (혹은 더 밝아짐)
            buttonImage.transform.DOScale(0.92f, 0.1f);
            buttonImage.color = activeColor;
        }
        else
        {
            // 원래 상태: 크기 복구 및 색상 복구
            buttonImage.transform.DOScale(1.0f, 0.1f);
            buttonImage.color = inactiveColor;
        }
    }

    public void UpdateAutoBattleUI(bool isActive)
    {
            SetButtonState(autoButtonImage, isActive);

            // AUTO 텍스트가 있다면 텍스트 색상도 강조
            TMP_Text autoText = autoButtonImage.GetComponentInChildren<TMP_Text>();
            if (autoText != null)
            {
                autoText.color = isActive ? Color.yellow : Color.white;
            }
        }
    
}
