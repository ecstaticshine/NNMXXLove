using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattlePhase { None, PlayerSelectPhase, PlayerActionPhase, EnemyPhase, BattleEnd }

public class BattleManager : MonoBehaviour
{
    //턴제 배틀 시작. //1턴 아군 -> 적군
    [SerializeField]
    private BattlePhase _currentPhase = BattlePhase.None;
    public BattlePhase currentPhase => _currentPhase;

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

    // 순서
    public List<Unit> playerTurnOrder = new List<Unit>();
    public List<Unit> enemyTurnOrder = new List<Unit>();

    // 슬롯
    public Dictionary<int, Unit> playerSlot = new Dictionary<int, Unit>();
    public Dictionary<int, Unit> enemySlot = new Dictionary<int, Unit>();

    // 경험치 먹인 모든 캐릭터 리스트
    public List<Character> characterParties = new List<Character>();

    // 캐릭터 스폰 지역
    [Header("Slot Assignments")]
    public Transform[] playerSlotTransforms = new Transform[9];
    public Transform[] enemySlotTransforms = new Transform[9];

    public static BattleManager instance = null;

    public SynergyManager playerSynergy = new SynergyManager();
    public SynergyManager enemySynergy = new SynergyManager();

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
            return;
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
        RefreshSynergies();

        battleTimer.OnTimerOut += HandleTimerOut;

