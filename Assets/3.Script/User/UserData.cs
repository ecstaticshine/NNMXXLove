using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageSaveData
{
    public string stageID;
    public bool isCleared;
}

[Serializable]
public class UserData
{
    public int gold;    // 재화
    public int stamina; // 스태미너

    // 스테이지 진행도 리스트
    public List<StageSaveData> stageHistory = new List<StageSaveData>();
}
