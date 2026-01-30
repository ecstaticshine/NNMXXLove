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

    //배틀 타이머
    public BattleTimer battleTimer;

    //BattleUIManager
    public BattleUIManager uiManager;

    //퍼즈
    private bool isPaused = false;

    public List<Unit> playerTurnOrder = new List<Unit>();
    public List<Unit> enemyTurnOrder = new List<Unit>();

    public Dictionary<int, Unit> playerSlot = new Dictionary<int, Unit>();
    public Dictionary<int, Unit> enemySlot = new Dictionary<int, Unit>();

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
        InitBattleUnits();

        battleTimer.OnTimerOut += HandleTimerOut;

        EnterTurnStart();
    }

    private void InitBattleUnits()
    {
        playerSlot.Clear();
        enemySlot.Clear();
        playerTurnOrder.Clear();
        enemyTurnOrder.Clear();

        Unit[] unitsInfield = FindObjectsByType<Unit>(FindObjectsSortMode.None);

        foreach (Unit unit in unitsInfield)
        {
            string parentName = unit.transform.parent.parent.name;

            if (parentName.Contains("Slot_"))
            {
                string indexStr = parentName.Replace("Slot_", "");
                if (int.TryParse(indexStr, out int index))
                {
                    unit.SetSlotIndex(index);
                }
            }

            // 이제 어느 팀인지에 따라 맵에 등록합니다.
            if (unit.data.isEnemy)
            {
                enemySlot[unit.GetSlotIndex()] = unit;
                enemyTurnOrder.Add(unit);
            }
            else
            {
                playerSlot[unit.GetSlotIndex()] = unit;
                playerTurnOrder.Add(unit);
            }
        }
        // 스피드 빠른 순서로 재정렬
        SortTurnOrder();


    }


    private void SortTurnOrder()
    {
        playerTurnOrder.Sort((a, b) =>
        {
            return b.GetCurrentSpeed().CompareTo(a.GetCurrentSpeed());
        });

        enemyTurnOrder.Sort((a, b) =>
        {
            return b.GetCurrentSpeed().CompareTo(a.GetCurrentSpeed());
        });
    }
    public void RegisterUnitToSlot(int slotIndex, Unit unit)
    {
        if (unit.data.isEnemy)
            enemySlot[slotIndex] = unit;
        else
            playerSlot[slotIndex] = unit;

        Debug.Log($"{slotIndex}번 슬롯에 {unit.data.unitName} 님이 배치되었습니다!");
    }

    public void TestBattle()
    {
        if (playerSlot.Count > 0 && enemySlot.Count > 0)
        {
            // 첫 번째 아군이 첫 번째 적을 공격!
            Unit attacker = playerTurnOrder[0];
            Unit target = enemyTurnOrder[0];

            Debug.Log($"{attacker.data.unitName}의 공격!");
            target.TakeDamage(attacker.GetCurrentAttack());
        }
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
                uiManager.RefreshTimeline(playerTurnOrder);
                battleTimer.StartTimer();
                Debug.Log("플레이어의 턴입니다.");
                break;
            case BattlePhase.EnemyPhase:
                uiManager.RefreshTimeline(enemyTurnOrder);
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

    public void RemoveUnit(Unit unit)
    {
        int slotIdx = unit.GetSlotIndex();

        if (unit.data.isEnemy)
        {
            // 딕셔너리에서 해당 슬롯 번호(Key)를 삭제합니다.
            if (enemySlot.ContainsKey(slotIdx))
            {
                enemySlot.Remove(slotIdx);
            }
            if (enemyTurnOrder.Contains(unit))
            {
                enemyTurnOrder.Remove(unit);
            }

            uiManager.RefreshTimeline(enemyTurnOrder);

            Debug.Log($"{unit.data.unitName} 적을 물리쳤습니다. 남은 적: {enemySlot.Count}명");

            // 승리 조건 체크 (딕셔너리의 개수가 0인지 확인)
            if (enemySlot.Count <= 0)
            {
                EndBattle(true);
            }
        }
        else
        {
            if (playerSlot.ContainsKey(slotIdx))
            {
                playerSlot.Remove(slotIdx);
            }
            if (playerTurnOrder.Contains(unit))
            {
                playerTurnOrder.Remove(unit);
            }

            uiManager.RefreshTimeline(playerTurnOrder);

            Debug.Log($"{unit.data.unitName} 아군이 퇴각했습니다... 남은 아군: {playerSlot.Count}명");

            if (playerSlot.Count <= 0)
            {
                EndBattle(false);
            }
        }

        // 전장에서 오브젝트를 제거합니다.
        Destroy(unit.gameObject);
    }
}
