using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterInfo
{
    public int unitID;      // 어떤 캐릭터인지 (ID)
    public int level;       // 현재 레벨
    public int breakthrough; // 돌파 단계
    public int exp;      // 필요하다면 경험치까지
}

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
    
    private Dictionary<int, UnitData> _playerDataCache = new Dictionary<int, UnitData>();
    private Dictionary<int, UnitData> _enemyDataCache = new Dictionary<int, UnitData>();
    private Dictionary<int, ItemData> itemDataCache = new Dictionary<int, ItemData>();

    public List<WorldDataInfo> worldList = new List<WorldDataInfo>(); // WorldData.csv 내용 담는 곳

    [Header("User Party Data")]
    public List<PartyMember> currentParty = new List<PartyMember>();

    // 유저가 보유한 유닛들의 성장 정보
    public List<CharacterInfo> userInventory = new List<CharacterInfo>();

    private List<ItemInventoryData> _lastEarnedRewards = new List<ItemInventoryData>();

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

        //나중에 지울 것
        PlayerPrefs.DeleteKey("SaveFile");
    }

    public CharacterInfo GetUserUnitInfo(int id)
    {
        // 리스트에서 ID가 일치하는 정보를 찾고, 없으면 기본값(1레벨) 반환
        return userInventory.Find(info => info.unitID == id)
               ?? new CharacterInfo { unitID = id, level = 1, breakthrough = 0 };
    }

    public List<PartyMember> GetCurrentParty()
    {
        // 만약 파티가 비어있다면, 기본 유닛을 넣어주기
        if (currentParty.Count == 0)
        {
            Debug.LogWarning("파티가 비어있습니다! 임시 데이터를 생성합니다.");
            currentParty.Add(new PartyMember(0, 1)); // 0번 슬롯에 1번 유닛
            currentParty.Add(new PartyMember(1, 2)); // 1번 슬롯에 2번 유닛
        }

        return currentParty;
    }

    // 파티를 저장하는 함수 (파티 편성 창에서 호출)
    public void SaveParty(List<PartyMember> newParty)
    {
        currentParty = newParty;
        // 필요하다면 여기서 로컬 저장(Json 등)을 수행합니다.
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
            ParseRewardDataByWorld(worldIndex);
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

    private void ParseRewardDataByWorld(int worldIndex)
    {
        string worldKey = $"W{worldIndex:D2}";
        TextAsset rewardCsv = Resources.Load<TextAsset>("Data/StageRewardData"); // 파일명 확인!
        if (rewardCsv == null) return;

        string[] lines = rewardCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 5) continue;

            string stageID_str = row[0].Trim();

            if (stageID_str.StartsWith(worldKey))
            {
                if (stageDetailDict.TryGetValue(stageID_str, out StageDetailData targetStage))
                {
                    ItemDropData rewardData = new ItemDropData
                    {
                        itemID = int.Parse(row[1]),
                        count = int.Parse(row[2]),
                        chance = float.Parse(row[3]),
                        isFirstReward = bool.Parse(row[4].Trim().ToLower())
                    };

                    if (rewardData.isFirstReward)
                        targetStage.firstRewards.Add(rewardData);
                    else
                        targetStage.dropItems.Add(rewardData);
                }
            }
        }
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

    // 아군 데이터 가져오기
    public UnitData GetPlayerData(int id)
    {
        if (_playerDataCache.TryGetValue(id, out var data)) return data;

        string path = $"UnitDatas/Players/UnitData_{id}";

        UnitData newData = Resources.Load<UnitData>(path);
        if (newData == null)
        {
            // 이 로그가 콘솔에 찍힌다면 100% 경로 혹은 파일 이름 문제입니다.
            Debug.LogError($"[DataManager] 파일을 찾을 수 없습니다! 경로: Resources/{path}");
        }
        Debug.Log(newData.name);

        // Players 폴더에서 로드
        if (newData != null) _playerDataCache.Add(id, newData);
        return newData;
    }

    // 적군 데이터 가져오기
    public UnitData GetEnemyData(int id)
    {
        if (_enemyDataCache.TryGetValue(id, out var data)) return data;

        // Enemies 폴더에서 로드
        UnitData newData = Resources.Load<UnitData>($"UnitDatas/Enemies/UnitData_{id}");
        if (newData != null) _enemyDataCache.Add(id, newData);
        return newData;
    }

    // 전투 종료 시 적군 캐시 비우기
    public void ClearEnemyCache() => _enemyDataCache.Clear();


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

    public void AttachTag(Character target, int tagItemID, int slotIndex)
    {
        var itemInInv = userData.inventory.Find(x => x.itemID == tagItemID);

        if (itemInInv == null || itemInInv.count <= 0)
        {
            Debug.LogWarning("아이템이 부족합니다!");
            return;
        }

        // 2. 아이템 데이터에서 태그 이름 가져오기
        ItemData tagData = GetItemData(tagItemID);

        // 3. 캐릭터의 해당 슬롯에 이름 저장 (덮어쓰기)
        target.customTags[slotIndex] = tagData.itemName; //

        // 4. 인벤토리 개수 차감 및 저장
        itemInInv.count--;
        if (itemInInv.count <= 0) userData.inventory.Remove(itemInInv);

        SaveData(); //
        Debug.Log($"{target.name}에게 {tagData.itemName} 장착 완료!");
    }

    public List<ItemInventoryData> CompleteStage(string stageID)
    {
        _lastEarnedRewards.Clear();
        Debug.Log($"[보상체크] 시작! 대상 스테이지 ID: {stageID}");

        // 1. 기록 갱신
        StageHistory history = userData.stageHistory.Find(x => x.stageID == stageID);
        if (history == null)
        {
            history = new StageHistory { stageID = stageID, isCleared = false, isFirstRewardClaimed = false  };
            userData.stageHistory.Add(history);
        }
        history.isCleared = true;

        // 2. 보상 지급 로직
        StageDetailData details = DataManager.Instance.GetStageDetail(stageID);

        if (details == null)
        {
            Debug.LogError($"[보상체크] 실패! {stageID}에 해당하는 상세 데이터를 찾을 수 없습니다.");
            return null;
        }

        Debug.Log($"[보상체크] 데이터 찾음! 첫보상 개수: {details.firstRewards.Count}, 드롭템 개수: {details.dropItems.Count}");

        if (details != null)
        {
            // A. 첫 클리어 보상 (아직 클리어한 적이 없을 때만)
            if (!history.isFirstRewardClaimed)
            {
                foreach (var first in details.firstRewards)
                {
                    GiveItem(first.itemID, first.count);
                    AddRewardToDisplayList(first.itemID, first.count);
                }
                history.isFirstRewardClaimed = true;
            }

            // B. 일반 드롭 보상
            foreach (var drop in details.dropItems)
            {
                float roll = UnityEngine.Random.Range(0f, 100f);
                Debug.Log($"[드롭체크] 아이템:{drop.itemID}, 확률:{drop.chance}%, 결과:{roll}"); // 이 로그가 중요!

                if (roll <= drop.chance)
                {
                    GiveItem(drop.itemID, drop.count);
                    AddRewardToDisplayList(drop.itemID, drop.count);
                }
            }
        }

        // 3. 저장
        history.isCleared = true;
        SaveData();

        return _lastEarnedRewards;
    }

    private void GiveItem(int id, int count)
    {
        // 1. 재화(Currency)인 경우 직접 변수 수정
        if (id == 1001) // 1번이 골드
        {
            userData.gold += count;
            Debug.Log($"골드 획득: {count}, 현재: {userData.gold}");
        }
        else if (id == 1002) // 2번이 다이아
        {
            userData.diamond += count;
        }
        // 2. 그 외의 모든 아이템은 인벤토리 리스트에서 관리
        else
        {
            AddToInventory(id, count);
        }
    }

    private void AddToInventory(int id, int count)
    {
        // 인벤토리에 이미 해당 아이템이 있는지 확인
        ItemInventoryData existingItem = userData.inventory.Find(x => x.itemID == id);

        if (existingItem != null)
        {
            // 이미 있다면 개수만 더함
            existingItem.count += count;
        }
        else
        {
            // 없다면 새로 생성해서 리스트에 추가
            userData.inventory.Add(new ItemInventoryData { itemID = id, count = count });
        }

        Debug.Log($"아이템 ID {id} 획득: {count}개");
    }

    public List<ItemInventoryData> GetLastEarnedRewards()
    {
        return _lastEarnedRewards;
    }

    private void AddRewardToDisplayList(int id, int count)
    {
        ItemInventoryData existingItem = _lastEarnedRewards.Find(x => x.itemID == id);
        if (existingItem != null)
            existingItem.count += count;
        else
            _lastEarnedRewards.Add(new ItemInventoryData { itemID = id, count = count });
    }
}
