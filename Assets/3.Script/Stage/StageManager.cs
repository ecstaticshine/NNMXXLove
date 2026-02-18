using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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

    private string currentWorldName; // 현재 속한 월드명
    private int currentWorldIndex;  // 현재 속한 월드 번호
    private string currentStageIndex; // 현재 선택한 스테이지 번호

    [Header("StageDetailPopUp")]
    public TMP_Text staminaText;         // 스테미나 표시용
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

            // 1. 제목 설정 (id를 직접 쓰는 대신 detail.stageID 사용)
            titleText.text = $"{currentWorldName} {currentWorldIndex}-{int.Parse(displayID)}";

            // 2. 적 목록 갱신
            RefreshEnemyUI(detail.enemies);

            // 보상(첫클리어) 갱신
            RefreshFirstRewardUI(detail.firstRewards);

            // 획득(일반드롭) 갱신
            RefreshDropItemUI(detail.dropItems);

            // 5. 스테미나 정보
            if (staminaText != null)
                staminaText.text = detail.staminaCost.ToString();

            stageDetailPanel.SetActive(true);
        }
    }
    public void OnCancelButtonOnStageDetail()
    {
        stageDetailPanel.SetActive(false);
    }

    public void OnClickStartBattle()
    {
        DataManager.Instance.selectedStageID = currentStageIndex;

        GlobalUIManager.Instance.SetBattleLayout(false);

        SceneManager.LoadScene("BattleScene");
    }

    public void RefreshEnemyUI(List<StageEnemyInfo> enemyList)
    {
        // 1. 기존에 생성된 슬롯들 제거
        foreach (Transform child in enemyContent) Destroy(child.gameObject);

        if (enemyList == null) return;

        foreach (var info in enemyList)
        {
            UnitData unitData = DataManager.Instance.GetUnitData(info.unitID);

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
            itemIcon.Setup(data, res.count);
            itemIcon.SetChanceText(res.chance);

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
                itemIcon.Setup(data, res.count);
                itemIcon.SetChanceText(res.chance);
            }
        }

        dropLeftBtn.SetActive(drops.Count > 5);
        dropRightBtn.SetActive(drops.Count > 5);
    }
}
