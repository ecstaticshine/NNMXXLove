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

    [Header("Data Assets")]
    public TextAsset worldCsv;          // WorldData.csv
    public TextAsset stageCsv;          // StageData.csv
    public TextAsset localizationCsv;   // 현재 언어에 맞는 csv

    [Header("Panels")]
    public GameObject mainPanel;        // 메인 끄게
    public GameObject stageSelectPanel; // 스테이지 선택창 키게
    public GameObject stageDetailPanel; // 스테이지 상세창
    public TMP_Text titleText;          // 패널의 제목

    private List<Dictionary<string, string>> worldDataList;
    private List<Dictionary<string, string>> stageDataList;
    private Dictionary<string, string> localizationMap;

    private string currentWorldName; // 현재 속한 월드명
    private int currentWorldIndex;  // 현재 속한 월드 번호
    private int currentStageIndex; // 현재 선택한 스테이지 번호

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
        worldDataList = ParseCSV(worldCsv);
        stageDataList = ParseCSV(stageCsv);
        LoadLocalization();
    }

    void Start()
    {
        LoadWorld(0);
    }

    // Load World
    public void LoadWorld(int worldIndex)
    {
        currentWorldIndex = worldIndex;

        // 1.  해당 월드의 정보를 확인하기 위해서 worldInfo 선언
        Dictionary<string, string> worldInfo = worldDataList[worldIndex];


        Debug.Log($"{worldInfo["Background"]}");
        // 2. 해당 월드의 배경화면 교체
        backgroundImage.sprite = Resources.Load<Sprite>($"Backgrounds/{worldInfo["Background"]}");

        // 3. 월드 이름 다국어 적용
        string nameKey = worldInfo["WorldNameKey"];
        currentWorldName = localizationMap.ContainsKey(nameKey) ? localizationMap[nameKey] : nameKey;
        Debug.Log(currentWorldName);
        GlobalUIManager.Instance.SetWorldName(currentWorldName);


        // 4. StartRow ~ EndRow를 이용한 노드 업데이트
        int startRow = int.Parse(worldInfo["StartRow"]);
        int endRow = int.Parse(worldInfo["EndRow"]);
        int nodeIdx = 0;


        for (int i = startRow; i<= endRow; i++)
        {
            if (nodeIdx >= nodeObjects.Length) break;

            Dictionary<string, string> stageInfo = stageDataList[i];
            int id = int.Parse(stageInfo["StageID"]);
            int preId = int.Parse(stageInfo["PrevStageID"]);
            float posX = float.Parse(stageInfo["NodePosX"]);
            float posY = float.Parse(stageInfo["NodePosY"]);
            bool preCleared = true;

            StageNode node = nodeObjects[nodeIdx].GetComponent<StageNode>();
            node.Setup(worldIndex + 1, id, preId, posX, posY, preCleared); // 인덱스 0부터 시작해서 +1 함.

            if(preId != -1)
            {
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

    // 간단한 CSV 파서 (Dictionary 형태로 한 줄씩 담습니다)
    private List<Dictionary<string, string>> ParseCSV(TextAsset csv)
    {
        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
        string[] lines = csv.text.Split('\n');
        string[] headers = lines[0].Trim().Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = lines[i].Trim().Split(',');
            var entry = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++) entry[headers[j]] = values[j];
            list.Add(entry);
        }
        return list;
    }

    private void LoadLocalization()
    {
        localizationMap = new Dictionary<string, string>();
        string[] lines = localizationCsv.text.Split('\n');
        foreach (string line in lines)
        {
            string[] split = line.Trim().Split(',');
            if (split.Length >= 2) localizationMap[split[0]] = split[1];
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
        RectTransform rt = line.GetComponent<RectTransform>();
        Vector2 dir = end - start;
        float distance = dir.magnitude;

        rt.sizeDelta = new Vector2(distance, 5f); // 두께 5
        rt.anchoredPosition = start + dir * 0.5f;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    public void OpenStageDetail(int id)
    {
        Dictionary<string, string> stageInfo = stageDataList.Find(x => int.Parse(x["StageID"]) == id);

        if (stageInfo != null)
        {
            titleText.text = $"{currentWorldName} {currentWorldIndex + 1}-{stageInfo["StageID"]}";
        }
        currentStageIndex = id;
        stageDetailPanel.SetActive(true);
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
}
