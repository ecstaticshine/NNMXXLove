using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "ScriptableObjects/UnitData")]
public class UnitDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName;
    public Sprite unitPortrait;
    public bool  isEnemy;    // 적인지 아군인지 확인

    [Header("기본 능력치 (Lv.1 기준)")]
    public float baseHp;
    public float baseAttack;
    public float baseSpeed;

    [Header("고유 특징")]
    public string characterTag; // 캐릭터 태그 (스플, 한방, 도트) 절대 안 바뀜
    public List<string> customTags; // 캐릭터 커스텀 태그(유저가 결정한 태그)

}
