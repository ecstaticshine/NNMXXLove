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
    CharacterUpgrade,       //  Home -> CharacterList -> Upgrade
    CharacterCustomTag,     //  Home -> CharacterList -> Character -> CharacterCustomTag
    CharacterBreakThrough,  //  Home -> CharacterList -> Character -> CharacterBreakThrough
    Detail,                 //  Home -> CharacterList -> Character -> Detail
    WorldSelect,            //  Home -> Adventure -> StageSelect -> WorldSelect 
    StageSelect,            //  Home -> Adventure -> StageSelect
    StageDetailPopup,       //  Home -> Adventure -> StageSelect -> StageDetailPopup 
    Placement,              //  Home -> Adventure -> StageSelect -> StageDetailPopup -> Placement
    Stage,                  //  Home -> Adventure -> StageSelect -> StageDetailPopup -> Placement -> Stage
    Battle,                 //  Home -> Adventure -> StageSelect -> StageDetailPopup -> Placement -> Stage -> Battle
    Multi,                  //  Home -> Adventure -> Multi
    Story,                  //  Home -> StorySelect
    Prologue,
    Title,                  //  Title
}

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance;

    [Header("Global UI")]
    [SerializeField] private GameObject topUI;   // 배틀 씬 등에서 필요없을 경우 끄기.
    [SerializeField] private GameObject bottomUI;// 배틀 씬 등에서 필요없을 경우 끄기.

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Top Bar Controller")]
    [SerializeField] private TopBarUI topBarUI;

    [Header("World")]
    [SerializeField] private GameObject worldArea;
    [SerializeField] private TMP_Text worldNameText;

    [Header("BackButton")]
    [SerializeField] private GameObject BackButton; // 뒤로가기 버튼 오브젝트

    [Header("PlayerInfo")]
    [SerializeField] private GameObject PlayerInfo;

    [Header("SceneState")]
    [SerializeField]
    public SceneState currentState = SceneState.CharacterList;
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
        if (scene.name == "TitleScene")
        {
            topUI.SetActive(false);
            bottomUI.SetActive(false);
            PlayerInfo.SetActive(false);
            settingsPanel.SetActive(false);
            stateStack.Clear();
            currentState = SceneState.Title;
        }

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
        if (newState == SceneState.Settings)
        {
            HandleSettingsToggle();
            return;
        }

        if (!isBack && currentState != newState)
            stateStack.Push(currentState);

        currentState = newState;

        UpdateBackButton();
        HandleSceneTransition();
        UpdateUIByState();
    }
    private void HandleSettingsToggle()
    {
        bool isOpening = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isOpening);

        if (!isOpening)
        {
            // 닫을 때만 이전 상태 복원 + UI 갱신
            currentState = stateStack.Count > 0 ? stateStack.Pop() : SceneState.Home;
            UpdateBackButton();
            UpdateUIByState();
            RefreshCurrentUI();
        }
    }

    public void RefreshCurrentUI()
    {

        if (topBarUI != null) topBarUI.RefreshUI();

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

    private void HandleSceneTransition()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 2. 상태에 따른 실제 씬 전환 로직 추가
        switch (currentState)
        {
            case SceneState.Title:
                BackButton.SetActive(false);
                SetMainLayout(false);
                break;
            case SceneState.Home:
                SetMainLayout(true);
                LoadSceneIfNeeded("HomeScene", currentSceneName);
                break;
            case SceneState.Adventure:
            case SceneState.StageSelect:
            case SceneState.StageDetailPopup: // 추가
            case SceneState.Placement:        // 추가
                PlayerInfo.SetActive(false);
                topUI.SetActive(true);
                bottomUI.SetActive(true);
                if (!LoadSceneIfNeeded("AdventureScene", currentSceneName))
                    RefreshCurrentUI(); // 이미 AdventureScene이면 패널만 갱신
                break;
            case SceneState.Battle:
                SetBattleLayout(false);
                PlayerInfo.SetActive(false);
                LoadSceneIfNeeded("BattleScene", currentSceneName);
                SetBattleLayout(false);
                break;
            case SceneState.Gacha:
                topUI.SetActive(true);
                LoadSceneIfNeeded("GachaScene", currentSceneName);
                break;
            case SceneState.StorySelect:
                topUI.SetActive(true);
                bottomUI.SetActive(true);
                LoadSceneIfNeeded("StorySelectScene", currentSceneName);
                break;
            case SceneState.Story:
            case SceneState.Prologue:
                topUI.SetActive(false);
                PlayerInfo.SetActive(false);
                bottomUI.SetActive(false);
                LoadSceneIfNeeded("StoryScene", currentSceneName);
                break;
            case SceneState.CharacterList:
                topUI.SetActive(false);
                PlayerInfo.SetActive(false);
                LoadSceneIfNeeded("CharacterListScene", currentSceneName);
                break;
            case SceneState.CharacterUpgrade:
            case SceneState.CharacterCustomTag:
            case SceneState.CharacterBreakThrough:
                topUI.SetActive(false);
                PlayerInfo.SetActive(false);
                break;
            case SceneState.Settings:
                settingsPanel.SetActive(!settingsPanel.activeSelf);
                if (!settingsPanel.activeSelf)
                    currentState = stateStack.Count > 0 ? stateStack.Pop() : SceneState.Home;
                RefreshCurrentUI();
                break;

        }
    }

    private bool LoadSceneIfNeeded(string targetScene, string currentScene)
    {
        if (currentScene != targetScene)
        {
            SceneManager.LoadScene(targetScene);
            return true;
        }
        return false;
    }



    private void UpdateUIByState()
    {
        // 스테이지 상태일 때만 월드 버튼 보이게 설정
        worldArea.SetActive(currentState == SceneState.StageSelect);
    }

    public void OnTabMenuButtonClicked(int targetState)
    {
        SceneState target = (SceneState)targetState;

        if (currentState == target && target != SceneState.Settings) return;

        // Settings는 스택 클리어 없이 처리
        if (target != SceneState.Settings)
            stateStack.Clear();

        ChangeState(target, true);
    }

    public void SetBattleLayout(bool isActive)
    {
        topUI.SetActive(isActive);
        bottomUI.SetActive(isActive);
    }

    public void ClearStateStack()
    {
        stateStack.Clear();
    }

    private void SetMainLayout(bool isActive)
    {
        topUI.SetActive(isActive);
        bottomUI.SetActive(isActive);
        PlayerInfo.SetActive(isActive);
        gameObject.SetActive(isActive);
    }

    // 타이틀 초기화 후 세팅 패널 지워두기
    public void CloseSettingsPanel()
    {

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void UpdateBackButton()
    {
        bool isMainTab = currentState is SceneState.Home or SceneState.CharacterList
            or SceneState.StorySelect or SceneState.Adventure
            or SceneState.Gacha or SceneState.Settings;

        BackButton.SetActive(!isMainTab && stateStack.Count > 0 && currentState != SceneState.Battle);
    }
}
