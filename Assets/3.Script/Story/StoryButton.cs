using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StoryButton : MonoBehaviour
{
    public TMP_Text titleText;
    private StoryData data;
    private bool isUnlocked;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    // 매니저가 버튼을 생성한 후 이 함수를 호출해서 데이터를 세팅
    public void Setup(StoryData newData, bool isUnlocked)
    {
        data = newData;
        titleText.text = isUnlocked ? data.storyTitle : "???";
        GetComponent<Button>().interactable = isUnlocked;

        button.image.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
    }

    // 버튼이 클릭됐을 때 실행될 함수
    public void OnClickButton()
    {
        if (data == null) return;
        
        // 선택한 스토리 데이터 넣기.
        DataManager.Instance.selectedStoryData = data;

        // 1. 스토리 전용 씬으로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("StoryScene");
    }
}