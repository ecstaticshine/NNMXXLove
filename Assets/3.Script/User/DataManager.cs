using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterInfo
{
    public int unitID;      // 어떤 캐릭터인지 (ID)
    public int currentLevel;       // 현재 레벨
    public int currentAttack;      //
    public Rarity baseRarity;       // 태생 등급 (데이터시트에서 가져온 고정값)
    public Rarity currentRarity;    // 레어리티
    public int currentBreakthrough; // 돌파 단계
    public int currentExp;      // 필요하다면 경험치까지
    public string[] equippedTags = new string[4]; // 4개의 슬롯

    public int TotalPoint => GetTierOffset(baseRarity) + currentBreakthrough;

    private int GetTierOffset(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.L: return 0;
            case Rarity.PL: return 7;
            case Rarity.TL: return 14;
            case Rarity.EL: return 21;
            default: return 0;
        }
    }
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    // 데이터가 변경되었을 때 실행될 이벤트
    public Action OnDataChanged;

    public UserData userData;

    [Header("Stamina Settings")]
    public int maxStamina = 120;
    public int staminaRegenSeconds = 300;

    public string selectedStageID; //현재 진행중인 스테이지 (임시 저장)

    // 현재 로드된 월드의 상세 정보를 담아둘 변수 (추가)
    private WorldDataInfo currentWorldInfo;

    // 유저가 현재 위치한 월드 (UserData 등에서 가져옴)
    public int currentWorldIndex = 1;

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

    // 게임에 존재하는 캐릭터 풀
    public List<UnitData> allUnitDatas = new List<UnitData>();

    public int CurrentStamina => userData != null ? userData.stamina : 0;

    public enum Language { KO = 1, JP = 2 } // 0은 string키용
    public Language currentLanguage = Language.KO; // 기본값 - 인스펙터 창 확인

    public static event Action<UnitData, CharacterInfo> OnCharacterSelected;    //캐릭터 선택

    public StoryData selectedStoryData { get; set; } // 선택한 스토리 데이터

