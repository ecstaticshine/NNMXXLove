using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    //턴제 배틀 시작. //1턴 아군 -> 적군
    public enum BattlePhase { None, PlayerPhase, EnemyPhase, BattleEnd }
    private BattlePhase currentPhase = BattlePhase.None;

    public int turnCount = 1;

    public List<BattleController> playerUnits;
    public List<BattleController> enemyUnits;

    //public GameObject 

    private void Start()
    {
        EnterTurnStart();
    }

    // 턴 시작
    void EnterTurnStart()
    {
        currentPhase = BattlePhase.PlayerPhase;
        // 아군 턴부터 시작
    }
    IEnumerator Show

}
