using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitIcon : MonoBehaviour
{
    [Header("UI Reference")]
    public Image portraitImage;     // 캐릭터 얼굴
    public Image frameImage;        // 등급별 테두리

    
    public Sprite[] rarityFrames;   // 레어리티 프레임
    
    public void SetUnitData(Unit unit)
    {
        if (unit == null || unit.data.unitPortrait == null) return;


        //유닛의 초상화 넣기
        portraitImage.sprite = unit.data.unitPortrait;

        //유닛의 레어리티에 맞는 프레임 넣기
        int rarityIndex = (int)unit.data.rarity;
        if (rarityIndex < rarityFrames.Length)
        {
            frameImage.sprite = rarityFrames[rarityIndex];
        }

    }

}
