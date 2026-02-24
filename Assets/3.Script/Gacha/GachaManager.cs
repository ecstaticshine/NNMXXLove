using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    public float[] rates = { 3f, 27f, 70f }; // SSR, SR, R 확률
    public int pityLimit = 50; // 천장 횟수
    private int currentPity = 0;

    public string Pull()
    {
        currentPity++;

        // 1. 천장 체크
        if (currentPity >= pityLimit)
        {
            ResetPity();
            return "Pickup TL (Pity)";
        }

        // 2. 일반 확률 계산 로직 (Random.Range 이용)
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        if (randomValue <= rates[0]) // 3% 확률 당첨 시
        {
            ResetPity();
            return "TL Character";
        }else if (randomValue <= (rates[0] + rates[1]))
        {
            return "PL Character";
        }
        else
        {
            return "L Character";
        }
    }

    public void Pull10()
    {
        Debug.Log("10연차");
        List<string> finalResults = new List<string>();

        for (int i = 0; i < 10; i++)
        {
            string result = Pull();

            if (i == 9)
            {
                if(result == "L Character")
                {
                    result = "<color=green>[PL Character</color>";
                }
            }

            // 기존 1회 뽑기 로직을 10번 반복하여 리스트에 저장
            finalResults.Add(result);
        }

        // 결과 리포트 출력 (기획안의 박스 연출 부분)
        string report = "10연차 결과:\n";
        for (int i = 0; i < finalResults.Count; i++)
        {
            report += $"{i + 1}번: {finalResults[i]}\n";
        }
        Debug.Log(report);
    }

    void ResetPity() { currentPity = 0; }
}
