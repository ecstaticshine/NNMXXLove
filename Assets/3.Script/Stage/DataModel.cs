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
public class StageRewardInfo{
    public int itemID; //아이템 ID
    public int count; // 개수
    public float chance; // 드롭 확률 (0.0 ~ 1.0, 첫 클리어는 보통 1.0)
}

// 3. 스테이지 전체 상세 정보 (정보 바인딩)
public class StageDetailData
{
    public string stageID;
    public string prevStageID;     // 이전 스테이지 ID (None이면 시작점)
    public Vector2 nodePos;     // 노드 좌표 (X, Y)
    public int staminaCost; // 소모 스테미나
    public List<StageEnemyInfo> enemies = new List<StageEnemyInfo>();
    public List<StageRewardInfo> firstRewards = new List<StageRewardInfo>(); // 첫 클리어 보상
    public List<StageRewardInfo> dropItems = new List<StageRewardInfo>();    // 일반 드롭 아이템
}