using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterUpgradePanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform content;     // 아이템 항목들이 생성될 부모 (ScrollRect의 Content)
    public GameObject itemIcon;   // 아이템 하나하나의 프리팹

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
        foreach (Transform child in content) Destroy(child.gameObject);

        List<ItemInventoryData> expItems = DataManager.Instance.GetOwnedExpItems();

        // 2. 경험치 아이템만 가지고 와서 보여주기
        foreach (var item in expItems)
        {
            GameObject itemObject = Instantiate(itemIcon, content);

            // 슬롯 스크립트에 데이터 전달 (이미 만들어두신 UpgradeItemSlot 활용)
            if (itemObject.TryGetComponent<UpgradeItemSlot>(out var slot))
            {
                slot.Setup(item, currentSelectedInfo, this);
            }
        }
    }
}
