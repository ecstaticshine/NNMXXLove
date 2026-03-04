using System.Collections.Generic;
using UnityEngine;

public class StoryListManager : MonoBehaviour
{
    public GameObject buttonPrefab;  // 버튼 프리팹
    public Transform contentParent; // 버튼 프리팹 들어갈 곳
    public List<StoryData> storyDataList; // 현재 만들어진 스토리 데이터 들어갈 곳

    void Start()
    {
        GenerateButtons();
    }

    void GenerateButtons()
    {
        // 1. 기존에 혹시 있을지 모를 버튼들을 청소합니다.
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 리스트에 있는 데이터 개수만큼 버튼을 만듭니다.
        foreach (StoryData data in storyDataList)
        {
            // 프리팹 복사본 생성
            GameObject newBtnObj = Instantiate(buttonPrefab, contentParent);

            // 버튼 스크립트 가져와서 Setup 실행!
            StoryButton btnScript = newBtnObj.GetComponent<StoryButton>();

            bool isUnlocked = IsStoryUnlocked(data, DataManager.Instance.userData);

            btnScript.Setup(data, isUnlocked);
        }
    }

    public bool IsStoryUnlocked(StoryData data, UserData user)
    {
        // 1. 선행 전투(Stage)가 조건인 경우
        if (!string.IsNullOrEmpty(data.requiredStageID))
        {
            var stageLog = user.stageHistory.Find(x => x.stageID == data.requiredStageID);
            // 전투를 안 깼으면 통과 못 함!
            if (stageLog == null || !stageLog.isCleared) return false;
        }

        // 2. 선행 스토리(Story)가 조건인 경우
        if (!string.IsNullOrEmpty(data.requiredStoryID))
        {
            var storyLog = user.stageHistory.Find(x => x.stageID == data.requiredStoryID);
            // 이전 스토리를 안 읽었으면 통과 못 함!
            if (storyLog == null || !storyLog.isStoryRead) return false;
        }

        return true; // 두 관문을 다 통과해야 해금!
    }
}