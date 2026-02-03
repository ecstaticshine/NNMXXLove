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

    // 퍼즈
    private bool isPaused = false;
    // AutoBattle
    public bool isAutoBattle = false;
    // 배속
    [Header("Game speed")]
    public float currentSpeed = 1f; // 기본 1배속

    public List<Unit> playerTurnOrder = new List<Unit>();
    public List<Unit> enemyTurnOrder = new List<Unit>();

    public Dictionary<int, Unit> playerSlot = new Dictionary<int, Unit>();
    public Dictionary<int, Unit> enemySlot = new Dictionary<int, Unit>();

    public static BattleManager instance = null;

    public SynergyManager synergyManager = new SynergyManager();

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

    private void RefreshSynergies()
    {
        synergyManager.UpdateSynergy(playerSlot);
        synergyManager.UpdateSynergy(enemySlot);
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

    public void ToggleAutoBattle()
    {
        isAutoBattle = !isAutoBattle;

        // UI 업데이트 호출 (추가된 부분)
        uiManager.UpdateAutoBattleUI(isAutoBattle);

        Debug.Log(isAutoBattle ? "AutoBattle" : "Not AutoBattle");

        if (isAutoBattle && currentPhase == BattlePhase.PlayerSelectPhase)
        {
            OnAttackButtonClicked();
        }
    }

    public void ChangeGameSpeed()
    {
        if (currentSpeed == 1f) currentSpeed = 2f;
        else if (currentSpeed == 2f) currentSpeed = 3f;
        else currentSpeed = 1f;

        // 일시정지 상태가 아닐 때만 즉시 적용
        if (Time.timeScale != 0f)
        {
            Time.timeScale = currentSpeed;
        }

        uiManager.UpdateSpeedUI(currentSpeed);
    }

    public void OnAttackButtonClicked()
    {
        if (currentPhase != BattlePhase.PlayerSelectPhase) return;

        OnPhaseChanged(BattlePhase.PlayerActionPhase);

        StartCoroutine(ExecutePhaseActions(playerTurnOrder, enemyTurnOrder));
    }

    private void EnterEnemyPhase()
    {
        OnPhaseChanged(BattlePhase.EnemyPhase);

        // 공격자는 적군 리스트, 타겟은 플레이어 리스트!
        StartCoroutine(ExecutePhaseActions(enemyTurnOrder, playerTurnOrder));
    }


    private IEnumerator ExecutePhaseActions(List<Unit> attackers, List<Unit> targets)
    {
        // 페이즈 시작 전 시너지 한 번 갱신
        RefreshSynergies();

        foreach (Unit attacker in attackers)
        {
            if (attacker == null) continue;
            if (targets.Count <= 0) break;

            //타겟 찾기
            Unit target = GetTarget(attacker);
            if (target == null) continue;

            // 시너지 데이터 가지고 오기
            SynergyEffect eff = attacker.data.isEnemy ? new SynergyEffect() : synergyManager.currentEffect;

            Debug.Log($"{attacker.data.unitName}이(가) {target.data.unitName}을(를) 조준!");

            // 행동
            switch (attacker.data.unitType)
            {
                case UnitType.Healer:   // 힐러
                    // 공격 애니메이션 실행
                    attacker.GetComponentInChildren<UnitAnimationController>().SetState(UnitAnimState.Attack);

                    // 타격 시점 대기 (애니메이션 박자에 맞춰 조절)
                    yield return new WaitForSeconds(0.4f);

                    int healAmount = Mathf.RoundToInt(attacker.GetCurrentAttack() * attacker.data.skillMultiplier);
                    Debug.Log($"{healAmount} 만큼 힐합니다.");
                    target.Heal(healAmount);
                    break;
                case UnitType.Buffer:
                    // 쉴드 적용
                    yield return StartCoroutine(ExecuteBufferAction(attacker, target, eff));
                    break;
                default:  // 딜러
                    // 실제 대미지 적용
                    yield return StartCoroutine(ExecuteDealerAction(attacker, target, eff));
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
            case UnitType.Buffer:
                if (mySlot < 3) // 전열 - 자신과 같은 전열 아군들의 방어력/피해 감소 버프 (탱킹 강화)
                {
                    return GetLowestHPInLine(0, allySlots);
                }
                else if (mySlot < 6) //중열/후열 딜러들의 공격력 버프 (화력 집중)
                {
                    //중열일 경우, 중열 먼저
                    return GetHighestAttackTarget(allySlots);
                }
                else
                {       //아군 전체의 **스피드(Speed)**를 소량 상승
                    //후열일 경우, 제일 피가 적은 아군을 담당
                    return GetLowestHPTarget(allySlots);
                }
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
        }

        return null;
    }

    private Unit GetLowestHPInLine(int startIndex, Dictionary<int, Unit> allySlots)
    {
        Unit bestTarget = null;
        float minHpRatio = 1.1f;

        for (int i = startIndex; i < startIndex + 3; i++)
        {
            if (allySlots.TryGetValue(i, out Unit ally))
            {
                float hpRatio = (float)ally.GetCurrentHP() / ally.GetMaxHP();

                if (hpRatio < minHpRatio)
                {
                    minHpRatio = hpRatio;
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
        turnCount++;

        uiManager.UpdateTurnUI(turnCount);

        // 타이머 시작
        battleTimer.StartTimer();

        // 아군 페이즈
        OnPhaseChanged(BattlePhase.PlayerSelectPhase);

        if (isAutoBattle)
        {
            Invoke("OnAttackButtonClicked", 0.2f);
        }
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
                break;
            case BattlePhase.EnemyPhase:
                uiManager.RefreshTimeline(enemyTurnOrder);
                break;
            case BattlePhase.BattleEnd:
                break;

        }
    }

    private void HandleTimerOut()
    {
        if (currentPhase.Equals(BattlePhase.PlayerSelectPhase))
        {
            if (isAutoBattle)
            {
                OnAttackButtonClicked();
            }
            else
            {
                currentPhase = BattlePhase.BattleEnd;

                EndBattle(false);
            }
        }
    }

    public void RemoveUnit(Unit unit)
    {
        int slotIdx = unit.GetSlotIndex();

        if (unit.data.isEnemy)
        {
            // 딕셔너리에서 해당 슬롯 번호(Key)를 삭제
            if (enemySlot.ContainsKey(slotIdx))
            {
                enemySlot.Remove(slotIdx);
            }
            if (enemyTurnOrder.Contains(unit))
            {
                enemyTurnOrder.Remove(unit);
            }

            // [수정] 현재 플레이어 페이즈라면 플레이어 리스트로 타임라인 유지
            if (currentPhase == BattlePhase.PlayerSelectPhase || currentPhase == BattlePhase.PlayerActionPhase)
            {
                uiManager.RefreshTimeline(playerTurnOrder);
            }
            else
            {
                uiManager.RefreshTimeline(enemyTurnOrder);
            }

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
    private IEnumerator ExecuteDealerAction(Unit attacker, Unit target, SynergyEffect eff)
    {

        Dictionary<int, Unit> targetSlots = attacker.data.isEnemy ? playerSlot : enemySlot;
        List<Unit> areaTargets = GetUnitsInArea(target, attacker.data.skillArea, targetSlots);

        // [Dot 태그인 경우] 시너지에 따라 연격 횟수 결정
        int attackCount = 1;
        if (attacker.data.defaultTag == "Dot")
            attackCount += eff.dotExtraHits;

        for (int i = 0; i < attackCount; i++)
        {
            // 애니메이션 및 타격
            attacker.GetComponentInChildren<UnitAnimationController>().SetState(UnitAnimState.Attack);
            yield return new WaitForSeconds(0.4f);

            foreach (Unit areaUnit in areaTargets)
            {
                if (areaUnit == null) continue; // 그 사이 죽었을 수도 있으니 체크
                float finalDamage = attacker.GetCurrentAttack();

                // [Direct 태그인 경우] 피해 증폭
                if (attacker.data.defaultTag == "Direct")
                    finalDamage *= (1f + eff.directDamageMult);

                target.TakeDamage(Mathf.RoundToInt(finalDamage));

                // [Splash 태그인 경우] 주변 확산
                if (attacker.data.defaultTag == "Splash" && eff.splashBonus > 0)
                    ApplySplashDamage(target, finalDamage * eff.splashBonus, attacker.data.isEnemy);
            }

            yield return new WaitForSeconds(0.2f); // 연격 간격
        }
    }

    private List<Unit> GetUnitsInArea(Unit mainTarget, SkillArea area, Dictionary<int, Unit> targetSlots)
    {
        // 공격 범위 계산
        List<Unit> areaTargets = new List<Unit>();
        if (mainTarget == null) return areaTargets;

        // 공격할 메인 타겟 위치 확인
        int center = mainTarget.GetSlotIndex();
        // 슬롯 인덱스 확인
        List<int> targetIndices = new List<int>();

        // 스킬 범위에 맞춰서 범위 계산
        switch (area)
        {
            case SkillArea.Single:
                targetIndices.Add(center);
                break;

            case SkillArea.Row: // 가로줄 (0-1-2 / 3-4-5 / 6-7-8)
                int rowStart = (center / 3) * 3;
                // 가로줄 다 넣기
                for (int i = rowStart; i < rowStart + 3; i++) targetIndices.Add(i);
                break;

            case SkillArea.Column: // 세로줄 (0-3-6 / 1-4-7 / 2-5-8)
                int colStart = center % 3;
                // 세로 줄 다 넣기
                for (int i = colStart; i <= 8; i += 3) targetIndices.Add(i);
                break;

            case SkillArea.Cross: // 십자 (본인 + 상하좌우)
                targetIndices.Add(center);
                if (center % 3 != 0) targetIndices.Add(center - 1); // 왼쪽 끝이 아닐 때만 좌측 추가
                if (center % 3 != 2) targetIndices.Add(center + 1); // 오른쪽 끝이 아닐 때만 우측 추가
                targetIndices.Add(center - 3); // 상
                targetIndices.Add(center + 3); // 하
                break;

            case SkillArea.All: // 전체
                for (int i = 0; i < 9; i++) targetIndices.Add(i);
                break;
        }

        // 실제 존재하는 유닛만 필터링
        foreach (int idx in targetIndices)
        {
            if (targetSlots.TryGetValue(idx, out Unit u) && u != null)
                areaTargets.Add(u);
        }

        return areaTargets;
    }

    private void ApplySplashDamage(Unit mainTarget, float damage, bool isEnemyAttacker)
    {
        int centerSlot = mainTarget.GetSlotIndex();
        Dictionary<int, Unit> targets = isEnemyAttacker ? playerSlot : enemySlot;

        // 인접 슬롯 (상하좌우)
        int[] neighbors = { centerSlot - 1, centerSlot + 1, centerSlot - 3, centerSlot + 3 };
        foreach (int idx in neighbors)
        {
            if (targets.TryGetValue(idx, out Unit neighbor))
            {
                neighbor.TakeDamage(Mathf.RoundToInt(damage));
            }
        }
    }

    private IEnumerator ExecuteBufferAction(Unit attacker, Unit target, SynergyEffect eff)
    {
        int mySlot = attacker.GetSlotIndex();
        attacker.GetComponentInChildren<UnitAnimationController>().SetState(UnitAnimState.Attack);
        yield return new WaitForSeconds(0.4f);

        // 기반이 되는 쉴드량 계산 (버퍼의 공격력 * 스킬 계수)
        int baseShieldAmount = Mathf.RoundToInt(attacker.GetCurrentAttack() * attacker.data.skillMultiplier);

        if (mySlot < 3) // [전열] 강철의 벽
        {
            // 50%의 높은 내구도 / 횟수 1회 (강력한 한 방 방어)
            int shieldHP = Mathf.RoundToInt(baseShieldAmount * 0.5f);
            ApplyBuffToLine(0, attacker.data.isEnemy, (u) => {
                u.AddShield(1, shieldHP);
                Debug.Log($"{u.data.unitName}: 전열 쉴드 부여 (내구도: {shieldHP})");
            });
        }
        else if (mySlot < 6) // [중열] 다중 편광막
        {
            // 10%의 적당한 내구도 / 횟수 3회 (범위기 및 일반 공격 방어)
            int shieldHP = Mathf.RoundToInt(baseShieldAmount * 0.1f);
            if (target != null)
            {
                target.AddShield(3, shieldHP);
                Debug.Log($"{target.data.unitName}: 중열 쉴드 부여 (내구도: {shieldHP} / 3회)");
            }
        }
        else // [후열] 안개 장막
        {
            // 5%의 낮은 내구도 / 횟수 5회 (연타 및 짤딜 방어)
            int shieldHP = Mathf.RoundToInt(baseShieldAmount * 0.05f);
            Dictionary<int, Unit> allies = attacker.data.isEnemy ? enemySlot : playerSlot;
            foreach (var ally in allies.Values)
            {
                ally.AddShield(5, shieldHP);
            }
            Debug.Log($"아군 전체 쉴드 부여 (내구도: {shieldHP} / 5회)");
        }

        yield return new WaitForSeconds(0.2f);
    }

    // 라인(전열/중열/후열) 전체에 버프를 주는 함수
    private void ApplyBuffToLine(int startIndex, bool isEnemySide, Action<Unit> buffAction)
    {
        // 공격자가 적군이면 적군 슬롯에서, 아군이면 아군 슬롯에서 대상을 찾기.
        Dictionary<int, Unit> slots = isEnemySide ? enemySlot : playerSlot;

        // 해당 라인의 3개 슬롯(0-2, 3-5, 6-8)을 검사합니다.
        for (int i = startIndex; i < startIndex + 3; i++)
        {
            if (slots.TryGetValue(i, out Unit unit) && unit != null)
            {
                // 전달받은 버프 로직(쉴드 추가 등)을 실행합니다.
                buffAction?.Invoke(unit);
            }
        }
    }

}
