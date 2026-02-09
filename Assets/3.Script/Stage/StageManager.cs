using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public GameObject[] nodeObjects;
    public Text worldNameText;

    [Header("Data Assets")]
    public TextAsset worldCsv;          // WorldData.csv
    public TextAsset stageCsv;          // StageData.csv
    public TextAsset localizationCsv;   // 현재 언어에 맞는 csv

    private List<Dictionary<string, string>> worldDataList;
    private List<Dictionary<string, string>> stageDataList;
    private Dictionary<string, string> localizationMap;

    private void Awake()
    {
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
        // 1.  해당 월드의 정보를 확인하기 위해서 worldInfo 선언
        Dictionary<string, string> worldInfo = worldDataList[worldIndex];

        // 2. 해당 월드의 배경화면 교체
        backgroundImage.sprite = Resources.Load<Sprite>($"Backgrounds/{worldInfo["Background"]}");

        // 3. 월드 이름 다국어 적용
        string nameKey = worldInfo["WorldNameKey"];
        worldNameText.text = localizationMap.ContainsKey(nameKey) ? localizationMap[nameKey] : nameKey;

        // 4. StartRow ~ EndRow를 이용한 노드 업데이트
        int startRow = int.Parse(worldInfo["StartRow"]);
        int endRow = int.Parse(worldInfo["EndRow"]);
        int nodeIdx = 0;

        for (int i = startRow; i<= endRow; i++)
        {
            if (nodeIdx >= nodeObjects.Length) break;

            Dictionary<string, string> stageInfo = stageDataList[i];
            float px = float.Parse(stageInfo["PosX"]);
            float py = float.Parse(stageInfo["PosY"]);

            nodeObjects[nodeIdx].SetActive(true);
            nodeObjects[nodeIdx].GetComponent<RectTransform>().anchoredPosition = new Vector2(px, py);


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
}
