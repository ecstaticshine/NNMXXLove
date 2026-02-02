using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattlePhase { None, PlayerSelectPhase, PlayerActionPhase, EnemyPhase, BattleEnd }

public class BattleManager : MonoBehaviour
{
    //턴제 배틀 시작. //1턴 아군 -> 적군
    [SerializeField]
    private BattlePhase currentPhase = BattlePhase.None;

    public int turnCount = 0;

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

    private int _actionIndex = 0;

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

    public void OnAttackButtonClicked()
    {
        if (currentPhase != BattlePhase.PlayerSelectPhase) return;

        OnPhaseChanged(BattlePhase.PlayerActionPhase);


    }

    private void EnterEnemyPhase()
    {
        OnPhaseChanged(BattlePhase.EnemyPhase);

        // 공격자는 적군 리스트, 타겟은 플레이어 리스트!
        StartCoroutine(ExecutePhaseActions(enemyTurnOrder, playerTurnOrder));
    }

    private IEnumerator ExecutePhaseActions(List<Unit> attackers, List<Unit> targets)
    {
        foreach (Unit attacker in attackers)
        {
            if (attacker == null) continue;
            if (targets.Count <= 0) break;

            //타겟 찾기
            Unit target = GetTarget(attacker);
            if (target == null) continue;

            Debug.Log($"{attacker.data.unitName}이(가) {target.data.unitName}을(를) 조준!");

            // 공격 애니메이션 실행 (공격 상태로 변경)
            attacker.GetComponentInChildren<UnitAnimationController>().SetState(UnitAnimState.Attack);
            // 타격 시점 대기 (애니메이션 박자에 맞춰 조절)
            yield return new WaitForSeconds(0.4f);

            switch (attacker.data.unitType)
            {
                case UnitType.Healer:   // 힐러
                    int healAmount = Mathf.RoundToInt(attacker.GetCurrentAttack() * attacker.data.skillMultiplier);
                    target.Heal(healAmount);
                    break;
                case UnitType.Buffer:
                    break;
                default:              // 딜러
                    // 실제 대미지 적용
                    target.TakeDamage(attacker.GetCurrentAttack());
                    break;
            }

            // 다음 유닛이 나가기 전까지 대기 (복귀 시간 포함)
            yield return new WaitForSeconds(0.6f);

            if (enemySlot.Count <= 0 || playerSlot.Count <= 0) yield break;

        }

        // 모든 유닛 행동 종료 후 잠시 대기
        yield return new WaitForSeconds(0.5f);

        // 아군 공격이 끝났다면? 적군 턴으로!
        if (attackers == playerTurnOrder)
            EnterEnemyPhase();
        else // 적군 공격이 끝났다면? 다시 아군 선택으로!
            EnterTurnStart();

    }

    private Unit GetTarget(Unit attacker)
    {
        //적군 찾기
        Dictionary<int, Unit> targetSlots = attacker.data.isEnemy ? playerSlot : enemySlot;

        //아군 찾기
        Dictionary<int, Unit> allySlots = attacker.data.isEnemy ? enemySlot : playerSlot;

        int mySlot = attacker.GetSlotIndex();

        switch (attacker.data.unitType)
        {
            case UnitType.Healer:
                if (mySlot < 3) // 전열 먼저
                {
                    return GetLowestHPInLine(0, allySlots);
                }
                else if (mySlot < 6)
                {
                    //중열일 경우, 중열 먼저
                    return GetLowestHPInLine(3, allySlots);
                }
                else
                {
                    //후열일 경우, 제일 피가 적은 아군을 담당
                    return GetLowestHPTarget(allySlots);
                }
                break;
            case UnitType.Buffer:
                break;
            default:    // 딜러
                if (mySlot < 3) // 전열
                {
                    return GetFrontlineTarget(mySlot, targetSlots);
                }
                else if (mySlot < 6)
                {
                    //중열
                    return GetHighestAttackTarget(targetSlots);
                }
                else
                {
                    //후열일 경우, 제일 피가 적은 적군을 노림
                    return GetLowestHPTarget(targetSlots);
                }
                break;
        }

        return null;
    }

