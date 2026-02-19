using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserData
{
    public int gold;    // 재화
    public int diamond;  // 다이아몬드
    public int stamina; // 스태미너

    // 각 유저가 보유한 모든 캐릭터들의 상세 정보 리스트
    public List<CharacterSaveData> ownedCharacters = new List<CharacterSaveData>();

    public List<ItemInventoryData> inventory = new List<ItemInventoryData>();

    // 스테이지 진행도 리스트
    public List<StageHistory> stageHistory = new List<StageHistory>();
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
}