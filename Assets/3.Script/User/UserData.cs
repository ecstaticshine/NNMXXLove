using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserData
{
    public int gold;    // 재화
    public int stamina; // 스태미너

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