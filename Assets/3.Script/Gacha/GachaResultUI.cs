using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class GachaResultUI : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup mainCanvasGroup;
    public GameObject coverEffectPanel; // "터치하여 확인" 연출용 검은 화면/이펙트
    public GameObject resultPanel;
    public Transform cardContainer;      // 유닛 아이콘들이 배치될 Grid 부모

    [Header("Prefabs")]
    public GameObject unitIconPrefab;    // 기존에 사용하던 UnitIcon 프리팹

    private bool isWaitingForClick = false;
    private List<int> storedResults;

    public void ShowResult(List<int> resultIDs)
    {
        storedResults = resultIDs;

        // 1. 초기화: 결과창 켜기
        gameObject.SetActive(true);
        mainCanvasGroup.alpha = 1;
        coverEffectPanel.SetActive(true); // 연출용 커버 먼저 켜기
        cardContainer.gameObject.SetActive(false); // 카드는 아직 숨김

        // 기존 카드 청소
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        isWaitingForClick = true;
        Debug.Log("연출 대기 중... 화면을 클릭하세요.");
    }

    // 화면 전체를 덮는 투명 버튼 등에 연결할 함수
    public void OnScreenClick()
    {
        if (!isWaitingForClick) return;
        isWaitingForClick = false;

        // 2. 연출 커버 치우기 (DOTween 활용)
        coverEffectPanel.GetComponent<CanvasGroup>()?.DOFade(0, 0.5f).OnComplete(() => {
            coverEffectPanel.SetActive(false);
            DisplayCards();
        });
    }

    private void DisplayCards()
    {
        resultPanel.SetActive(true);
        cardContainer.gameObject.SetActive(true);

        for (int i = 0; i < storedResults.Count; i++)
        {
            int unitID = storedResults[i];
            UnitData data = DataManager.Instance.GetPlayerData(unitID);

            if (data != null)
            {
                GameObject iconObj = Instantiate(unitIconPrefab, cardContainer);
                UnitIcon iconScript = iconObj.GetComponent<UnitIcon>();

                // 기존 UnitIcon의 함수를 그대로 재활용! (레벨은 1로 표시)
                iconScript.SetUnitIcon(data, 1);

                // 3. 톡톡 튀어나오는 연출
                iconObj.transform.localScale = Vector3.zero;
                iconObj.transform.DOScale(1f, 0.4f)
                    .SetDelay(i * 0.1f)
                    .SetEase(Ease.OutBack)
                    .OnStart(() => {
                // 여기에 소리 재생 코드 추가
                if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySE("Pon");
                        }
                    });
            }
        }
    }

    public void CloseResult()
    {
        mainCanvasGroup.DOFade(0, 0.3f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
}