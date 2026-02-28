using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Currency,      // 젬(Gem), 골드(Gold) - 수치로 관리되는 주요 재화
    Material,      // 레벨업 재료, 강화 재료(돌파용) - 성장에 소모되는 일반 아이템
    CustomTag,     // 캐릭터에 장착하는 특수 태그 (파기 가능/해제 불가 속성)
    Consumable,    // 스테미나 회복제 등 사용 시 즉시 효과를 주는 소모품
    Character,      // 캐릭터 돌파 조각
    Skin,          // 가챠나 상점에서 획득하는 캐릭터 외형 (사라지지 않는 영구템)
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Localization Keys")]
    public string itemNameKey;
    public string descriptionKey;  //아이템 설명

    [Header("Basic Info")]
    public int itemID;
    [TextArea(3, 5)]

    [Header("Visual")]
    public Sprite itemIcon;
    public int grade = 1;       // 1~5성 (UI 테두리 등에 사용)

    [Header("Settings")]
    public ItemType itemType;
    public int maxStack = 999;

    [Header("Item Value")]
    // 아이템의 효과 수치를 저장
    // 예: 골드면 금액, 스테미나 약이면 회복량, 태그면 능력치 증가량 등
    public float effectValue;

    // 재료나 태그가 어떤 스탯을 건드리는지 구분 (예: "EXP", "ATK", "HP", "STAMINA")
    public string effectStatType;

    [Header("Custom Tag Details")]
    // 태그 아이템일 때만 사용하는 정보 (태그 설명 등)
    public string tagAbilityName;
}
