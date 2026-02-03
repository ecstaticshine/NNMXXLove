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

    [Header("커스텀 태그 (유저 설정)")]
    public string[] customTags = new string[4];

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
        maxHp = currentHp;  //최대 체력으로
        currentAttack = Mathf.RoundToInt(characterdata.baseAttack * totalAtkGrowth);
        currentSpeed = characterdata.baseSpeed;

        if (hpBar != null)
        {
            hpBar.maxValue = maxHp;
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

    // 태그 다 가지고 오기
    public List<string> GetAllTags()
    {
        List<string> tags = new List<string>();

        // 1. UnitData에 기본 태그가 있다면 추가 (예: characterdata.defaultTag)
        if (data is CharacterData cd && !string.IsNullOrEmpty(cd.defaultTag))
            tags.Add(cd.defaultTag);

        // 2. 유저가 설정한 커스텀 태그들 추가
        foreach (var tag in customTags)
        {
            if (!string.IsNullOrEmpty(tag)) tags.Add(tag);
        }
        return tags;
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
