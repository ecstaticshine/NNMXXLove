using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

[System.Serializable]
public struct RarityEffect
{
    public string effectName; // 구분용 이름 (L, PL, TL, Pickup)
    public Sprite coverSprite;
    public string soundName;
}
public class GachaResultUI : MonoBehaviour
{
    public enum ResultState { None, WaitingForFirstClick, AnimatingCards, Finished }

    [Header("Panels")]
    public CanvasGroup mainCanvasGroup;
    public GameObject darkArea; // Inspector에서 할당
    public GameObject coverEffectPanel; // 연출용 커버
    public GameObject resultPanel;
    public Transform cardContainer;      // 유닛 아이콘들이 배치될 Grid 부모

    [Header("Prefabs")]
    public GameObject unitIconPrefab;    // 기존에 사용하던 UnitIcon 프리팹

    [Header("Rarity Visuals")]
    public Image coverImageDisplay;
    public RarityEffect L_Effect;
    public RarityEffect PL_Effect;
    public RarityEffect TL_Effect;
    public RarityEffect Pickup_Effect; // 픽업 전용

    private ResultState currentState = ResultState.None;
    private List<int> storedResults;

    public void ShowResult(List<int> resultIDs, int pickupID)
    {
        storedResults = resultIDs;

        currentState = ResultState.WaitingForFirstClick;

        // 1. 연출 등급 결정
        RarityEffect selectedEffect;

        if (resultIDs.Contains(pickupID))
        {
            selectedEffect = Pickup_Effect;
        }
        else
        {
            Rarity best = GetBestRarity(resultIDs);
            if (best == Rarity.TL) selectedEffect = TL_Effect;
            else if (best == Rarity.PL) selectedEffect = PL_Effect;
            else selectedEffect = L_Effect;
        }

        // 2. 적용
        ApplyRarityEffect(selectedEffect);


        // 1. 초기화: 결과창 켜기
        gameObject.SetActive(true);
        mainCanvasGroup.alpha = 1;
        mainCanvasGroup.blocksRaycasts = true; // 중요: 입력 차단

        // 2. 연출용 커버(coverEffectPanel) 초기화 [핵심 수정 부분]
        coverEffectPanel.SetActive(true);
        CanvasGroup coverCanvas = coverEffectPanel.GetComponent<CanvasGroup>();
        if (coverCanvas != null)
        {
            coverCanvas.alpha = 1; // 연출 커버를 다시 불투명하게(보이게) 설정
            coverCanvas.blocksRaycasts = true; // 클릭을 받을 수 있게 설정
        }

        // 3. 기타 패널 설정
        darkArea.SetActive(true);
        resultPanel.SetActive(false);

        // 기존 카드 청소
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        Debug.Log("연출 대기 중... 화면을 클릭하세요.");

        Debug.Log($"[GachaResultUI] coverEffectPanel Active:{coverEffectPanel.activeSelf}, Alpha:{coverCanvas?.alpha}, BlocksRaycasts:{coverCanvas?.blocksRaycasts}, Interactable:{coverCanvas?.interactable}");

    }

    // 화면 전체를 덮는 투명 버튼 등에 연결할 함수
    public void OnScreenClick()
    {
        Debug.Log($"[GachaResultUI] OnScreenClick 호출! 현재 상태: {currentState}");


        if (currentState == ResultState.WaitingForFirstClick)
        {
            // [단계 1] 커버 치우기
            currentState = ResultState.AnimatingCards;
            coverEffectPanel.GetComponent<CanvasGroup>()?.DOFade(0, 0.5f).OnComplete(() =>
            {
                coverEffectPanel.SetActive(false);
                DisplayCards();
            });
        }
        else if (currentState == ResultState.Finished)
        {
            // [단계 2] 결과창 닫기
            CloseResult();
        }
    }

    private void DisplayCards()
    {
        resultPanel.SetActive(true);
        darkArea.SetActive(false);
        cardContainer.gameObject.SetActive(true);

        for (int i = 0; i < storedResults.Count; i++)
        {
            int unitID = storedResults[i];
            UnitData data = DataManager.Instance.GetPlayerData(unitID);

            if (data != null)
            {
                GameObject iconObj = Instantiate(unitIconPrefab, cardContainer);
                iconObj.transform.localScale = Vector3.zero;

                iconObj.GetComponent<UnitIcon>()?.SetUnitIcon(data, 1);
                iconObj.transform.DOScale(1f, 0.4f).SetDelay(i * 0.1f).SetEase(Ease.OutBack)
                    .OnStart(() => AudioManager.Instance?.PlaySE("Pon"))
                    .OnComplete(() =>
                    {
                        // 마지막 카드 연출이 끝나면 상태를 Finished로 변경
                        if (i == storedResults.Count - 1) currentState = ResultState.Finished;
                    });
            }
        }
    }

    public void CloseResult()
    {
        currentState = ResultState.None;
        mainCanvasGroup.blocksRaycasts = false; // 중요: 모든 입력 차단 해제

        mainCanvasGroup.DOFade(0, 0.3f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            darkArea.SetActive(false);
        });
    }

    private Rarity GetBestRarity(List<int> resultIDs)
    {
        Rarity max = Rarity.L;
        foreach (var id in resultIDs)
        {
            var data = DataManager.Instance.GetPlayerData(id);
            if (data.rarity > max) max = data.rarity;
        }
        return max;
    }

    private void ApplyRarityEffect(RarityEffect effect)
    {
        if (effect.coverSprite != null) coverImageDisplay.sprite = effect.coverSprite;
        if (!string.IsNullOrEmpty(effect.soundName)) AudioManager.Instance?.PlaySE(effect.soundName);
    }
}