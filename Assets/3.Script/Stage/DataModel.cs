using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StageEnemyInfo{
    public int slotIndex;   // 들어갈 슬롯
    public int unitID;  // 유닛ID
    public int level;   //레벨
    public string rarity;// 레어리티
    public string type; // 캐릭터인지, 유닛인지
    public string tag;  //  (Direct, Splash, Dot)
    public float multiplier;    // 유닛 강도
}

[System.Serializable]
public class ItemDropData
{
    public int itemID;
    public int count;
    public float chance;
    public bool isFirstReward;
}

// 3. 스테이지 전체 상세 정보 (정보 바인딩)
public class StageDetailData
{
    public string stageID;
    public string prevStageID;     // 이전 스테이지 ID (None이면 시작점)
    public Vector2 nodePos;     // 노드 좌표 (X, Y)
    public int staminaCost; // 소모 스테미나
    public List<StageEnemyInfo> enemies = new List<StageEnemyInfo>();
    public List<ItemDropData> firstRewards = new List<ItemDropData>(); // 첫 클리어 보상
    public List<ItemDropData> dropItems = new List<ItemDropData>();    // 일반 드롭 아이템
}

