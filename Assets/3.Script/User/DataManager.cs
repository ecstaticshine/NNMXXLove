using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public UserData userData;
    public string selectedStageID; //현재 진행중인 스테이지 (임시 저장)

    // 현재 로드된 월드의 상세 정보를 담아둘 변수 (추가)
    private WorldDataInfo currentWorldInfo;

    // 유저가 현재 위치한 월드 (UserData 등에서 가져옴)
    public int currentWorldIndex = 0;

    // 스테이지 리스트
    public List<StageDetailData> stageList = new List<StageDetailData>();

    // 스테이지 ID로 스테이지 상세 정보 확인
    private Dictionary<string, StageDetailData> stageDetailDict = new Dictionary<string, StageDetailData>();
    
    private Dictionary<int, UnitData> unitDataCache = new Dictionary<int, UnitData>();
    private Dictionary<int, ItemData> itemDataCache = new Dictionary<int, ItemData>();

    public List<WorldDataInfo> worldList = new List<WorldDataInfo>(); // WorldData.csv 내용 담는 곳

    public enum Language { KO = 1, JP = 2 } // 0은 string키용
    public Language currentLanguage = Language.KO; // 기본값
    // 로컬라이제이션맵
    private Dictionary<string, string> localizationMap = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadData();           // 유저 세이브 데이터 먼저
            InitializeLocalization();
            // 유저가 있는 월드 데이터만 로드
            LoadGameDataByWorld(currentWorldIndex);   
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsStageUnlocked(string stageID, string prevStageID)
    {
        if (prevStageID.Equals("None")) return true;
        // 내 진행 기록에서 이전 스테이지가 클리어 되었는지 확인
        return userData.stageHistory.Exists(x => x.stageID == prevStageID && x.isCleared);
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(userData);
        PlayerPrefs.SetString("SaveFile", json);
    }

    public void LoadData()
    {
        string json = PlayerPrefs.GetString("SaveFile", "{}");
        userData = JsonUtility.FromJson<UserData>(json);
    }
    public void LoadGameDataByWorld(int worldIndex)
    {
        // 메모리 관리를 위해 기존 스테이지 상세 정보 초기화
        stageList.Clear();
        stageDetailDict.Clear();
        
        // 지금 유저가 있는 월드 데이터 획득
        currentWorldInfo = GetWorldInfo(worldIndex);
        if (currentWorldInfo != null)
        {

            // 월드 데이터의 스테이지 데이터 로드
            ParseStageDataByWorld(worldIndex);

            // 적 정보 가지고 오기
            ParseEnemyDataByWorld(worldIndex);

            // 스테이지 아이템 정보 가지고 오기
            // ParseItemDataByWorld(worldIndex);
            Debug.Log($"{worldIndex} 월드 데이터 로드 완료!");
        }

    }

    private WorldDataInfo GetWorldInfo(int worldIndex)
    {
        TextAsset worldCsv = Resources.Load<TextAsset>("Data/WorldData");
        if (worldCsv == null) return null;

        string[] lines = worldCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');

            if (int.Parse(row[0]) == worldIndex)
            {
                WorldDataInfo worldInfo = new WorldDataInfo();
                worldInfo.SetWorldDataInfo(
                    int.Parse(row[0]),
                    row[1],
                    int.Parse(row[2]),
                    int.Parse(row[3]),
                    row[4].Trim()
                );
                return worldInfo;
            }
        }

        Debug.LogError($"[DataManager] CSV에서 WorldIndex {worldIndex}를 찾을 수 없습니다.");
        return null;
    }

    public WorldDataInfo GetCurrentWorldInfo()
    {
        return this.currentWorldInfo;
    }

    private void ParseStageDataByWorld(int worldIndex)
    {

        // 지금 속한 월드 데이터 가지고 오기.
        WorldDataInfo worldInfo = GetWorldInfo(worldIndex);
        if (worldInfo == null) return;

        TextAsset stageCsv = Resources.Load<TextAsset>("Data/StageData");
        string[] lines = stageCsv.text.Trim().Split('\n');

        // 0번째는 헤더, 컬럼명이여서 +1
        int start = worldInfo.startRow + 1;
        int end = worldInfo.endRow + 1;

        for (int i = start; i <= end; i++)
        {
            if (i >= lines.Length) break;

            string[] row = lines[i].Split(',');

            // 1. 객체 생성
            StageDetailData detail = new StageDetailData();

            // 2. CSV 컬럼 순서에 맞춰 데이터 주입 (StageData.csv 기준)
            detail.stageID = row[0].Trim();
            detail.nodePos = new Vector2(float.Parse(row[1]), float.Parse(row[2]));
            detail.prevStageID = row[3].Trim();

            // 기본 스테미나 값 설정 (필요 시 CSV 추가 로드)
            detail.staminaCost = 10;

            // 3. 딕셔너리와 List에 저장
            stageList.Add(detail);
            stageDetailDict[detail.stageID] = detail;
        }
    }
    private void ParseEnemyDataByWorld(int worldIndex)
    {

        string worldKey = $"W{worldIndex:D2}"; // W01 이런 식으로 만들고 csv 비교

        TextAsset enemyCsv = Resources.Load<TextAsset>("Data/StageEnemyData");
        if (enemyCsv == null)
        {
            Debug.LogError("스테이지에 출현하는 적 데이터가 없습니다.");
            return;
        }

        string[] lines = enemyCsv.text.Trim().Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 8) continue; // 컬럼 개수 안 맞으면 스킵

            string stageID_str = row[0].Trim(); // "W01S01"

            if (stageID_str.StartsWith(worldKey))
            {
                if (stageDetailDict.TryGetValue(stageID_str, out StageDetailData targetStage))
                {
                    // 현재 월드와 일치하는 데이터라면 저장
                    StageEnemyInfo enemyInfo = new StageEnemyInfo
                    {
                        slotIndex = int.Parse(row[1]),
                        unitID = int.Parse(row[2]),
                        level = int.Parse(row[3]),
                        rarity = row[4],
                        type = row[5],
                        tag = row[6],
                        multiplier = float.Parse(row[7])
                    };

                    // 스테이지 상세 데이터에 적 추가
                    targetStage.enemies.Add(enemyInfo);
                }

            }
            else if(string.Compare(stageID_str, worldKey) > 0)
        {
                break;
            }
        }
    }

    // 팝업에 부르는 모든 정보
    public StageDetailData GetStageDetail(string stageID)
    {
        return stageDetailDict.ContainsKey(stageID) ? stageDetailDict[stageID] : null;
    }

    public UnitData GetUnitData(int unitID)
    {
        if (unitDataCache.ContainsKey(unitID)) return unitDataCache[unitID];

        // Resources/UnitDatas/ 폴더 안에 UnitData_101 식으로 저장되어 있어야 함
        UnitData data = Resources.Load<UnitData>($"UnitDatas/UnitData_{unitID}");
        if (data != null) unitDataCache[unitID] = data;

        return data;
    }


    // 스테이지 클리어 체크용
    public bool IsStageCleared(string stageID)
    {
        // string으로 저장되어 있다면 ToString() 처리
        return userData.stageHistory.Exists(x => x.stageID == stageID && x.isCleared);
    }

    // 아이템 데이터 로드
    public ItemData GetItemData(int itemID)
    {
        if (itemDataCache.ContainsKey(itemID)) return itemDataCache[itemID];

        ItemData data = Resources.Load<ItemData>($"ItemDatas/ItemData_{itemID}");
        if (data != null) itemDataCache[itemID] = data;

        return data;
    }

    // 
    public void InitializeLocalization()
    {
        LoadLocalization();
        
    }

    private void LoadLocalization()
    {
        localizationMap.Clear();
        TextAsset csv = Resources.Load<TextAsset>("Data/Localization"); // 경로 확인!
        if (csv == null) return;

        string[] lines = csv.text.Split('\n');

        // 데이터가 실제 몇 번째 컬럼에 있는지 확인 (enum 값 활용)
        int langCol = (int)currentLanguage;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 쉼표로 분리 (데이터 내부에 쉼표가 있다면 정규식이나 별도 파서 사용 권장)
            string[] split = lines[i].Trim().Split(',');

            if (split.Length > langCol)
            {
                string key = split[0];
                string value = split[langCol];

                // 줄바꿈 기호(\n)를 실제 줄바꿈으로 변환
                localizationMap[key] = value.Replace("\\n", "\n");
            }
        }
        Debug.Log($"로컬라이징 로드 완료: {currentLanguage}");
    }

    // 어디서든 쓸 수 있는 번역 함수
    public string GetLocalizedText(string key)
    {
        if (localizationMap.ContainsKey(key)) return localizationMap[key];
        return key; // 키가 없으면 키 그대로 반환 (디버깅용)
    }

    // 설정창 - 언어 변경
    public void ChangeLanguage(Language newLang)
    {
        currentLanguage = newLang;
        // PlayerPrefs.SetInt("SettingLanguage", (int)newLang); // 설정 저장
        LoadLocalization();

        // 이벤트 등을 활용해 현재 열려있는 UI들의 텍스트를 갱신하도록 신호를 보낼 수 있음
    }


}
