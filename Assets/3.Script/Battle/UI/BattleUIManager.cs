using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BattleUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject startUIPanel;
    public GameObject turnPanel;
    public CanvasGroup playerPhasePanel;
    public CanvasGroup enemyPhasePanel;
    public CanvasGroup resultPanel;
    public TMP_Text resultText;
    public TMP_Text speedText;

    [Header("Button References")]
    public Color defaultColor = Color.white;
    public Image speedButtonImage; // 배속 버튼 이미지
    public GameObject speedEffectObject;

    public Image autoButtonImage;  // 자동 버튼 이미지
    public GameObject autoEffectObject;


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

    public Transform container;   // 아이템 아이콘들이 생성될 부모
    public GameObject rewardItemPrefab; 


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
    public void ShowStartUI()
    {
        StartCoroutine(StartUI_Co());
    }

    private IEnumerator StartUI_Co()
    {
        CanvasGroup cg = startUIPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
        startUIPanel.transform.localScale = Vector3.one * 0.8f;
        startUIPanel.SetActive(true);

        // 나타나기
        if (cg != null) cg.DOFade(1f, 0.5f);
        startUIPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);

        // 잠시 대기 (플레이어가 글자를 읽을 시간)
        yield return new WaitForSeconds(0.5f);

        // 3. 사라지기 연출
        if (cg != null) cg.DOFade(0f, 0.4f);
        startUIPanel.transform.DOScale(1.5f, 0.4f).SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.4f); // 연출 끝날 때까지 대기

        startUIPanel.SetActive(false);

        // 4. 전투 시작!
        BattleManager.instance.BattleStart();
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
                unitIcon.SetUnitIcon(unit.data, unit.level);
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


    public void ShowResult(bool isVictory, List<ItemInventoryData> rewards = null, List<Character> characterParties = null)
    {

        // 패널 활성화 및 초기화
        resultPanel.gameObject.SetActive(true);
        resultPanel.alpha = 0f;
        resultPanel.blocksRaycasts = false;


        if (isVictory)
        {
            resultText.text = "Victory";
            resultText.color = Color.yellow;

            // 1. 보상 아이템 UI 생성 로직
            if (rewards == null)
            {
                rewards = DataManager.Instance.GetLastEarnedRewards();
            }

            // 코루틴 시작
            StartCoroutine(ResultSequence_Co(isVictory, rewards, characterParties));


        }
        else
        {
            resultText.text = "Defeated";
            resultText.color = Color.red;
        }

        // 페이드 인 완료 후 상호작용 허용
        resultPanel.DOFade(1f, fadeDuration).OnComplete(() => {
            resultPanel.blocksRaycasts = true;
        });

        // 텍스트 펀치 연출 (강조)
        resultText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
    }

    private void DisplayRewards(List<ItemInventoryData> rewards)
    {
        // 기존 아이콘들 청소
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 보상이 하나도 없을 경우 처리 (선택사항)
        if (rewards == null || rewards.Count == 0) return;

        for(int i = 0; i < rewards.Count; i++)
        {

            ItemInventoryData item = rewards[i];

            // 프리팹 생성
            GameObject itemObj = Instantiate(rewardItemPrefab, container);

            // 3. ItemIcon 컴포넌트 가져오기
            ItemIcon itemIcon = itemObj.GetComponent<ItemIcon>();

            if (itemIcon != null)
            {
                // 데이터 로드
                ItemData data = DataManager.Instance.GetItemData(item.itemID);

                if (data != null)
                {
                    itemIcon.Setup(data, item.count);
                    itemIcon.ShowChanceText(false);
                    Debug.Log($"[UI] 보상 생성 성공: {item.itemID} (개수: {item.count})");
                }
                else
                {
                    Debug.LogError($"[UI] {item.itemID}의 ItemData를 찾을 수 없습니다!");
                }
            }

            // [연출] 톡톡 튀어나오는 애니메이션
            itemObj.transform.localScale = Vector3.zero;
            itemObj.transform.DOScale(1f, 0.5f)
                .SetDelay(i * 0.1f)
                .SetEase(Ease.OutBack);
        }
    }

    public void ShowBattleResult()
    {
        FadeIn(resultPanel);

        // 1. 데이터 매니저에서 방금 얻은 보상 리스트 가져오기
        List<ItemInventoryData> rewards = DataManager.Instance.GetLastEarnedRewards();

        // 2. 화면에 표시
        DisplayRewards(rewards);
    }

    private void DisplayCharacterGrowth(List<Unit> party)
    {
        foreach (Unit unit in party)
        {
            if (unit is Character character)
            {
                // 캐릭터의 레벨업 게이지를 UI로 표현
                Debug.Log($"{character.data.unitNameKey}: LV.{character.level} EXP 상승 중...");
            }
        }
    }

    public void OnClickExitResult()
    {
        // 1. 결과창 페이드 아웃
        resultPanel.DOFade(0f, 0.3f).OnComplete(() => {
            resultPanel.gameObject.SetActive(false);

            // 2. 글로벌 UI 다시 켜기
            if (GlobalUIManager.Instance != null)
            {
                GlobalUIManager.Instance.SetBattleLayout(true);
                // 전투 종료 후 돌아갈 상태 설정 (예: Adventure)
                // 글로벌UI은 전 상태로 돌아가기
                GlobalUIManager.Instance.OnBackButtonClicked();
            }
            else
            {
                // 만약 GlobalUIManager가 없는 상황을 대비한 예외 처리
                SceneManager.LoadScene("AdventureScene");
            }
        });
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

            SetButtonState(speedButtonImage, isSpeedActive, speedEffectObject);

            speedText.color = isSpeedActive ? Color.yellow : Color.white;



            // 시각적 효과: 배속이 바뀔 때 텍스트가 살짝 커졌다 작아지게 (DOTween 활용)
            speedText.transform.DOKill();
            speedText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }
    }

    public void SetButtonState(Image buttonImage, bool isActive, GameObject effectObject)
    {
        buttonImage.transform.DOKill();


        // [효과] 클릭했을 때 움찔하는 연출 (누를 때마다 실행)
        buttonImage.transform.DOPunchScale(Vector3.one * -0.1f, 0.1f, 10, 1f);

        buttonImage.color = defaultColor;

        if (effectObject != null)
        {
            effectObject.SetActive(isActive);

            if (isActive)
            {
                // [연출] 이펙트가 켜져 있을 때 살살 회전하거나 깜빡이게 함
                effectObject.transform.DOKill();
                effectObject.transform.localRotation = Quaternion.identity;

                // 무한 회전 (선택 사항)
                effectObject.transform.DOLocalRotate(new Vector3(0, 0, 360), 3f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);

                // 살짝 깜빡이는 느낌 (알파값 조절)
                CanvasGroup cg = effectObject.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.DOFade(0.4f, 0.8f).SetLoops(-1, LoopType.Yoyo);
                }
            }
        }

    }

    public void UpdateAutoBattleUI(bool isActive)
    {
            SetButtonState(autoButtonImage, isActive, autoEffectObject);

            // AUTO 텍스트가 있다면 텍스트 색상도 강조
            TMP_Text autoText = autoButtonImage.GetComponentInChildren<TMP_Text>();
            if (autoText != null)
            {
                autoText.color = isActive ? Color.yellow : Color.white;
            }
    }

    private IEnumerator ResultSequence_Co(bool isVictory, List<ItemInventoryData> rewards,List<Character> characterParties = null)
    {
        // --- [1단계: 아이템 표시] ---
        resultPanel.DOFade(1f, fadeDuration);
        DisplayRewards(rewards); // 기존 아이템 생성 로직 호출

        yield return new WaitForSeconds(fadeDuration);
        resultPanel.blocksRaycasts = true;

        // 유저 클릭 대기 (3초 자동 넘김을 원하시면 타이머를 섞어도 됩니다)
        yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame ||
                                 (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame));

        // --- [2단계: 컨테이너 비우고 캐릭터 경험치 표시] ---
        // 기존 아이템 아이콘들 제거
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        
        int gainExp = 150; // 전투 승리 기본 획득 경험치량

        if(characterParties != null)
        {
            foreach (Unit unit in characterParties)
            {
                if (unit is Character character)
                {
                    // 프리팹 생성 및 배치 (rewardContainer 재활용)
                    GameObject iconObj = Instantiate(unitIconPrefab, container);
                    UnitIcon unitIcon = iconObj.GetComponent<UnitIcon>();

                    if (unitIcon != null)
                    {
                        // 기본 정보 세팅 (기존 함수 재사용)
                        unitIcon.SetUnitIcon(character.data, character.level);

                        // 경험치/레벨업 연출 (만렙 100 체크 포함)
                        if (character.level < 100)
                        {
                            // TODO: 실제 DataManager의 경험치 테이블과 연동 필요
                            float currentExp = 0; // 실제 데이터 연결 필요
                            float nextExp = 1000f;
                            bool isLevelUp = (currentExp + gainExp >= nextExp);
                            unitIcon.SetExpUI(currentExp + gainExp, nextExp, isLevelUp);
                        }

                        // 죽은 애들은 살짝 어둡게 (선택 사항)
                        if (unit.GetCurrentHP() <= 0)
                        {
                            iconObj.GetComponent<CanvasGroup>().alpha = 0.6f;
                        }
                    }
                }
            }
        }

       

        // 결과창 최종 종료 대기 (한 번 더 클릭하면 나감)
        yield return new WaitForSeconds(0.5f); // 연출 직후 바로 꺼짐 방지
        yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame ||
                                         (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame));

        OnClickExitResult();
    }

}
