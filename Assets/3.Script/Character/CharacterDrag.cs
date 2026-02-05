using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterDrag : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Transform _originalSlot; // 원래 슬롯
    private CanvasGroup _canvasGroup; // 캔버스 그룹

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 현재 페이즈가 플레이어 선택 페이즈가 아니면 드래그 원천 차단!
        if (BattleManager.instance.currentPhase != BattlePhase.PlayerSelectPhase)
        {
            Debug.Log("지금은 유닛을 옮길 수 없는 시간입니다!");
            return;
        }
        
        _originalSlot = transform.parent;    // 현재 부모를 기억

        BattleManager.instance.UpdateSlotColor(_originalSlot.parent, null);

        transform.SetParent(transform.root); // 일단 가려지지 않게 최상위 부모로 옮기기
        _canvasGroup.blocksRaycasts = false; // 마우스가 캐릭터를 통과해 슬롯을 감지
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (BattleManager.instance.currentPhase != BattlePhase.PlayerSelectPhase) return;

            transform.position = eventData.position; // 마우스에 캐릭터 끌려가기
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (BattleManager.instance.currentPhase != BattlePhase.PlayerSelectPhase) return;

        _canvasGroup.blocksRaycasts = true; // 다시 마우스를 감지할 수 있게 해요

        // 만약 슬롯 위에 놓인 게 아니라면 원래 자리로 돌아가요
        if (transform.parent == transform.root)
        {
            ReturnToOriginalSlot();
        }

        Unit myUnit = GetComponent<Unit>();
        BattleManager.instance.UpdateSlotColor(transform.parent.parent, myUnit);
    }

     public void ReturnToOriginalSlot()
    {
        transform.SetParent(_originalSlot);
        transform.localPosition = Vector3.zero;

        // 돌아왔을 때도 색깔을 다시 칠해줌
        Unit myUnit = GetComponent<Unit>();
        BattleManager.instance.UpdateSlotColor(_originalSlot.parent, myUnit);
    }

    public Transform GetOriginalSlot()
    {
        return _originalSlot;
    }

}
