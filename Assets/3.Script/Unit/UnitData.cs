using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType { Dealer, Healer, Buffer }

[CreateAssetMenu(fileName = "NewUnitData", menuName = "ScriptableObjects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("고유 ID")]
    public int unitID;

    [Header("Localization Keys")]
    public string unitNameKey;
    public string descriptionKey;

    [Header("기본 정보")]
    public UnitType unitType;           // 유닛 타입 : 힐러 / 딜러 / 버퍼

    [Header("태그 정보")]
    public string defaultTag; // Dot, Splash, Direct 중 하나

    public Sprite unitPortrait;         // 캐릭터 얼굴 이미지
    public Sprite unitBattleSD;         // 전투 필드 배치용
    public Sprite unitAttackSD;         // 전투 필드 공격용
    public Sprite unitTakeDamageSD;     // 전투 필드 피격용
    public Sprite unitFullIllust;       // 강화 상세 정보창용
    public Rarity rarity;               // 캐릭터 등급
    public bool isEnemy;

    [Header("기본 능력치")]
    public int baseHp;
    public int baseAttack;
    public int baseSpeed;

    [Header("전투 매개변수")]
    public SkillArea skillArea;
    public float skillMultiplier = 1.0f;
}

