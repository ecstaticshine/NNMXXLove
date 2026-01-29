using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattlePhase { None, PlayerPhase, EnemyPhase, BattleEnd }

public class BattleManager : MonoBehaviour
{
    //턴제 배틀 시작. //1턴 아군 -> 적군
    [SerializeField]
    private BattlePhase currentPhase = BattlePhase.None;

    public int turnCount = 1;

    public List<BattleController> playerUnits;
    public List<BattleController> enemyUnits;

    //배틀 타이머
    public BattleTimer battleTimer;

    //BattleUIManager
    public BattleUIManager uiManager;

    //퍼즈
    private bool isPaused = false;

    public List<CharacterStat> allCharacters = new List<CharacterStat>();
    public List<CharacterStat> allEnemys = new List<CharacterStat>();

    public static BattleManager instance = null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void TogglePause()
    {
        if (currentPhase.Equals(BattlePhase.BattleEnd)) return;
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            Debug.Log("일시정지: 대마법사가 생각을 정리 중입니다...");
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("재개: 신부들을 향한 여정이 계속됩니다!");
        }
    }

    private void Start()
    {
        battleTimer.OnTimerOut += HandleTimerOut;

        EnterTurnStart();
    }


    // 턴 시작
    void EnterTurnStart()
    {
        // 타이머 시작
        battleTimer.StartTimer();

        // 턴 시작 시, 아군 페이즈부터
        currentPhase = BattlePhase.PlayerPhase;

        // 아군 페이즈
        uiManager.OnPhaseChanged(currentPhase);
        OnPhaseChanged(currentPhase);
    }

    public void EndBattle(bool victory)
    {
        battleTimer.StopTimer();

        uiManager.ShowResult(victory);
    }
    public void OnPhaseChanged(BattlePhase battlePhase)
    {
        // 상태 업데이트
        currentPhase = battlePhase;

        uiManager.OnPhaseChanged(battlePhase);

        switch (battlePhase)
        {
            case BattlePhase.PlayerPhase:
                battleTimer.StartTimer();
                Debug.Log("플레이어의 턴입니다.");
                break;
            case BattlePhase.EnemyPhase:
                battleTimer.StopTimer();
                break;
            case BattlePhase.BattleEnd:
                break;
        }
    }

    private void HandleTimerOut()
    {
        if (currentPhase.Equals(BattlePhase.PlayerPhase))
        {
            currentPhase = BattlePhase.BattleEnd;

            EndBattle(false);
        }
    }

    public void RemoveUnit()
    {

    }
}
