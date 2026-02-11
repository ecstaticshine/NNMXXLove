using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public enum SceneState
{
    CharacterList,          //  Home -> CharacterList
    StorySelect,            //  Home -> StorySelect
    Adventure,              //  Home -> Adventure
    Home,
    Gacha,                  //  Home -> Gacha
    Settings,               //  Home -> Settings
    Character,              //  Home -> CharacterList -> Character
    CharacterCustomTag,     //  Home -> CharacterList -> Character -> CharacterCustomTag
    CharacterBreakThrough,  //  Home -> CharacterList -> Character -> CharacterBreakThrough
    WorldSelect,            //  Home -> Adventure -> WorldSelect -> WorldSelect 
    StageSelect,            //  Home -> Adventure -> StageSelect
    Multi,                  //  Home -> Adventure -> Multi

}

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance;

    [Header("Global UI")]
    [SerializeField] private GameObject globalUI;   // 배틀 씬 등에서 필요없을 경우 끄기.

    [Header("World")]
    [SerializeField] private GameObject worldButton;
    [SerializeField] private TMP_Text worldNameText;

    [Header("BackButton")]
    [SerializeField] private GameObject BackButton; // 뒤로가기 버튼 오브젝트

    private SceneState currentState = SceneState.Adventure;
    private Stack<SceneState> stateStack = new Stack<SceneState>(); // 씬 되돌아가기 위한 스택

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetWorldName(string worldName)
    {
        if (currentState != SceneState.StageSelect)
        {
            worldButton.SetActive(false);
            return;
        }

        worldButton.SetActive(true);
        worldNameText.text = worldName;
    }

    public void ChangeState(SceneState newState, bool isBack = false)
    {
        if (!isBack && currentState != newState)
        {
            stateStack.Push(currentState);
        }

        currentState = newState;

        bool isMainTab = (currentState == SceneState.Home ||
                      currentState == SceneState.CharacterList ||
                      currentState == SceneState.StorySelect ||
                      currentState == SceneState.Adventure ||
                      currentState == SceneState.Gacha);

        // 1. 뒤로가기 버튼 활성화
        BackButton.SetActive(!isMainTab && stateStack.Count > 0);

        // 2. 상태에 따른 실제 씬 전환 로직 추가
        switch (currentState)
        {
            case SceneState.Home:
                SceneManager.LoadScene("HomeScene");
                break;
            case SceneState.Adventure:
                SceneManager.LoadScene("AdventureScene"); // 월드맵 씬으로 이동
                break;
            case SceneState.Gacha:
                SceneManager.LoadScene("GachaScene");
                break;
            case SceneState.StorySelect:
                SceneManager.LoadScene("StoryScene");
                break;
            case SceneState.CharacterList:
                SceneManager.LoadScene("CharacterListScene");
                break;

        }

        // 3. UI 업데이트 호출 (월드 버튼 노출 등)
        UpdateUIByState();
    }

    // 뒤로가기 버튼에 연결할 함수
    public void OnBackButtonClicked()
    {
        if (stateStack.Count > 0)
        {
            // 스택에서 이전 상태를 꺼내서 돌아감
            SceneState previousState = stateStack.Pop();
            Debug.Log(previousState);
            ChangeState(previousState, true);
        }
        else
        {
            // 스택이 비어있다면 무조건 홈으로!
            ChangeState(SceneState.Home, true);
        }
    }

    private void UpdateUIByState()
    {
        // 스테이지 상태일 때만 월드 버튼 보이게 설정
        worldButton.SetActive(currentState == SceneState.StageSelect);
    }

    public void OnTabMenuButtonClicked(int targetState)
    {
        SceneState target = (SceneState)targetState;
        if (currentState == target)
        {
            return;
        }

        // 1. 하단 탭으로 이동할 경우 기존의 스택 클리어
        stateStack.Clear();


        // 2. 뒤로가기 버튼 숨기기
        ChangeState(target, true); // true를 넣어서 현재 상태가 스택에 쌓이지 않게 합니다.
    }

    public void SetBattleLayout(bool isActive)
    {
        globalUI.SetActive(isActive);
    }
}
