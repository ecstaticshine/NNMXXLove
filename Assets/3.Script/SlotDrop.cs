using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotDrop : MonoBehaviour, IDropHandler
{
    public Transform characterAnchorSlot;
    public bool isPlayerSlot;

    public void OnDrop(PointerEventData eventData)
    {
        
        // 드래그 중인 오브젝트
        GameObject draggedObject = eventData.pointerDrag;

        CharacterDrag characterDrag = draggedObject.GetComponent<CharacterDrag>();
        Transform dragOriginalSlot = characterDrag.GetOriginalSlot(); // 원래 슬롯 (Character_Anchor)
        Unit draggedUnit = draggedObject.GetComponent<Unit>();

        if (draggedObject == null || !isPlayerSlot) return;

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
    }

}