        if (uiManager != null)
        {
            uiManager.ShowStartUI();
        }
        else
        {
            // UI 매니저가 없을 경우를 대비한 안전장치
            BattleStart();
        }
    }

    public void BattleStart()
    {
        Debug.Log("전투 개시!");
        EnterTurnStart();
    }

    private void RefreshSynergies()
    {
        playerSynergy.UpdateSynergy(playerSlot, true);
        enemySynergy.UpdateSynergy(enemySlot, false);
    }

    private void SpawnEnemiesFromData()
    {
        //데이터 매니저에 저장한 내용 가지고 오기
        string stageID = DataManager.Instance.selectedStageID;
        StageDetailData detail = DataManager.Instance.GetStageDetail(stageID);

        if (detail == null) return;

        foreach (var enemyInfo in detail.enemies)
        {
            // 1. 유닛 데이터 로드
            UnitData data = DataManager.Instance.GetEnemyData(enemyInfo.unitID);
            GameObject commonMonsterPrefab = Resources.Load<GameObject>("Prefabs/Units/Monster");
            GameObject commonCharacterPrefab = Resources.Load<GameObject>("Prefabs/Units/Character");

            GameObject prefabToUse = (data is CharacterData) ? commonCharacterPrefab : commonMonsterPrefab;

            if (prefabToUse != null && enemyInfo.slotIndex < enemySlotTransforms.Length)
            {
                Transform targetSlot = enemySlotTransforms[enemyInfo.slotIndex];

                Transform anchor = targetSlot.Find("Character_Anchor");

                GameObject instance = Instantiate(prefabToUse, anchor);
                instance.transform.localPosition = Vector3.zero; // 위치 초기화
                Unit unit = instance.GetComponent<Unit>();

                // [수정] 클래스 타입에 따른 초기화 분기
                if (unit is Character character && data is CharacterData charData)
                {
                    // 적군 캐릭터라면 레벨과 돌파 정보 설정 (enemyInfo에 해당 데이터가 있다면 넣어주세요)
                    character.SetCharacterData(charData, 10, 0);
                }
                else
                {
                    // 일반 몬스터 초기화
                    unit.data = data;
                    unit.InitUnitStat();
                }

                unit.SetSlotIndex(enemyInfo.slotIndex);
                enemySlot[enemyInfo.slotIndex] = unit;
                enemyTurnOrder.Add(unit);

                SlotController sc = targetSlot.GetComponent<SlotController>();
                if (sc != null)
                {
                    sc.RefreshColor(unit.data.defaultTag);
                }
            }
        }
    }

    private void SpawnPlayersFromData()
    {
        // 1. 데이터 매니저에서 현재 편성된 파티 정보를 가지고 오기
        var partyData = DataManager.Instance.GetCurrentParty();

        if (partyData == null) return;

        //아군 캐릭터는 캐릭터만 있음.
        GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/Units/Character");

        foreach (var member in partyData)
        {
            // 2. 유닛 데이터 및 프리팹 로드
            UnitData data = DataManager.Instance.GetPlayerData(member.unitID);

            // [중요] 유저의 실제 성장 데이터를 가져옴
            CharacterInfo userInfo = DataManager.Instance.GetUserUnitInfo(member.unitID);
            if (playerPrefab != null && member.slotIndex < playerSlotTransforms.Length)
            {
                Transform targetSlot = playerSlotTransforms[member.slotIndex];

                Transform anchor = targetSlot.Find("Character_Anchor");

                // 3. 실제 생성 및 배치
                GameObject instance = Instantiate(playerPrefab, anchor);
                instance.transform.localPosition = Vector3.zero;

                Character character = instance.GetComponent<Character>();

                if (character != null && data is CharacterData charData)
                {
                    character.SetCharacterData(charData, userInfo.currentLevel, userInfo.currentBreakthrough);

                    characterParties.Add(character);

                    character.SetSlotIndex(member.slotIndex);
                    playerSlot[member.slotIndex] = character;
                    playerTurnOrder.Add(character);

                    SlotController sc = targetSlot.GetComponent<SlotController>();
                    if (sc != null)
                    {
                        sc.RefreshColor(character.data.defaultTag);
                    }
                }
            }
        }
    }

    private void InitBattleUnits()
    {
        playerSlot.Clear();
        enemySlot.Clear();
        playerTurnOrder.Clear();
        enemyTurnOrder.Clear();
        characterParties.Clear();

        SpawnPlayersFromData();

        SpawnEnemiesFromData();

        // 스피드 빠른 순서로 재정렬
        SortTurnOrder();

        RefreshSynergies();

        ForceUpdateAllSlotColors();
    }

    private void ForceUpdateAllSlotColors()
    {
        // 플레이어 슬롯 갱신
        for (int i = 0; i < playerSlotTransforms.Length; i++)
        {
            playerSlot.TryGetValue(i, out Unit unit);
            UpdateSlotColor(playerSlotTransforms[i], unit);
        }

        // 적 슬롯 갱신
        for (int i = 0; i < enemySlotTransforms.Length; i++)
        {
            enemySlot.TryGetValue(i, out Unit unit);
            UpdateSlotColor(enemySlotTransforms[i], unit);
        }
    }

    public void UpdateSlotColor(Transform slotTransform, Unit unit = null)
    {
        if (slotTransform == null) return;

        // slotTransform은 Slot_0, Slot_1 같은 최상위 슬롯
        SlotController slotCtrl = slotTransform.GetComponent<SlotController>();

        if (slotCtrl != null)
        {
            // 유닛이 있으면 해당 태그 색상으로, 없으면 투명하게(null)
            slotCtrl.RefreshColor(unit?.data?.defaultTag);
        }
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
            if (attacker == null || attacker.GetCurrentHP() <= 0) continue;
            if (targets.Count <= 0) break;

            //타겟 찾기
            Unit target = GetTarget(attacker);
            if (target == null || target.GetCurrentHP() <= 0) continue;

            // 시너지 데이터 가지고 오기
            SynergyEffect eff = attacker.data.isEnemy ? enemySynergy.currentEffect : playerSynergy.currentEffect;

            Debug.Log($"{attacker.data.unitNameKey}이(가) {target.data.unitNameKey}을(를) 조준!");

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
                else if (mySlot < 6) //중열/후열
                {
                    //중열일 경우, 중열 먼저
                    return GetHighestAttackTarget(allySlots);
                }
                else
                {
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

        Debug.Log($"{slotIndex}번 슬롯에 {unit.data.unitNameKey} 님이 배치되었습니다!");
    }

    public void TestBattle()
    {
        if (playerSlot.Count > 0 && enemySlot.Count > 0)
        {
            // 첫 번째 아군이 첫 번째 적을 공격!
            Unit attacker = playerTurnOrder[0];
            Unit target = enemyTurnOrder[0];

            Debug.Log($"{attacker.data.unitNameKey}의 공격!");
            target.TakeDamage(attacker.GetCurrentAttack());
        }
    }

    // 턴 시작
    void EnterTurnStart()
    {
        turnCount++;

        uiManager.UpdateTurnUI(turnCount);

        if (!isPaused && !isAutoBattle)
        {
            Time.timeScale = 1f;
            uiManager.UpdateSpeedUI(1f);
        }

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
        OnPhaseChanged(BattlePhase.BattleEnd);

        if (victory)
        {
            Debug.Log(" 전투 승리! 보상을 획득합니다.");
            // DataManager에 클리어 알림 (보상 지급 및 다음 스테이지 해금이 여기서 처리됨)
            List<ItemInventoryData> earnedRewards = DataManager.Instance.CompleteStage(DataManager.Instance.selectedStageID);

            uiManager.ShowResult(victory, earnedRewards, characterParties);
        }
        else
        {
            Debug.Log(" 전투 패배... 강해져서 돌아오세요.");
            uiManager.ShowResult(victory);
        }


    }
    public void OnPhaseChanged(BattlePhase battlePhase)
    {
        // 상태 업데이트
        _currentPhase = battlePhase;

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
                _currentPhase = BattlePhase.BattleEnd;

                EndBattle(false);
            }
        }
    }

    public void RemoveUnit(Unit unit)
    {
        int slotIdx = unit.GetSlotIndex();

        // 죽으면 발판 색 초기화
        if (unit.transform.parent != null && unit.transform.parent.parent != null)
        {
            UpdateSlotColor(unit.transform.parent.parent, null);
        }

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

            Debug.Log($"{unit.data.unitNameKey} 적을 물리쳤습니다. 남은 적: {enemySlot.Count}명");

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

            Debug.Log($"{unit.data.unitNameKey} 아군이 퇴각했습니다... 남은 아군: {playerSlot.Count}명");

            if (playerSlot.Count <= 0)
            {
                EndBattle(false);
            }

        }

        RefreshSynergies();

        // 전장에서 오브젝트를 제거합니다.
        Destroy(unit.gameObject);
    }
    private IEnumerator ExecuteDealerAction(Unit attacker, Unit target, SynergyEffect eff)
    {

        Dictionary<int, Unit> targetSlots = attacker.data.isEnemy ? playerSlot : enemySlot;
        List<Unit> areaTargets = GetUnitsInArea(target, attacker.data.skillArea, targetSlots);

        // [Dot 태그인 경우] 시너지에 따라 연격 횟수 결정
        int attackCount = 1;
        if (attacker.data.defaultTag.Equals("Dot"))
        {
            attackCount += eff.dotExtraHits;
        }

        for (int i = 0; i < attackCount; i++)
        {

            if (target == null || target.GetCurrentHP() <= 0) yield break;


            // 애니메이션 및 타격
            attacker.GetComponentInChildren<UnitAnimationController>().SetState(UnitAnimState.Attack);
            yield return new WaitForSeconds(0.4f);

            // --- 합산을 위한 저장소 ---
            HashSet<int> processedIndices = new HashSet<int>();


            foreach (Unit areaUnit in areaTargets)
            {
                if (areaUnit == null || areaUnit.GetCurrentHP() <= 0) continue; // 그 사이 죽었을 수도 있으니 체크

                float finalDamage = attacker.GetCurrentAttack();

                // [Direct 태그인 경우] 피해 증폭
                if (attacker.data.defaultTag.Equals("Direct"))
                    finalDamage *= (1f + eff.directDamageMult);

                areaUnit.TakeDamage(Mathf.RoundToInt(finalDamage));
                processedIndices.Add(areaUnit.GetSlotIndex()); // 이미 맞은 놈은 기록
            }

            if (attacker.data.defaultTag.Equals("Splash") && eff.splashBonus > 0 && target != null)
            {
                float splashDamage = attacker.GetCurrentAttack() * eff.splashBonus;

                HashSet<int> allNeighbors = new HashSet<int>();

                foreach (Unit areaUnit in areaTargets)
                {
                    if (areaUnit == null) continue;

                    List<int> neighborsOfUnit = GetNeighborIndices(areaUnit.GetSlotIndex());
                    foreach (int nIdx in neighborsOfUnit)
                    {
                        allNeighbors.Add(nIdx);
                    }
                }

                // 수집된 모든 이웃 인덱스에 대해 데미지 적용
                foreach (int idx in allNeighbors)
                {
                    // [조건] 이미 메인 공격을 받은 놈은 제외 && 실제 유닛이 존재하는 슬롯이어야 함
                    if (!processedIndices.Contains(idx) && targetSlots.TryGetValue(idx, out Unit u))
                    {
                        u.TakeDamage(Mathf.RoundToInt(splashDamage));
                    }
                }
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
        }

        // 실제 존재하는 유닛만 필터링
        foreach (int idx in targetIndices)
        {
            if (targetSlots.TryGetValue(idx, out Unit u) && u != null)
                areaTargets.Add(u);
        }

        return areaTargets;
    }

    private List<int> GetNeighborIndices(int center)
    {
        List<int> neighborIndices = new List<int>();
        int centerRow = center / 3; // 현재 행 (0, 1, 2)

        // 1. 상 (+3) / 하 (-3) : 인덱스 범위(0~8) 내에 있으면 추가
        if (center + 3 <= 8) neighborIndices.Add(center + 3);
        if (center - 3 >= 0) neighborIndices.Add(center - 3);

        // 2. 좌 (-1) / 우 (+1) : 같은 행(Row)일 때만 추가
        if (center - 1 >= 0 && (center - 1) / 3 == centerRow)
            neighborIndices.Add(center - 1);

        if (center + 1 <= 8 && (center + 1) / 3 == centerRow)
            neighborIndices.Add(center + 1);

        return neighborIndices;
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
            ApplyBuffToLine(0, attacker.data.isEnemy, (u) =>
            {
                u.AddShield(1, shieldHP);
                Debug.Log($"{u.data.unitNameKey}: 전열 쉴드 부여 (내구도: {shieldHP})");
            });
        }
        else if (mySlot < 6) // [중열] 다중 편광막
        {
            // 10%의 적당한 내구도 / 횟수 3회 (범위기 및 일반 공격 방어)
            int shieldHP = Mathf.RoundToInt(baseShieldAmount * 0.3f);
            if (target != null)
            {
                target.AddShield(3, shieldHP);
                Debug.Log($"{target.data.unitNameKey}: 중열 쉴드 부여 (내구도: {shieldHP} / 3회)");
            }
        }
        else // [후열] 안개 장막
        {
            // 5%의 낮은 내구도 / 횟수 5회 (연타 및 짤딜 방어)
            int shieldHP = Mathf.RoundToInt(baseShieldAmount * 0.1f);
            Dictionary<int, Unit> allies = attacker.data.isEnemy ? enemySlot : playerSlot;
            foreach (var ally in allies.Values)
            {
                if (ally == null) continue;
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
