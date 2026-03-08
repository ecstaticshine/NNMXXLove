using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterDrag : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Transform _originalSlot; // 원래 슬롯
    private CanvasGroup _canvasGroup; // 캔버스 그룹

    public UnitIcon originIcon;

    private Vector2 _pointerDownPosition;
    private const float _dragThreshold = 10f; // 10픽셀이상 움직여야 드래그로 인정
    private bool _isDraggingActual = false; // 실제로 임계값을 넘었는지 여부

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalSlot = transform.parent;    // 현재 부모를 기억
        _pointerDownPosition = eventData.position;         // 드래그 시작 지점 저장
        _isDraggingActual = false; // 초기화

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();

        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = 310;


        SceneState currentState = GlobalUIManager.Instance.GetCurrentState();
        if (currentState != SceneState.Placement && currentState != SceneState.Stage)
        {
            return;
        }

        if (BattleManager.instance != null)
        {
            // 현재 페이즈가 플레이어 선택 페이즈가 아니면 드래그 원천 차단!
            if (BattleManager.instance.currentPhase != BattlePhase.PlayerSelectPhase)
            {
                Debug.Log("지금은 유닛을 옮길 수 없는 시간입니다!");
                return;
            }


            BattleManager.instance.UpdateSlotColor(_originalSlot.parent, null);
        }


    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그 중이 아닐 때만 판단
        if (!eventData.dragging)
        {
            // 1. 현재 씬 상태를 가져옵니다.
            SceneState current = GlobalUIManager.Instance.GetCurrentState();

            // 2. 만약 어드벤처(배치 중) 상태라면? -> 회수 실행
            if (current == SceneState.Placement)
            {
                Debug.Log("Yeah");
                RemoveUnitFromField();
            }
            // 3. 만약 배틀(실제 전투) 상태라면? -> 아무것도 안 함 (무시)
            else
            {
                Debug.Log("전투 중에는 유닛을 회수할 수 없습니다!");
            }
        }
    }

    public void RemoveUnitFromField()
    {
        // 1. 리스트 아이콘 다시 활성화
        if (originIcon != null)
        {
            originIcon.SetPlaced(false);
        }

        // 2. 부모 슬롯(SlotDrop)을 찾아 업데이트 호출
        SlotDrop parentSlot = GetComponentInParent<SlotDrop>();

        // 3. 유닛 파괴
        Destroy(gameObject);

        // 4. 슬롯 색상 및 시너지 갱신
        if (parentSlot != null)
        {
            BattleManager.instance?.UpdateSlotColor(parentSlot.characterAnchorSlot.parent, null);
            // SlotDrop의 UpdateOverallSynergy
            parentSlot.Invoke("UpdateOverallSynergy", 0.1f); // 파괴 후 계산되도록 살짝 지연
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (BattleManager.instance != null)
        {
            if (BattleManager.instance.currentPhase != BattlePhase.PlayerSelectPhase) return;
        }

        if (!_isDraggingActual)
        {
            float distance = Vector2.Distance(_pointerDownPosition, eventData.position);
            if (distance >= _dragThreshold)
            {
                // [드래그 확정 순간] 이때 부모를 바꾸고 레이캐스트를 끕니다.
                _isDraggingActual = true;
                transform.SetParent(transform.root);
                _canvasGroup.blocksRaycasts = false;

                if (BattleManager.instance != null)
                    BattleManager.instance?.UpdateSlotColor(_originalSlot.parent, null);
            }
            else return;
        }
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDraggingActual)
        {
            ResetSorting();
            return; // 드래그가 시작조차 안 했다면 로직 스킵
        }

        if (BattleManager.instance != null)
        {
            if (BattleManager.instance.currentPhase != BattlePhase.PlayerSelectPhase) return;
        }

        _canvasGroup.blocksRaycasts = true; // 다시 마우스를 감지할 수 있게 변경
        _isDraggingActual = false;

        // 슬롯 위에 놓인 게 아니라면 원래 자리로 복귀
        if (transform.parent == transform.root)
        {
            ReturnToOriginalSlot();
        }

        ResetSorting();

        if (BattleManager.instance != null && transform.parent != null && transform.parent.parent != null)
        {
            Unit myUnit = GetComponent<Unit>();
            BattleManager.instance.UpdateSlotColor(transform.parent.parent, myUnit);
        }
    }

    private void ResetSorting()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 301; // 평소 상태 (슬롯보다 아주 살짝 앞)
        }
    }

    public void ReturnToOriginalSlot()
    {
        transform.SetParent(_originalSlot);
        transform.localPosition = Vector3.zero;

        if (BattleManager.instance != null)
        {
            // 돌아왔을 때도 색깔을 다시 칠해줌
            Unit myUnit = GetComponent<Unit>();
            BattleManager.instance.UpdateSlotColor(_originalSlot.parent, myUnit);
        }
    }

    public Transform GetOriginalSlot()
    {
        return _originalSlot;
    }

}
