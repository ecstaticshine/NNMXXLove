using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "ScriptableObjects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName;
    public Sprite unitPortrait;
    public bool isEnemy;

    [Header("기본 능력치")]
    public int baseHp;
    public int baseAttack;
    public int baseSpeed;

    [Header("전투 매개변수")]
    public SkillArea areaType;
    public int skillRange;     // +1, +2 등
    public float skillMultiplier = 1.0f;
}
