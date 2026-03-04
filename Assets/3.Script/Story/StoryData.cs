using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryData", menuName = "ScriptableObject/StoryData")]
public class StoryData : ScriptableObject
{
    public string storyID;
    public string storyTitle;    // 스토리 제목 (예: 시작의 해변)
    public TextAsset storyCsv;  // 해당 스토리의 대사 CSV 파일

    [Header("해금 조건 (둘 다 만족해야 함)")]
    public string requiredStageID;    // 필수로 클리어해야 하는 스테이지 ID (예: W01S01)
    public string requiredStoryID;    // 필수로 읽어야 하는 이전 스토리 ID (예: S00)

    [Header("보상 설정")]
    public int rewardItemID;       // 보상 아이템 ID (예: 1001 코인)
    public int rewardCount;        // 보상 수량
}