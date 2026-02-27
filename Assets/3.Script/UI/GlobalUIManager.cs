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
    WorldSelect,            //  Home -> Adventure -> StageSelect -> WorldSelect 
    StageSelect,            //  Home -> Adventure -> StageSelect
    StageDetailPopup,       //  Home -> Adventure -> StageSelect -> StageDetailPopup 
    Placement,              //  Home -> Adventure -> StageSelect -> StageDetailPopup -> Placement
    Stage,                  //  Home -> Adventure -> StageSelect -> StageDetailPopup -> Placement -> Stage
    Battle,                 //  Home -> Adventure -> StageSelect -> StageDetailPopup -> Placement -> Stage -> Battle
    Multi,                  //  Home -> Adventure -> Multi

}

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance;

    [Header("Global UI")]
    [SerializeField] private GameObject topUI;   // 배틀 씬 등에서 필요없을 경우 끄기.
    [SerializeField] private GameObject bottomUI;// 배틀 씬 등에서 필요없을 경우 끄기.
    [Header("World")]
    [SerializeField] private GameObject worldArea;
    [SerializeField] private TMP_Text worldNameText;

    [Header("BackButton")]
    [SerializeField] private GameObject BackButton; // 뒤로가기 버튼 오브젝트

    [Header("PlayerInfo")]
    [SerializeField] private GameObject PlayerInfo;

    [Header("SceneState")]
    [SerializeField]
    private SceneState currentState = SceneState.CharacterList;
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀔 때마다 현재 상태에 맞춰 UI를 강제 동기화
        RefreshCurrentUI();
    }


    // 현상황 확인하기 
    public SceneState GetCurrentState()
    {
        return currentState;
    }

    public void SetWorldName(string worldName)
    {
        if (currentState != SceneState.StageSelect)
        {
            worldArea.SetActive(false);
            return;
        }

        worldArea.SetActive(true);
        worldNameText.text = worldName;
    }

    public void ChangeState(SceneState newState, bool isBack = false)
    {
        if (!isBack && currentState != newState)
        {
            stateStack.Push(currentState);
        }

        currentState = newState;

        SetBattleLayout(currentState != SceneState.Battle);

        bool isMainTab = (currentState == SceneState.Home ||
                      currentState == SceneState.CharacterList ||
                      currentState == SceneState.StorySelect ||
                      currentState == SceneState.Adventure ||
                      currentState == SceneState.Gacha);

        // 1. 뒤로가기 버튼 활성화
        BackButton.SetActive(!isMainTab && stateStack.Count > 0 && currentState != SceneState.Battle);

        string currentSceneName = SceneManager.GetActiveScene().name;

        // 2. 상태에 따른 실제 씬 전환 로직 추가
        switch (currentState)
        {
            case SceneState.Home:
                topUI.SetActive(true);
                bottomUI.SetActive(true);
                PlayerInfo.SetActive(true);
                if (currentSceneName != "HomeScene") SceneManager.LoadScene("HomeScene");
                break;
            case SceneState.Adventure:
            case SceneState.StageSelect:
            case SceneState.StageDetailPopup: // 추가
            case SceneState.Placement:        // 추가
                PlayerInfo.SetActive(false);
                topUI.SetActive(true);
                if (currentSceneName != "AdventureScene") { 
                SceneManager.LoadScene("AdventureScene");
                }
                else
                {
                    RefreshCurrentUI();
                }
                break;
            case SceneState.Battle:
                if (currentSceneName != "BattleScene")
                {
                    SceneManager.LoadScene("BattleScene");
                }
                break;
                break;
            case SceneState.Gacha:
                SceneManager.LoadScene("GachaScene");
                break;
            case SceneState.StorySelect:
                SceneManager.LoadScene("StoryScene");
                break;
            case SceneState.CharacterList:
                SceneManager.LoadScene("CharacterListScene");
                topUI.SetActive(false);
                PlayerInfo.SetActive(false);
                break;

        }

        // 3. UI 업데이트 호출 (월드 버튼 노출 등)
        UpdateUIByState();
    }

    public void RefreshCurrentUI()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "AdventureScene")
        {
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.SyncPanelWithState(currentState);
            }
        }
        // 필요하다면 다른 씬(CharacterList 등)의 동기화 로직도 여기에 추가 가능
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
        worldArea.SetActive(currentState == SceneState.StageSelect);
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
        topUI.SetActive(isActive);
    }

    public void ClearStateStack()
    {
        stateStack.Clear();
    }
}
