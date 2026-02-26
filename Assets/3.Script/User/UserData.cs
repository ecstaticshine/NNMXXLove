using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserData
{
    public string userId;       // 시스템용 고유 키 (수정 불가)
    public string playerName;   // 화면 표시용 이름 (수정 가능)
    public int currentLevel = 1;
    public int currentExp = 0;
    public int gold = 0;            // 재화
    public int diamond = 0;         // 다이아몬드
    public int stamina = 120;         // 스태미너
    public string lastStaminaUpdateTime; // 스테미나 회복 계산용 시간            

    // 각 유저가 보유한 모든 캐릭터들의 상세 정보 리스트
    public List<CharacterSaveData> ownedCharacters = new List<CharacterSaveData>();

    public List<ItemInventoryData> inventory = new List<ItemInventoryData>();

    // 스테이지 진행도 리스트
    public List<StageHistory> stageHistory = new List<StageHistory>();

    // [기본 생성자] JsonUtility가 로드할 때 사용
    public UserData() { }

    // [초기화용 생성자] 게임 처음 시작할 때 딱 한 번 호출
    public void InitDefaultData()
    {
        this.userId = Guid.NewGuid().ToString(); // 고유 ID 생성
        this.playerName = "New Commander";
        this.currentLevel = 1;
        this.currentExp = 0;
        this.gold = 5000;      // 초기 자금
        this.diamond = 1000;    // 초기 유료 재화
        this.stamina = 120;    // 최대치로 시작
        this.lastStaminaUpdateTime = DateTime.Now.ToString();

        // 초기 캐릭터 지급 (예: 1번 캐릭터)
        this.ownedCharacters.Add(new CharacterSaveData(1));
        this.ownedCharacters.Add(new CharacterSaveData(2));
    }
}

[System.Serializable]
public class StageHistory
{
    public string stageID;          // 스테이지 아이디 (예: W01S01)
    public bool isCleared;          // 클리어 여부
    public bool isFirstRewardClaimed; // 첫 보상 수령 여부
}

[Serializable]
public class ItemInventoryData
{
    public int itemID;      //아이템 아이디
    public int count;
}

[Serializable]
public class CharacterSaveData
{
    public int unitID;       // 어떤 캐릭터인지 (ID)
    public Rarity rarity;     // 현재 레어리티 (L, PL, TL 등)

    // 현재 적용 중인 외형
    public string currentSkin;

    public int currentLevel;        // 현재 레벨
    public int currentExp;          // 현재 경험치
    public int currentBreakthrough; // 돌파 단계

    // 캐릭터마다 개별적으로 붙인 태그 정보 (한 번 붙이면 끝!)
    public string[] customTags = new string[4]; //
    
    // 직렬화용 생성자
    public CharacterSaveData() { }

    // 실제 게임 내에서 캐릭터를 처음 생성할 때 호출하는 생성자
    public CharacterSaveData(int id)
    {
        this.unitID = id;
        this.currentLevel = 1;
        // 데이터를 생성하는 시점에 정확한 크기로 초기화합니다.
        this.customTags = new string[4] { "", "", "", "" };
    }
}