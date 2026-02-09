using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public UserData userData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
            LoadData();
        }
        else
        {
            Destroy(this);
        }
    }

    public bool IsStageUnlocked(string stageID, string prevStageID)
    {
        if (prevStageID == "None") return true;
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
}
