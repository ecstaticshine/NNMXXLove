using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStat : MonoBehaviour
{
    public CharacterData data;
    public List<Buff> activeBuffs = new List<Buff> (); // 지금 캐릭터에 걸려져 있는 버프

    [Header("성장 데이터")]
    public int breakthroughCount = 0; // 0 ~ 7 유지
    public int level = 1;
    private int currentSkillLevel = 1;

    private float currentHp;
    private float currentAttack;
    private float currentSpeed;
    private float finalSkillMultiplier; // 최종 스킬 배율

    private void Start()
    {
        InitCharacterStat();
        Debug.Log($"{data.charName} 등장! 공격 범위는 {data.areaType} +{data.skillRange}칸입니다.");
    }

    public void InitCharacterStat()
    {
        //레어리티별 성장 가중치 결정
        float rarityWeight = GetRarityWeight();

        float growFactor = 1f + (0.05f * rarityWeight);
        float totalGrowth = Mathf.Pow(growFactor,level -1);

        currentHp = data.baseHp * totalGrowth;
        currentAttack = data.baseHp * totalGrowth;
        currentSpeed = data.baseSpeed * totalGrowth;

        float loveFactor = (data.rarity == Rarity.EL)
            ? 2.00f
            : Cal_Rarity() + (breakthroughCount * 0.05f);

        // 캐릭터 고유 스킬 배율(data.skillMultiplier)과 결합
        finalSkillMultiplier = data.skillMultiplier * loveFactor;
    }

    public void SetCharacterData(CharacterData newData, int targetLevel, int targetBT)
    {
        data = newData;
        level = targetLevel;
        breakthroughCount = targetBT;
        InitCharacterStat(); // 데이터 설정 후 즉시 스탯 갱신
    }

    private float Cal_Rarity()
    {
        return data.rarity switch
        {
            Rarity.L => 0.95f,
            Rarity.PL => 1.30f,
            Rarity.TL => 1.65f,
            Rarity.EL => 2.00f,
            _ => 0.5f
        };
    }

    private float GetRarityWeight()
    {
        return data.rarity switch
        {
            Rarity.L => 1.0f,
            Rarity.PL => 1.2f,
            Rarity.TL => 1.5f,
            Rarity.EL => 2.0f,
            _ => 1.0f
        };
    }
}
