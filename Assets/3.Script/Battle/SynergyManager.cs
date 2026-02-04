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
    public void UpdateSynergy(Dictionary<int, Unit> playerSlots, bool isPlayer)
    {
        int dotCount = 0;
        int splashCount = 0;
        int directCount = 0;


        // 1. 태그 카운트 (기획안 기반)
        foreach (var unit in playerSlots.Values)
        {
            if (unit == null) continue;

                // 한 캐릭터가 가진 모든 태그(기본1 + 커스텀4)를 가져옴
                List<string> tags = unit.GetSynergyTags();

                foreach (string tag in tags)
                {
                    if (tag == "Dot") dotCount++;
                    else if (tag == "Splash") splashCount++;
                    else if (tag == "Direct") directCount++;
                }
        }

        // [수정 포인트] 로그를 카운트 완료 후에 출력!
        Debug.Log($"[Synergy] D:{directCount}, S:{splashCount}, Dot:{dotCount}");

        // 아군용 시너지 UI
        if (isPlayer)
        {
            SynergyUI.instance.UpdateUI(directCount, splashCount, dotCount);
        }
        else
        {
            // 적군용 UI
            EnemySynergyUI.instance.UpdateUI(directCount, splashCount, dotCount);
        }
        // 시너지 이펙트 수치 계산
        CalculateEffect(directCount, splashCount, dotCount);

    }

    private void CalculateEffect(int direct, int splash, int dot)
    {
        // 시너지 이펙트 계산 처리
        currentEffect = new SynergyEffect();

        // Dot: 공격 횟수 증가 (3/6/9)
        currentEffect.dotExtraHits = (dot >= 9) ? 4 : (dot >= 6) ? 2 : (dot >= 3) ? 1 : 0;

        // Splash: 확산 피해 (C안 조정 수치)
        if (splash >= 9) currentEffect.splashBonus = 0.6f;
        else if (splash >= 6) currentEffect.splashBonus = 0.35f;
        else if (splash >= 3) currentEffect.splashBonus = 0.15f;

        // Direct: 피해량 증가 (3/6/9)
        if (direct >= 9) currentEffect.directDamageMult = 1.0f;
        else if (direct >= 6) currentEffect.directDamageMult = 0.6f;
        else if (direct >= 3) currentEffect.directDamageMult = 0.25f;
    }


}
