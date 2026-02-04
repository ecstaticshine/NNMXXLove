using System.Collections.Generic;
using UnityEngine;

public enum Rarity { L, PL, TL, EL }// Love, PureLove, TrueLove, EternalLove

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "ScriptableObjects/CharacterData")]
public class CharacterData : UnitData
{
    [Header("아군 전용 성장 데이터")]
    public float hpGrowth;
    public float attackGrowth;

    [Header("태그 시스템")]
    public List<string> customTags; // 캐릭터 커스텀 태그(유저가 결정한 태그)
}
