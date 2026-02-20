using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotDrop : MonoBehaviour, IDropHandler
{
    public Transform characterAnchorSlot;
    public bool isPlayerSlot;

    [SerializeField] private GameObject unitPrefab;

    public void OnDrop(PointerEventData eventData)
    {
        if (!isPlayerSlot) return; // 아군 슬롯 아니면 리턴

        // 드래그 중인 오브젝트
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        // 리스트에서 드래그한 캐릭터 아이콘인가?
        UnitIcon listIcon = draggedObject.GetComponent<UnitIcon>();

        // 배틀 씬에서 드래그한 캐릭터 아이콘인가?
        CharacterDrag characterDrag = draggedObject.GetComponent<CharacterDrag>();

        if (listIcon != null)
        {
            HandleNewUnitDrop(listIcon);
        }
        else if(characterDrag != null)
        {
            HandleUnitSwap(draggedObject, characterDrag);
        }
        
        
        
    }

    private void HandleNewUnitDrop(UnitIcon listIcon)
    {
        // 슬롯에 이미 유닛이 있다면 제거 (또는 교체)
        if (characterAnchorSlot.childCount > 0)
        {
            Destroy(characterAnchorSlot.GetChild(0).gameObject);
        }

        GameObject newUnitObj = Instantiate(unitPrefab, characterAnchorSlot);
        newUnitObj.transform.localPosition = Vector2.zero;
        newUnitObj.transform.localScale = Vector3.one;

        Character newChar = newUnitObj.GetComponent<Character>();
        if (newChar != null)
        {
            // listIcon에서 UnitData를 가져와 주입
            newChar.SetCharacterData(listIcon.GetUnitData(),1,0);
        }

       
        UpdateOverallSynergy();

        if (BattleManager.instance != null)
        {
            BattleManager.instance.UpdateSlotColor(characterAnchorSlot.parent, newChar);
        }
    }

    private void HandleUnitSwap(GameObject draggedObject, CharacterDrag characterDrag)
    {
        Transform dragOriginalSlot = characterDrag.GetOriginalSlot(); // 원래 슬롯 (Character_Anchor)
        Unit draggedUnit = draggedObject.GetComponent<Unit>();


        // 무언가 들어 있다면
        if (characterAnchorSlot.childCount > 0)
        {
            Transform existCharacter = characterAnchorSlot.GetChild(0);
            Unit existUnit = existCharacter.GetComponent<Unit>();

            Transform dragCharacterSlot = draggedObject.GetComponent<CharacterDrag>().GetOriginalSlot();

            existCharacter.SetParent(dragCharacterSlot);
            existCharacter.transform.localPosition = Vector2.zero;
            existCharacter.transform.localScale = Vector3.one;

            BattleManager.instance.UpdateSlotColor(dragOriginalSlot.parent, existUnit);

        }
        else
        {
            BattleManager.instance.UpdateSlotColor(dragOriginalSlot.parent, null);
        }

        // 캐릭터의 부모를 이 슬롯의 앵커 바꾸기
        draggedObject.transform.SetParent(characterAnchorSlot);
        draggedObject.transform.localPosition = Vector2.zero;
        draggedObject.transform.localScale = Vector3.one;

        BattleManager.instance.UpdateSlotColor(characterAnchorSlot.parent, draggedUnit);

        UpdateOverallSynergy();
    }


    private void UpdateOverallSynergy()
    {
        int direct = 0, splash = 0, dot = 0;

        SlotDrop[] allSlots = transform.parent.GetComponentsInChildren<SlotDrop>();

        foreach (var slot in allSlots)
        {
            if (slot.characterAnchorSlot.childCount > 0)
            {
                Unit unit = slot.characterAnchorSlot.GetChild(0).GetComponent<Unit>();
                if (unit != null && unit.data != null) // UnitData 기반
                {
                    // 태그 문자열에 따라 카운트 증가
                    if (unit.data.defaultTag == "Direct") direct++;
                    else if (unit.data.defaultTag == "Splash") splash++;
                    else if (unit.data.defaultTag == "Dot") dot++;
                }
            }
        }

        // 계산된 결과를 SynergyUI에 전달
        if (SynergyUI.instance != null)
        {
            SynergyUI.instance.UpdateUI(direct, splash, dot);
        }
    }
}
