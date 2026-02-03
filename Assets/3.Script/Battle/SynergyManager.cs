using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SynergyEffect
{
    public int dotExtraHits;      // Dot: 추가 공격 횟수 (+1, +2, +4)
    public float splashBonus;     // Splash: 확산 피해 비율 (15%, 35%, 60%)
    public float directDamageMult; // Direct: 즉각 피해 증가 (25%, 60%, 100%)
}

public class SynergyManager
{
    public SynergyEffect currentEffect;
    public void UpdateSynergy(Dictionary<int, Unit> playerSlots)
    {
        int dotCount = 0;
        int splashCount = 0;
        int directCount = 0;

        // 1. 태그 카운트 (재우 님의 기획안 기반)
        foreach (var slot in playerSlots.Values)
        {
            if (slot == null) continue;

            // 1. 기본 태그 (힐러, 버퍼, 딜러 공통)
            string baseTag = slot.data.defaultTag;
            AddTagCount(baseTag, ref dotCount, ref splashCount, ref directCount);

            if (slot is Character character)
            {
                // 한 캐릭터가 가진 모든 태그(기본1 + 커스텀4)를 가져옴
                List<string> tags = character.GetAllTags();

                foreach (string tag in tags)
                {
                    if (tag == "Dot") dotCount++;
                    else if (tag == "Splash") splashCount++;
                    else if (tag == "Direct") directCount++;
                }
            }
            else
            {
                if(slot.data != null)
                {
                    string tag = slot.data.defaultTag;
                    if (tag == "Dot") dotCount++;
                    else if (tag == "Splash") splashCount++;
                    else if (tag == "Direct") directCount++;
                }
            }
            
        }

        // 2. 조정된 C안 수치 적용
        currentEffect = new SynergyEffect();

        // Dot: 공격 횟수 증가 (3/6/9)
        currentEffect.dotExtraHits = (dotCount >= 9) ? 4 : (dotCount >= 6) ? 2 : (dotCount >= 3) ? 1 : 0;

        // Splash: 확산 피해 (C안 조정 수치)
        if (splashCount >= 9) currentEffect.splashBonus = 0.6f;
        else if (splashCount >= 6) currentEffect.splashBonus = 0.35f;
        else if (splashCount >= 3) currentEffect.splashBonus = 0.15f;

        // Direct: 피해량 증가 (3/6/9)
        if (directCount >= 9) currentEffect.directDamageMult = 1.0f;
        else if (directCount >= 6) currentEffect.directDamageMult = 0.6f;
        else if (directCount >= 3) currentEffect.directDamageMult = 0.25f;
    }

    private void AddTagCount(string tag, ref int dot, ref int splash, ref int direct)
    {
        if (tag == "Dot") dot++;
        else if (tag == "Splash") splash++;
        else if (tag == "Direct") direct++;
    }
}
