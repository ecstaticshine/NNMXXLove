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

        if (draggedObject == null || !isPlayerSlot) return;

        // 무언가 들어 있다면
        if (characterAnchorSlot.childCount > 0)
        {
            Transform existCharacter = characterAnchorSlot.GetChild(0);

            Transform dragCharacterSlot = draggedObject.GetComponent<CharacterDrag>().GetOriginalSlot();

            existCharacter.SetParent(dragCharacterSlot);
            existCharacter.transform.localPosition = Vector2.zero;
            existCharacter.transform.localScale = Vector3.one;

        }

        // 캐릭터의 부모를 이 슬롯의 앵커 바꾸기
        draggedObject.transform.SetParent(characterAnchorSlot);

        draggedObject.transform.localPosition = Vector2.zero;

        draggedObject.transform.localScale = Vector3.one;


    }

}
