using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterUpgradePanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform content;     // 아이템 항목들이 생성될 부모 (ScrollRect의 Content)
    public GameObject itemIcon;   // 아이템 하나하나의 프리팹

    private CharacterSaveData currentCharacter;

    // 패널이 켜질 때 호출될 함수
    public void Init(CharacterSaveData character)
    {
        currentCharacter = character;
        RefreshList();
    }

    public void RefreshList()
    {
        // 1. 기존 리스트 삭제 (풀링을 쓰면 더 좋지만 일단 삭제로!)
        foreach (Transform child in content) Destroy(child.gameObject);

        // 2. DataManager의 인벤토리에서 경험치 아이템만 필터링해서 표시
        // 예: 아이템 ID 2000~2999 사이가 경험치 아이템이라고 가정
        foreach (var item in DataManager.Instance.userData.inventory)
        {
            if (item.itemID >= 2000 && item.itemID < 3000)
            {
                GameObject itemObject = Instantiate(itemIcon, content);
                // 슬롯 스크립트에 데이터 전달
                itemObject.GetComponent<UpgradeItemSlot>().Setup(item, currentCharacter, this);
            }
        }
    }
}