    private Unit GetLowestHPInLine(int startIndex, Dictionary<int, Unit> allySlots)
    {
        Unit bestTarget = null;
        float minHp = float.MaxValue;

        for (int i = startIndex; i < startIndex + 3; i++)
        {
            if (allySlots.TryGetValue(i, out Unit ally))
            {
                if (ally.GetCurrentHP() < minHp)
                {
                    minHp = ally.GetCurrentHP();
                    bestTarget = ally;
                }
            }
        }
        return bestTarget;
    }

    private Unit GetFrontlineTarget(int mySlot, Dictionary<int, Unit> targetSlots)
    {
        // 같은 줄 확인
        Unit target = ScanVerticalLine(mySlot, targetSlots);
        if (target != null) return target;

        // 같은 줄 확인 후 없으면 오른쪽 줄 확인
        for (int nextCol = mySlot + 1; nextCol < 3; nextCol++)
        {
            target = ScanVerticalLine(nextCol, targetSlots);
            if (target != null) return target;
        }

        // 왼쪽 줄 확인
        for (int preCol = 0; preCol < mySlot; preCol++)
        {
            target = ScanVerticalLine(preCol, targetSlots);
            if (target != null) return target;
        }

        return null;
    }

    private Unit GetHighestAttackTarget(Dictionary<int, Unit> targets)
    {

        Unit bestTarget = null;
        float maxAtk = -1f;

        // 최고 공격력의 적을 갱신
        foreach (var target in targets.Values)
        {
            if (target.GetCurrentAttack() > maxAtk)
            {
                maxAtk = target.GetCurrentAttack();
                bestTarget = target;
            }
        }
        return bestTarget;
    }

    private Unit GetLowestHPTarget(Dictionary<int, Unit> targets)
    {

        Unit bestTarget = null;
        float minHP = float.MaxValue;

        // 피가 적은 캐릭터 찾기
        foreach (var target in targets.Values)
        {
            if (target.GetCurrentHP() < minHP)
            {
                minHP = target.GetCurrentHP();
                bestTarget = target;
            }
        }
        return bestTarget;
    }

    // 같은 줄 확인
    private Unit ScanVerticalLine(int startSlot, Dictionary<int, Unit> slots)
    {
        // 정면(0,1,2) -> 중간(3,4,5) -> 후방(6,7,8) 순서로 체크
        if (slots.ContainsKey(startSlot)) return slots[startSlot];
        if (slots.ContainsKey(startSlot + 3)) return slots[startSlot + 3];
        if (slots.ContainsKey(startSlot + 6)) return slots[startSlot + 6];

        return null;
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
        currentPhase = BattlePhase.PlayerSelectPhase;

        // 턴 바꾸기
        uiManager.UpdateTurnUI(turnCount);

        // 아군 페이즈
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
            case BattlePhase.PlayerSelectPhase:
                uiManager.RefreshTimeline(playerTurnOrder);
                battleTimer.StartTimer();
                Debug.Log("플레이어의 턴입니다.");
                break;
            case BattlePhase.PlayerActionPhase:
                battleTimer.StopTimer();
                StartCoroutine(ExecutePhaseActions(playerTurnOrder, enemyTurnOrder));
                break;
            case BattlePhase.EnemyPhase:
                uiManager.RefreshTimeline(enemyTurnOrder);
                StartCoroutine(ExecutePhaseActions(enemyTurnOrder, playerTurnOrder));
                break;
            case BattlePhase.BattleEnd:
                break;

        }
    }

    private void HandleTimerOut()
    {
        if (currentPhase.Equals(BattlePhase.PlayerSelectPhase))
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
