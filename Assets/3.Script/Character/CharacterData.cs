using System.Collections.Generic;
using UnityEngine;

public enum Rarity { L, PL, TL, EL }// Love, PureLove, TrueLove, EternalLove

[CreateAssetMenu(fileName = "NewUnitData", menuName = "ScriptableObjects/UnitData")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string charName;
    public Rarity rarity;
    public Sprite unitPortrait;
    public bool  isEnemy;    // 적인지 아군인지 확인

    [Header("능력치 및 성장률")]
    public float baseHp;
    public float hpGrowth;
    public float baseAttack;
    public float attackGrowth;
    public float baseSpeed;
    public float speedGrowth;

    [Header("스킬 및 범위")]
    public SkillArea areaType; // 아까 만든 Enum
    public int skillRange;     // +1, +2 등
    public float skillMultiplier = 1.0f; // 스킬 데미지 배율

    [Header("태그 시스템")]
    public string characterTag; // 캐릭터 태그 (스플, 한방, 도트) 절대 안 바뀜
    public List<string> customTags; // 캐릭터 커스텀 태그(유저가 결정한 태그)
}
