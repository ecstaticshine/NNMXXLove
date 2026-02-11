using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum UnitAnimState { Idle, Hit, Attack}

public class UnitAnimationController : MonoBehaviour
{
    private Image _image;
    private UnitData _data;

    private Vector3 _initialLocalPos;
    private Tween _idleTween;

    private void Start()
    {
        Init();
        SetState(UnitAnimState.Idle);
    }

    public void Init()
    {
        _image = GetComponent<Image>();
        _initialLocalPos = transform.localPosition;
        Unit unit = GetComponentInParent<Unit>();
        if (unit != null) _data = unit.data;

    }

    public void SetState(UnitAnimState newState)
    {
        if (this == null) return;

        StopCurrentAnim();

        switch (newState)
        {
            case UnitAnimState.Idle:
                PlayIdle();
                break;
            case UnitAnimState.Hit:
                PlayHit();
                break;
            case UnitAnimState.Attack:
                PlayAttack();
                break;
        }
    }

    private void StopCurrentAnim()
    {
        _idleTween?.Kill();
        transform.DOKill();
        transform.localPosition = _initialLocalPos;

        if (_data != null)
        {
            float targetScaleX = _data.isEnemy ? 1f : -1f;
            transform.localScale = new Vector3(targetScaleX, 1f, 1f);
        }
    }

    private void PlayIdle()
    {
        if (_data.unitBattleSD) _image.sprite = _data.unitBattleSD;
 
        _idleTween = transform.DOLocalMoveY(_initialLocalPos.y + 15f, 1.2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    public void PlayHit()
    {
        if (_data.unitTakeDamageSD) _image.sprite = _data.unitTakeDamageSD;

        transform.DOShakePosition(0.4f, strength: 20f);
        _image.DOColor(Color.red, 0.1f)
            .SetLink(gameObject)
            .OnComplete(() => _image.DOColor(Color.white, 0.2f));

        DOVirtual.DelayedCall(0.5f, () => SetState(UnitAnimState.Idle))
            .SetLink(gameObject);
    }

    private void PlayAttack()
    {
        // 공격 시 Sprite 교체
        if (_data.unitAttackSD) _image.sprite = _data.unitAttackSD;

        float dir = _data.isEnemy ? -1f : 1f;

        // 이동 거리 1.2f -> 150f / 점프 높이 0.4f -> 50f 정도로 수정!
        transform.DOLocalJump(_initialLocalPos + new Vector3(dir * 150f, 0, 0), 50f, 1, 0.4f)
            .SetEase(Ease.InBack)
            .SetLink(gameObject)
            .OnComplete(() => {
                transform.DOLocalMove(_initialLocalPos, 0.2f).SetLink(gameObject);
                DOVirtual.DelayedCall(0.2f, () => SetState(UnitAnimState.Idle))
                .SetLink(gameObject);
            });
    }

    private void OnDestroy()
    {
        _idleTween?.Kill();
        transform.DOKill();
    }
}