public static Action OnUserDataChanged; //유저 정보 변경

    // 로컬라이제이션맵
    private Dictionary<string, string> localizationMap = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PlayerPrefs.DeleteKey("SaveFile");

            LoadData();           // 유저 세이브 데이터 먼저
            InitializeLocalization();
            // 유저가 있는 월드 데이터만 로드
            LoadGameDataByWorld(currentWorldIndex);

            GiveItem(2001, 50);
            GiveItem(4001, 5);
            GiveItem(5001, 7);

            if (userData.stamina <= 0 && !PlayerPrefs.HasKey("SaveFile"))
            {
                userData.stamina = 100;
                userData.lastStaminaUpdateTime = DateTime.Now.ToString();
            }

            StartCoroutine(StaminaRegenerateRoutine()); // 자동 충전 시작!

        }
        else
        {
            Destroy(gameObject);
        }


    }

    public CharacterInfo GetUserUnitInfo(int id)
    {
        // 리스트에서 ID가 일치하는 정보를 찾고, 없으면 기본값(1레벨) 반환
        return userInventory.Find(info => info.unitID == id)
               ?? new CharacterInfo { unitID = id, currentLevel = 1, currentBreakthrough = 0 };
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

        Debug.Log("로컬 데이터 저장 완료");

        // [미래] 파이어베이스 연동 시 이곳에 추가
        // FirebaseManager.Instance.UploadUserData(json);
    }

    public void LoadData()
    {
        // 데이터가 없을 때 기본값 "{}" 대신 유효한 초기 데이터를 넣거나 체크
        if (!PlayerPrefs.HasKey("SaveFile"))
        {
            userData = new UserData(); // 객체 생성

            userData.InitDefaultData();

            SaveData();
            Debug.Log("신규 유저 초기 데이터 생성 완료");
        }
        else
        {
            string json = PlayerPrefs.GetString("SaveFile");
            userData = JsonUtility.FromJson<UserData>(json);
        }

        userInventory.Clear();
        foreach (CharacterSaveData charData in userData.ownedCharacters)
        {
            UnitData originalData = GetPlayerData(charData.unitID);
            Rarity rarity = (originalData != null) ? originalData.rarity : Rarity.L;

            Debug.Log(charData.unitID);
            userInventory.Add(new CharacterInfo
            {
                unitID = charData.unitID,
                currentLevel = charData.currentLevel,
                currentExp = charData.currentExp,
                currentBreakthrough = charData.currentBreakthrough,
                baseRarity = rarity,
                equippedTags = charData.customTags ?? new string[4]
            });
        }
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
            detail.staminaCost = int.Parse(row[4]);

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
            else if (string.Compare(stageID_str, worldKey) > 0)
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

        string[] lines = csv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // 데이터가 실제 몇 번째 컬럼에 있는지 확인 (enum 값 활용)
        int langCol = (int)currentLanguage;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 쉼표로 분리 (데이터 내부에 쉼표가 있다면 정규식이나 별도 파서 사용 권장)
            string[] split = lines[i].Trim().Split(',');

            if (split.Length > langCol)
            {
                string key = split[0].Trim();
                string value = split[langCol];

                value = value.Replace("\"", "").Replace("'", "");

                // 줄바꿈 기호(\n)를 실제 줄바꿈으로 변환
                localizationMap[key] = value.Replace("\\n", "\n");
            }
        }
        Debug.Log($"로컬라이징 로드 완료: {currentLanguage}");
    }

    // 어디서든 쓸 수 있는 번역 함수
    public string GetLocalizedText(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.Log("로컬라이징 키가 null이거나 비어있습니다!");
            return "Unknown";
        }

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
        target.customTags[slotIndex] = tagData.itemNameKey; //

        // 4. 인벤토리 개수 차감 및 저장
        itemInInv.count--;
        if (itemInInv.count <= 0) userData.inventory.Remove(itemInInv);

        SaveData(); //
        Debug.Log($"{target.name}에게 {tagData.itemNameKey} 장착 완료!");
    }

    public List<ItemInventoryData> CompleteStage(string stageID)
    {
        _lastEarnedRewards.Clear();
        Debug.Log($"[보상체크] 시작! 대상 스테이지 ID: {stageID}");

        // 1. 기록 갱신
        StageHistory history = userData.stageHistory.Find(x => x.stageID == stageID);
        if (history == null)
        {
            history = new StageHistory { stageID = stageID, isCleared = false, isFirstRewardClaimed = false };
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

        // 3. 플레이어는 스테미나의 10배 만큼 경험치 지급
        int earnedAccountExp = details.staminaCost * 10;
        AddAccountExp(earnedAccountExp);

        // 4. 저장
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
            Debug.Log($"다이아 획득: {count}, 현재: {userData.diamond}");
        }
        // 2. 그 외의 모든 아이템은 인벤토리 리스트에서 관리
        else
        {
            AddToInventory(id, count);
        }

        SaveData();
        OnDataChanged?.Invoke();
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

    public int GetRequiredExp(int level)
    {
        if (level >= 100) return int.MaxValue; // 만렙 100 설정
        return Mathf.FloorToInt(100f * Mathf.Pow(level, 1.2f));
    }

    public void UseExpItem(int unitID, int itemID, int useCount)
    {
        CharacterInfo unit = GetUserUnitInfo(unitID);
        if (unit == null || unit.currentLevel >= 100) return;

        // 아이템 ID에 따른 경험치 양 설정 (나중에 switch-case로 쉽게 추가 가능)
        ItemData data = GetItemData(itemID);
        if (data == null) return;

        // 2. 해당 아이템이 정말 경험치 관련 아이템인지 체크 (방어 코드)
        if (data.effectStatType != "EXP")
        {
            Debug.LogWarning($"{data.itemNameKey}은 경험치 아이템이 아닙니다!");
            return;
        }

        // 3. 데이터에 설정된 수치(effectValue)로 총 획득량 계산
        int expPerItem = Mathf.FloorToInt(data.effectValue);
        int totalGain = expPerItem * useCount;

        Debug.Log($"[강화 테스트] 아이템:{data.itemNameKey} | 개수:{useCount} | 획득EXP:{totalGain}");

        // 4. 경험치 적용 및 아이템 개수 차감 로직 실행
        AddExpToUnit(unit, totalGain);

        // 5. (중요) 실제 인벤토리에서 아이템 개수 줄이기
        RemoveInventoryItem(itemID, useCount);

    }

    public void RemoveInventoryItem(int id, int count)
    {
        // 1. 인벤토리에서 해당 아이템 찾기
        ItemInventoryData existingItem = userData.inventory.Find(x => x.itemID == id);

        if (existingItem != null)
        {
            existingItem.count -= count;

            // 2. 개수가 0 이하라면 리스트에서 제거
            if (existingItem.count <= 0)
            {
                userData.inventory.Remove(existingItem);
                Debug.Log($"[아이템 제거] ID {id}가 모두 소모되어 인벤토리에서 삭제되었습니다.");
            }
            else
            {
                Debug.Log($"[아이템 차감] ID {id} 사용. 남은 개수: {existingItem.count}");
            }

            // 3. 데이터 변경 저장 및 알림
            SaveData();
            OnDataChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[아이템 제거 실패] 인벤토리에 ID {id}가 없습니다.");
        }
    }

    private void AddExpToUnit(CharacterInfo unit, int amount)
    {
        unit.currentExp += amount;

        while (unit.currentLevel < 100 && unit.currentExp >= GetRequiredExp(unit.currentLevel))
        {
            unit.currentExp -= GetRequiredExp(unit.currentLevel);
            unit.currentLevel++;

            // 레벨업 시 스탯 갱신이 필요하다면 여기에 작성
            // UpdateUnitStats(unit);
            Debug.Log($"{unit.unitID} 레벨업! 현재 Lv.{unit.currentLevel}");
        }

        SaveData();
        OnUserDataChanged?.Invoke();
    }

    public bool TryConsumeStamina(string stageID)
    {
        StageDetailData stage = GetStageDetail(stageID);
        if (stage == null) return false;

        if (userData.stamina >= stage.staminaCost)
        {
            userData.stamina -= stage.staminaCost;
            SaveData(); // 깎인 직후 바로 저장!
            OnDataChanged?.Invoke(); // UI에게 알림 (아래 2번 참고)
            return true;
        }

        Debug.LogWarning("스태미나가 부족하여 스테이지에 진입할 수 없습니다.");
        return false;
    }


    private void RegenrateStamina()
    {
        // 최대치 이상이면 충전 중지
        if (userData.stamina >= maxStamina)
        {
            userData.lastStaminaUpdateTime = DateTime.Now.ToString();
            return;
        }

        DateTime lastTime;
        if (!DateTime.TryParse(userData.lastStaminaUpdateTime, out lastTime))
        {
            lastTime = DateTime.Now;
        }

        TimeSpan span = DateTime.Now - lastTime;
        int secondsPassed = (int)span.TotalSeconds;

        if (secondsPassed >= staminaRegenSeconds)
        {
            int regenAmount = secondsPassed / staminaRegenSeconds;
            int remainingSeconds = secondsPassed % staminaRegenSeconds;

            userData.stamina = Mathf.Min(userData.stamina + regenAmount, maxStamina);

            // 정산 완료 후, 남은 자투리 시간을 고려해 시간 업데이트
            userData.lastStaminaUpdateTime = DateTime.Now.AddSeconds(-remainingSeconds).ToString();

            OnDataChanged?.Invoke();
            SaveData();
            Debug.Log($"스테미나 자동 충전됨: 현재 {userData.stamina}");
        }
    }

    private IEnumerator StaminaRegenerateRoutine()
    {
        while (true)
        {
            RegenrateStamina();
            yield return new WaitForSeconds(1f); // 1초마다 체크
        }
    }

    public void AddAccountExp(int amount)
    {
        userData.currentExp += amount;

        // 계정 레벨업 체크 루프
        while (userData.currentExp >= GetRequiredExp(userData.currentLevel))
        {
            userData.currentExp -= GetRequiredExp(userData.currentLevel);
            userData.currentLevel++;

            Debug.Log($"[계정 레벨업] 축하합니다! Lv.{userData.currentLevel}이 되었습니다.");

            // 레벨업 보상 (예: 스태미나 전회복 등)
            userData.stamina = maxStamina;
        }

        OnDataChanged?.Invoke(); // UI(TopBarUI) 갱신 신호
        SaveData();
    }

    public void SetSelectedCharacter(UnitData data, CharacterInfo info)
    {
        // 씬 매니저나 다른 UI들이 이 소리를 듣고 각자 할 일을 합니다.
        OnCharacterSelected?.Invoke(data, info);
    }


    public List<ItemInventoryData> GetOwnedExpItems()
    {
        // inventory 리스트에서 ID가 2000 이상 3000 미만인 것만 추출
        return userData.inventory.FindAll(inv => inv.itemID >= 2000 && inv.itemID < 3000);
    }

    public List<ItemInventoryData> GetOwnedTagItems()
    {
        return userData.inventory.FindAll(inv => inv.itemID >= 4000 && inv.itemID < 5000);
    }

    public void EquipTag(CharacterInfo character, int itemID, int slotIndex)
    {
        // 소모 및 저장
        RemoveInventoryItem(itemID, 1);
        character.equippedTags[slotIndex] = itemID.ToString();

        SaveData();
        OnUserDataChanged?.Invoke();
    }

    public void RemoveTag(CharacterInfo character, int slotIndex)
    {
        character.equippedTags[slotIndex] = null;

        SaveData();
        OnUserDataChanged?.Invoke();
    }

    public (int hp, int atk, int spd) GetTotalTagStats(int unitID)
    {
        int totalHp = 0;
        int totalAtk = 0;
        int totalSpd = 0;

        CharacterInfo characterinfo = GetUserUnitInfo(unitID);

        if (characterinfo == null || characterinfo.equippedTags == null)
            return (0, 0, 0);

        for (int i = 0; i < characterinfo.equippedTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(characterinfo.equippedTags[i]))
            {
                int itemID = int.Parse(characterinfo.equippedTags[i]);
                ItemData data = GetItemData(itemID);

                int value = Mathf.RoundToInt(data.effectValue);

                switch (data.effectStatType.ToUpper())
                {
                    case "HP":
                        totalHp += value;
                        break;
                    case "ATK":
                    case "ATTACK": // 혹시 모를 오타 대비
                        totalAtk += value;
                        break;
                    case "SPD":
                    case "SPEED":
                        totalSpd += value;
                        break;
                    case "ALL": // 모든 능력치 상승 태그일 경우
                        totalHp += value;
                        totalAtk += value;
                        totalSpd += value;
                        break;
                }
            }
        }

        return (totalHp, totalAtk, totalSpd);
    }

    public void AddCharacter(int characterID)
    {
        // 이미 보유 중인지 확인
        if (IsCharacterOwned(characterID))
        {
            // 이미 있다면 조각으로 변환 (예: 5000번대 아이템)
            int pieceID = 5000 + characterID;
            int rewardAmount = 1; // 중복 획득 시 주는 조각 수
            GiveItem(pieceID, rewardAmount);
            Debug.Log($"{characterID} 중복! 조각 {pieceID}를 {rewardAmount}개 지급했습니다.");
        }
        else
        {
            // 처음 얻었다면 캐릭터 리스트에 추가
            AddNewCharacter(characterID);
            Debug.Log($"{characterID} 캐릭터를 처음 획득했습니다!");
        }
    }

    public void LoadAllUnitDatas()
    {
        // Resources/UnitDatas/Players 폴더 내의 모든 UnitData SO를 가져옴
        UnitData[] units = Resources.LoadAll<UnitData>("UnitDatas/Players");
        allUnitDatas.AddRange(units);
    }

    private bool IsCharacterOwned(int characterID)
    {
        return userData.ownedCharacters.Exists(x => x.unitID == characterID);
    }

    private void AddNewCharacter(int characterID)
    {
        UnitData originalData = GetPlayerData(characterID);
        Rarity rarity = (originalData != null) ? originalData.rarity : Rarity.L;

        // 저장용 데이터 생성 (UserData에 저장될 녀석)
        CharacterSaveData newSaveData = new CharacterSaveData
        {
            unitID = characterID,
            currentLevel = 1,
            currentExp = 0,
            currentBreakthrough = 0,
            customTags = new string[4]
        };
        userData.ownedCharacters.Add(newSaveData);

        // 인게임 정보 리스트에도 추가 (실제 데이터 관리용)
        userInventory.Add(new CharacterInfo
        {
            unitID = newSaveData.unitID,
            currentLevel = newSaveData.currentLevel,
            currentExp = newSaveData.currentExp,
            currentBreakthrough = newSaveData.currentBreakthrough,
            baseRarity = rarity,
            equippedTags = newSaveData.customTags
        });

        // 데이터 저장 및 UI 갱신 알림
        SaveData();
        OnDataChanged?.Invoke();
        OnUserDataChanged?.Invoke();
    }

    public void BreakthroughCharacter(int unitID)
    {
        CharacterInfo unit = GetUserUnitInfo(unitID);
        int pieceID = 5000 + unitID;

        // 1. 재고 확인
        ItemInventoryData piece = userData.inventory.Find(x => x.itemID == pieceID);

        if (piece != null && piece.count >= 1)
        {
            // 2. 조각 소모
            RemoveInventoryItem(pieceID, 1);

            // 3. 돌파 단계 상승
            unit.currentBreakthrough++;

            // 레어도 등급 로직 갱신
            int totalPt = unit.TotalPoint;
            if (totalPt >= 21) unit.currentRarity = Rarity.EL;
            else if (totalPt >= 14) unit.currentRarity = Rarity.TL;
            else if (totalPt >= 7) unit.currentRarity = Rarity.PL;
            else unit.currentRarity = Rarity.L;

            // 4. 세이브 데이터에도 동기화 (UserData 내 리스트 업데이트)
            var saveChar = userData.ownedCharacters.Find(x => x.unitID == unitID);
            if (saveChar != null) saveChar.currentBreakthrough = unit.currentBreakthrough;

            Debug.Log($"[돌파 성공] {unitID} 캐릭터가 {unit.currentBreakthrough}단계 돌파를 완료했습니다!");

            SaveData();
            OnUserDataChanged?.Invoke(); // UI 갱신 (빨간 점 알림 등을 끌 때 유용해요)
        }
        else
        {
            Debug.LogWarning("돌파에 필요한 캐릭터 조각이 부족합니다.");
        }
    }

    public List<ItemInventoryData> GiveStoryReward(int itemID, int count)
{
        // 1. 보상 리스트 생성 (결과창 연출용)
        List<ItemInventoryData> storyRewards = new List<ItemInventoryData>();

        // 2. 실제 아이템 지급
        GiveItem(itemID, count);

        // 3. 연출을 위해 리스트에 담기
        storyRewards.Add(new ItemInventoryData { itemID = itemID, count = count });

        // 4. (선택 사항) 스토리 전용 추가 보상이 있다면 여기서 처리
        // 예: 모든 스토리 클리어 시 계정 경험치 추가 등
        // AddAccountExp(10); 

        Debug.Log($"[Story Reward] ID: {itemID}, Count: {count} 지급 완료");

        return storyRewards; // 이 리스트를 받아 UI에서 '보상 획득!' 팝업을 띄울 수 있습니다.
    }

    public int GetCharacterIDByPiece(int pieceID)
    {
        if (pieceID >= 5000 && pieceID < 6000) { return pieceID - 5000; }

        return -1;
    }

    public ItemInventoryData GetOwnedItem(int itemID)
    {
        return userData.inventory.Find(x => x.itemID == itemID);
    }

    public bool IsStoryUnlocked(StoryData storyData)
    {
        // 1. 필수 스테이지 클리어 여부 확인
        bool stageCondition = string.IsNullOrEmpty(storyData.requiredStageID) ||
                              IsStageCleared(storyData.requiredStageID);

        // 2. 필수 이전 스토리 읽음 여부 확인
        bool storyCondition = string.IsNullOrEmpty(storyData.requiredStoryID) ||
                              userData.stageHistory.Exists(x => x.stageID == storyData.requiredStoryID && x.isStoryRead);

        return stageCondition && storyCondition;
    }
}

