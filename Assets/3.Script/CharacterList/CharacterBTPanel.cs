using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBTPanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform content;     // 아이템 항목들이 생성될 부모 (ScrollRect의 Content)
    public GameObject itemIcon;   // 아이템 하나하나의 프리팹
    public CharacterBTPanel tagPanel;

    private CharacterInfo currentSelectedInfo;

    // 패널이 켜질 때 호출될 함수
    public void Init(CharacterInfo character)
    {
        currentSelectedInfo = character;
        RefreshList();
    }

    public void RefreshList()
    {
        // 1. 기존 리스트 삭제 (풀링을 쓰면 더 좋지만 일단 삭제로!)
        //foreach (Transform child in content) Destroy(child.gameObject);

    }
}
