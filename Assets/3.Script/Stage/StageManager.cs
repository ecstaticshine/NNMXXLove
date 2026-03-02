using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class StageManager : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;

    [Header("Node")]
    public GameObject[] nodeObjects;
    public GameObject linePrefab;
    private List<GameObject> activeLines = new List<GameObject>(); // 노드 라인

    [Header("Panels")]
    public GameObject mainPanel;        // 메인 끄게
    public GameObject stageSelectPanel; // 스테이지 선택창 키게
    public GameObject stageDetailPanel; // 스테이지 상세창
    public TMP_Text titleText;          // 패널의 제목
    public CanvasGroup ClearCheckerGroup;          // 스테이지 클리어 체커
    public Image ClearCheckerImage;
    public Sprite clearedStampSprite;
    public Sprite notClearedSprite;

    private string currentWorldName; // 현재 속한 월드명
    private int currentWorldIndex;  // 현재 속한 월드 번호
    private string currentStageIndex; // 현재 선택한 스테이지 번호

    [Header("StageDetailPopUp")]
    public Button startButton;                  // 출발 버튼
    public TMP_Text currentStaminaText;         // 스테미나 표시용
    public TMP_Text staminaCostText;            // 스테미나 소비 표시용
    public TMP_Text remainStaminaText;          // 남은 스테미나 표시용
    public Transform enemyContent;       // 적 슬롯 부모
    public GameObject enemyIconPrefab;   // 적 슬롯 프리팹
    public Button enemyLeftButton, enemyRightButton;    // 적이 5마리가 넘어가면 표시
    public ScrollRect enemySection;

    [Header("Reward & Drop UI")]
    public Transform rewardContent;      // '보상' 줄의 Content
    public Transform dropContent;        // '획득' 줄의 Content
    public GameObject itemIconPrefab;    // 아이템 프리팹
    public GameObject dropLeftBtn, dropRightBtn;        // 반복 클리어용
    public GameObject rewardLeftBtn, rewardRightBtn;    // 첫 클리어용

    [Header("Placement UI")]
    public GameObject placementPanel;
    public GameObject unitPrefab; // 배치할 유닛 프리팹


    public static StageManager Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        LoadWorld(DataManager.Instance.currentWorldIndex);
        if (GlobalUIManager.Instance != null)
        {
            // GlobalUIManager가 내부적으로 StageManager를 찾아 SyncPanelWithState를 호출하게 함
            GlobalUIManager.Instance.RefreshCurrentUI();
        }
    }

    public void UpdateStaminaUI(int cost)
    {
        int current = DataManager.Instance.userData.stamina;
        int remain = current - cost;

        // 1. 현재 스테미나
        currentStaminaText.text = current.ToString();

        // 2. 소모값
        staminaCostText.text = $"- { cost }";

        remainStaminaText.text = remain.ToString();

        // 3. 남은 스테미나 (부족하면 빨간색 표시)
        if (remain < 0)
        {
            remainStaminaText.color = Color.red;
            if (startButton != null) startButton.interactable = false; // 버튼 비활성화
        }
        else
        {
            remainStaminaText.color = new Color(0.2f, 0.6f, 1f); // 정상 (파랑)
            if (startButton != null) startButton.interactable = true;  // 버튼 활성화
        }
    }

    public void SyncPanelWithState(SceneState state)
    {
        mainPanel.SetActive(state == SceneState.Adventure);
        stageSelectPanel.SetActive(state == SceneState.StageSelect ||
                                   state == SceneState.StageDetailPopup ||
                                   state == SceneState.Placement);

        // 상세 팝업: StageDetailPopup일 때만 켬
        stageDetailPanel.SetActive(state == SceneState.StageDetailPopup);

        // 배치 패널: Placement일 때만 켬
        placementPanel.SetActive(state == SceneState.Placement);

        // 월드 이름 UI 처리
        if (GlobalUIManager.Instance != null)
            GlobalUIManager.Instance.SetWorldName(currentWorldName);
    }

    // Load World
    public void LoadWorld(int worldIndex)
    {
        currentWorldIndex = worldIndex;

        // 1. 해당 월드의 정보를 확인하기 위해서 worldInfo 선언
        DataManager.Instance.LoadGameDataByWorld(worldIndex);

        // 2. 해당 월드 데이터 가지고 오기
        WorldDataInfo info = DataManager.Instance.GetCurrentWorldInfo();
        if (info == null) return;

        Debug.Log(info.background);

        // 3.  배경 이미지 교체
        backgroundImage.sprite = Resources.Load<Sprite>($"Backgrounds/{info.background}");

        // 4. 월드 이름 다국어 적용
        currentWorldName = DataManager.Instance.GetLocalizedText(info.worldNameKey);

        // UI에 월드 이름 표시 (GlobalUIManager 등 활용)
        if (GlobalUIManager.Instance != null)
            GlobalUIManager.Instance.SetWorldName(currentWorldName);

        // 5. 월드 노드 업데이트
        UpdateStageNodes();

        
    }

    // 월드 바꾸면 노드 끼리 연결한 라인 변경
    private void ClearActiveLines()
    {
        foreach (GameObject line in activeLines)
        {
            if (line != null) Destroy(line);
        }
        activeLines.Clear();
    }


    private void UpdateStageNodes()
    {
        //라인 초기화
        ClearActiveLines();

        int nodeIdx = 0;
        var stageList = DataManager.Instance.stageList;

        foreach (StageDetailData stage in stageList)
        {
            if (nodeIdx >= nodeObjects.Length) break;

            GameObject nodeObj = nodeObjects[nodeIdx];
            nodeObj.SetActive(true);
            StageNode node = nodeObj.GetComponent<StageNode>();

            bool isUnlocked = DataManager.Instance.IsStageUnlocked(stage.stageID, stage.prevStageID);

            //노드에 정보 넣어주기
            node.Setup(stage, isUnlocked);

            if (nodeIdx > 0 && !string.IsNullOrEmpty(stage.prevStageID) && stage.prevStageID.ToLower() != "none")
            {
                // 바로 직전 노드의 위치와 현재 노드의 위치를 연결
                Vector2 startPos = nodeObjects[nodeIdx - 1].GetComponent<StageNode>().nodePosition;
                Vector2 endPos = node.nodePosition;

                DrawLineNodeToNode(startPos, endPos);
            }

            nodeIdx++;
        }

        // 사용하지 않는 남은 노드들은 숨기기
        for (int i = nodeIdx; i < nodeObjects.Length; i++)
        {
            nodeObjects[i].SetActive(false);
        }
    }

    public void GotoMainAdventure()
    {
        mainPanel.SetActive(false);
        stageSelectPanel.SetActive(true);
        GlobalUIManager.Instance.ChangeState(SceneState.StageSelect);
    }

    private void DrawLineNodeToNode(Vector2 start, Vector2 end)
    {
        GameObject line = Instantiate(linePrefab, stageSelectPanel.transform.GetChild(0).transform);
        line.transform.SetAsFirstSibling(); // 노드 뒤로 보내기

        activeLines.Add(line);// 월드변경 시, 지우기 위해 저장

        RectTransform rt = line.GetComponent<RectTransform>();
        Vector2 dir = end - start;
        float distance = dir.magnitude;

        rt.sizeDelta = new Vector2(distance, 5f); // 두께 5
        rt.anchoredPosition = start + dir * 0.5f;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    public void OpenStageDetail(string id)
    {
        // DataManager에서 해당 스테이지 정보 가지고 오기
        StageDetailData detail = DataManager.Instance.GetStageDetail(id);

        if (detail != null)
        {
            currentStageIndex = id;
            string displayID = detail.stageID.Replace($"W{currentWorldIndex:D2}S", "");

            bool isCleared = DataManager.Instance.IsStageCleared(id);

            // 1. 제목 설정 (id를 직접 쓰는 대신 detail.stageID 사용)
            titleText.text = $"{currentWorldName} {currentWorldIndex}-{int.Parse(displayID)}";

            // 2. 적 목록 갱신
            RefreshEnemyUI(detail.enemies);

            // 보상(첫클리어) 갱신
            RefreshFirstRewardUI(detail.firstRewards);

            // 획득(일반드롭) 갱신
            RefreshDropItemUI(detail.dropItems);

            // 5. 스테미나 정보
            int cost = detail.staminaCost;
            UpdateStaminaUI(cost);

            if (ClearCheckerGroup != null)
            {
                if (isCleared)
                {
                    // 1. 활성화 및 스프라이트 설정
                    ClearCheckerGroup.gameObject.SetActive(true);
                    if (clearedStampSprite != null)
                        ClearCheckerImage.sprite = clearedStampSprite;

                    // 2. 초기 상태 설정 (완전 투명 + 2배 크기)
                    ClearCheckerGroup.DOKill();
                    ClearCheckerGroup.alpha = 0f;
                    ClearCheckerGroup.transform.localScale = Vector3.one * 2f;

                    // 3. 연출 실행 (알파를 1로 만드는 DOFade 추가!)
                    ClearCheckerGroup.DOFade(1f, 0.2f); // 0.2초 동안 선명해짐
                    ClearCheckerGroup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBounce);
                }
                else
                {
                    // 클리어하지 않았다면 숨기기 (혹은 미클리어용 기본 이미지)
                    ClearCheckerImage.sprite = notClearedSprite;
                }
            }

            stageDetailPanel.SetActive(true);
            GlobalUIManager.Instance.ChangeState(SceneState.StageDetailPopup);
        }
    }
    public void OnCancelButtonOnStageDetail()
    {
        GlobalUIManager.Instance.OnBackButtonClicked();
    }

    // 편성 취소
    public void OnClickCancelPlacement()
    {
        placementPanel.SetActive(false);
        stageDetailPanel.SetActive(true);

        // 상태를 다시 StageSelect로 복귀
        GlobalUIManager.Instance.ChangeState(SceneState.StageSelect);
    }
    // 편성 리셋
    public void OnClickResetPlacement()
    {
        // 모든 SlotDrop을 찾아서 유닛이 있다면 파괴
        SlotDrop[] allSlots = placementPanel.GetComponentsInChildren<SlotDrop>();

        foreach (var slot in allSlots)
        {
            if (slot.TryGetComponent(out SlotController slotCtrl))
            {
                slotCtrl.RefreshColor(null);
            }

            if (slot.characterAnchorSlot.childCount > 0)
            {
                Transform unitTransform = slot.characterAnchorSlot.GetChild(0);
                CharacterDrag drag = unitTransform.GetComponent<CharacterDrag>();

                // 리스트 아이콘 다시 활성화 (밝게 만들기)
                if (drag != null && drag.originIcon != null)
                {
                    drag.originIcon.SetPlaced(false);
                }

                Destroy(unitTransform.gameObject);
            }
        }

        // 시너지 UI 갱신 (SlotDrop 중 하나를 이용해 전체 갱신 호출)
        if (allSlots.Length > 0)
        {
            allSlots[0].UpdateOverallSynergy();
        }

    }

    public void OnClickGoToPlacement()
    {
        // 스테이지 상세창 끄고 편성창 키기
        stageDetailPanel.SetActive(false);
        placementPanel.SetActive(true);

        // 상태 변경 (유닛 드래그가 가능하도록)
        GlobalUIManager.Instance.ChangeState(SceneState.Placement);

        StartCoroutine(WaitAndLoadParty());

        // 현재 선택한 스테이지 정보를 저장
        DataManager.Instance.selectedStageID = currentStageIndex;
    }

    private IEnumerator WaitAndLoadParty()
    {
        // 리스트 UI가 갱신될 시간을 아주 잠깐 줍니다 (한 프레임)
        yield return null;
        LoadSavedParty();
    }

    private void LoadSavedParty()
    {
        // 기존 배치된 오브젝트 완전 삭제 (리셋 함수 호출)
        OnClickResetPlacement();

        // DataManager에서 현재 파티 리스트 가져오기
        List<PartyMember> savedParty = DataManager.Instance.GetCurrentParty();

        // 슬롯들을 찾아서 인덱스에 맞게 배치
        SlotDrop[] allSlots = placementPanel.GetComponentsInChildren<SlotDrop>();
        UnitIcon[] listIcons = FindObjectsOfType<UnitIcon>();

        foreach (PartyMember member in savedParty)
        {
            SlotDrop targetSlot = System.Array.Find(allSlots, s => s.slotIndex == member.slotIndex);

            // member.slotIndex가 유효한 범위인지 확인
            if (member.slotIndex >= 0 && member.slotIndex < allSlots.Length)
            {
                GameObject newUnit = SpawnUnitFromData(targetSlot, member.unitID);

                if (newUnit != null)
                {
                    CharacterDrag dragScript = newUnit.GetComponent<CharacterDrag>();
                    foreach (var icon in listIcons)
                    {
                        // 아이콘이 가진 데이터의 ID와 배치된 유닛의 ID가 같다면
                        if (icon.GetUnitData() != null && icon.GetUnitData().unitID == member.unitID)
                        {
                            icon.SetPlaced(true); // 아이콘 어둡게 처리
                            if (dragScript != null) dragScript.originIcon = icon; // 서로 연결
                            break;
                        }
                    }
                }
            }

        }
    }

    private GameObject SpawnUnitFromData(SlotDrop slot, int unitID)
    {
        UnitData uData = DataManager.Instance.GetPlayerData(unitID);
        if (uData == null) return null;

        // 프리팹 생성 및 부모 설정
        GameObject newUnit = Instantiate(unitPrefab, slot.characterAnchorSlot);
        newUnit.transform.localPosition = Vector2.zero;
        newUnit.transform.localScale = Vector3.one;

        // 컴포넌트 데이터 주입
        Character charScript = newUnit.GetComponent<Character>();
        CharacterDrag dragScript = newUnit.GetComponent<CharacterDrag>();

        // 성장 데이터(레벨, 돌파 등) 가져오기
        CharacterInfo growth = DataManager.Instance.GetUserUnitInfo(unitID);

        if (charScript != null)
            charScript.SetCharacterData(uData, growth.currentLevel, growth.currentBreakthrough,(0,0,0));

        if (slot.TryGetComponent(out SlotController slotCtrl))
        {
            slotCtrl.RefreshColor(uData.defaultTag);
        }

        // UI 및 시너지 갱신
        slot.UpdateOverallSynergy();

        return newUnit;
    }

    public void FinalStartBattle()
    {
        // 1. 스테이지 상세 정보 가지고 오기
        StageDetailData detail = DataManager.Instance.GetStageDetail(currentStageIndex);
        if(detail == null) return;

        // 2. 스테미나 충분한지 확인 및 차감
        if(DataManager.Instance.userData.stamina < detail.staminaCost)
        {
            Debug.Log("스태미나가 부족하여 전투에 진입할 수 없습니다.");
            // 여기에 '스태미나 부족 팝업'을 띄우는 코드를 넣을 예정
            return;
        }

        // 3. 스태미나 실제 차감
        DataManager.Instance.userData.stamina -= detail.staminaCost;

        // 4. 파티 구성 정보
        List<PartyMember> newParty = new List<PartyMember>();
        SlotDrop[] allSlots = placementPanel.GetComponentsInChildren<SlotDrop>();

        foreach (var slot in allSlots)
        {
            // 슬롯에 유닛이 있다면
            if (slot.characterAnchorSlot.childCount > 0)
            {
                Character character = slot.characterAnchorSlot.GetChild(0).GetComponent<Character>();

                if (character != null && character.data != null)
                {
                    // 중요: 루프 인덱스 i가 아니라, 슬롯에 설정된 slot.slotIndex를 사용!
                    newParty.Add(new PartyMember(slot.slotIndex, character.data.unitID));
                    Debug.Log($"슬롯 {slot.slotIndex}번에 유닛 {character.data.unitID} 저장 완료");
                }
            }
        }

        // 5. 데이터 매니저에 갱신 및 세이브
        DataManager.Instance.SaveParty(newParty);
        DataManager.Instance.SaveData();

        // 6. 씬 전환 및 상태 변경
        GlobalUIManager.Instance.ChangeState(SceneState.Battle, true);
    }

    public void RefreshEnemyUI(List<StageEnemyInfo> enemyList)
    {
        // 1. 기존에 생성된 슬롯들 제거
        foreach (Transform child in enemyContent) Destroy(child.gameObject);

        if (enemyList == null) return;

        foreach (var info in enemyList)
        {
            UnitData unitData = DataManager.Instance.GetEnemyData(info.unitID);

            Debug.Log(info.unitID);
            if (unitData != null)
            {
                GameObject slotObj = Instantiate(enemyIconPrefab, enemyContent);


                UnitIcon slotScript = slotObj.GetComponent<UnitIcon>();

                if (slotScript != null)
                {
                    // 여기서 유닛 정보, 레벨, 등급 등을 전달
                    slotScript.SetUnitIcon(unitData, info.level);
                }
            }
        }

        // 버튼 활성화 처리
        bool isScrollable = enemyList.Count > 5;
        enemyLeftButton.gameObject.SetActive(isScrollable);
        enemyRightButton.gameObject.SetActive(isScrollable);

        // 스크롤 위치 초기화
        Canvas.ForceUpdateCanvases();
        enemySection.horizontalNormalizedPosition = 0f;
    }

    public void RefreshFirstRewardUI(List<ItemDropData> rewards)
    {
        foreach (Transform child in rewardContent) Destroy(child.gameObject);

        // 유저 데이터에서 이 스테이지를 이미 깼는지 확인
        StageHistory history = DataManager.Instance.userData.stageHistory.Find(x => x.stageID == currentStageIndex);
        bool isAlreadyClaimed = (history != null && history.isFirstRewardClaimed);

        foreach (var res in rewards)
        {
            GameObject slot = Instantiate(itemIconPrefab, rewardContent);
            ItemIcon itemIcon = slot.GetComponent<ItemIcon>();

            // 아이템 SO 로드 (DataManager에 GetItemData가 있다고 가정)
            ItemData data = DataManager.Instance.GetItemData(res.itemID);
            itemIcon.Setup(data, res.count, res.chance);

            if (isAlreadyClaimed)
            {
                // 예: 아이콘의 색상을 어둡게 변경 (반투명하게)
                itemIcon.SetObtained(true); 
            }
        }

        // 화살표 활성화 (예: 5개 넘으면)
        rewardLeftBtn.SetActive(rewards.Count > 5);
        rewardRightBtn.SetActive(rewards.Count > 5);
    }

    // 2. '획득' (반복 드롭 전용) 갱신
    public void RefreshDropItemUI(List<ItemDropData> drops)
    {
        foreach (Transform child in dropContent) Destroy(child.gameObject);

        if (drops == null || drops.Count == 0) return;

        foreach (var res in drops)
        {
            GameObject slot = Instantiate(itemIconPrefab, dropContent);
            ItemIcon itemIcon = slot.GetComponent<ItemIcon>();

            ItemData data = DataManager.Instance.GetItemData(res.itemID);
            if (data != null)
            {
                itemIcon.Setup(data, res.count, res.chance);
            }
        }

        dropLeftBtn.SetActive(drops.Count > 5);
        dropRightBtn.SetActive(drops.Count > 5);
    }
}
