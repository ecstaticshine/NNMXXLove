using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Character : Unit
{

    [Header("성장 데이터")]
    public int breakthroughCount = 0; // 0 ~ 7 유지
    public int level = 1;
    private int currentSkillLevel = 1;

    protected override void Awake()
    {
        base.Awake();
        if (data != null)
        {
        SetCharacterData(data, 80, 3);
        }
    }

    public override void InitUnitStat()
    {
        if (data is CharacterData characterdata)
        {
        //레어리티별 성장 가중치 결정
        float rarityWeight = GetRarityWeight(characterdata);

            Debug.Log(rarityWeight);

        float hpGainPerLevel = (0.05f + characterdata.hpGrowth) * rarityWeight;
        float atkGainPerLevel = (0.05f + characterdata.attackGrowth) * rarityWeight;

        float totalHpGrowth = 1f + (hpGainPerLevel * (level - 1));
        float totalAtkGrowth = 1f + (atkGainPerLevel * (level - 1));

        currentHp = Mathf.RoundToInt(characterdata.baseHp * totalHpGrowth);
        currentAttack = Mathf.RoundToInt(characterdata.baseAttack * totalAtkGrowth);
        currentSpeed = characterdata.baseSpeed;

        if (hpBar != null)
        {
            hpBar.maxValue = currentHp;
            hpBar.value = currentHp;
        }

        float loveFactor = (characterdata.rarity == Rarity.EL)
            ? 2.00f
            : Cal_Rarity(characterdata) + (breakthroughCount * 0.05f);

        // 캐릭터 고유 스킬 배율(data.skillMultiplier)과 결합
        finalSkillMultiplier = Mathf.RoundToInt(characterdata.skillMultiplier * currentAttack * loveFactor);
        }
    }

    public void SetCharacterData(UnitData newData, int targetLevel, int targetBT)
    {
        data = newData;
        level = targetLevel;
        breakthroughCount = targetBT;
        InitUnitStat(); // 데이터 설정 후 즉시 스탯 갱신
    }

    private float Cal_Rarity(CharacterData characterData)
    {
        return characterData.rarity switch
        {
            Rarity.L => 0.95f,
            Rarity.PL => 1.30f,
            Rarity.TL => 1.65f,
            Rarity.EL => 2.00f,
            _ => 0.5f
        };
    }

    private float GetRarityWeight(CharacterData characterData)
    {
        return characterData.rarity switch
        {
            Rarity.L => 1.0f,
            Rarity.PL => 1.2f,
            Rarity.TL => 1.5f,
            Rarity.EL => 2.0f,
            _ => 1.0f
        };
    }




}
