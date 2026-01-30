using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public UnitData data; 
    public List<Buff> activeBuffs = new List<Buff>();

    [Header("UI Reference")]
    protected Slider hpBar;

    [SerializeField] protected int currentHp;
    [SerializeField] protected int currentAttack;
    [SerializeField] protected int currentSpeed;
    [SerializeField] protected int finalSkillMultiplier; // 최종 스킬 배율

    [SerializeField] private int slotIndex;     // 유닛이 위치한 슬롯

    protected virtual void Awake()
    {
        if (transform.childCount > 1)
        {
            transform.GetChild(1).TryGetComponent<Slider>(out hpBar);
        }

        InitUnitStat();
    }

    protected virtual void Start()
    {

        Debug.Log($"{data.unitName} 등장! 공격 범위는 {data.areaType} +{data.skillRange}칸입니다.");
    }

    public virtual void InitUnitStat()
    {
        currentHp = data.baseHp;
        currentAttack = data.baseAttack;
        currentSpeed = data.baseSpeed;

        if (hpBar != null)
        {
            hpBar.maxValue = currentHp;
            hpBar.value = currentHp;
        }

        Debug.Log($"[Monster] {data.unitName} 세팅 완료! HP: {currentHp}");

    }

    public int GetCurrentAttack()
    {
        return currentAttack;
    }

    public void SetSlotIndex(int index)
    {
        this.slotIndex = index;
        Debug.Log($"[{gameObject.name}] 슬롯 인덱스가 {index}로 설정되었습니다.");
    }

    public int GetSlotIndex()
    {
        return this.slotIndex;
    }

    public int GetCurrentSpeed()
    {
        return this.currentSpeed;
    }
    public void UpdateHPUI()
    {
        if (hpBar != null)
        {
            hpBar.DOValue(currentHp, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log($"{data.unitName}이(가) {damage}의 피해를 입었습니다. 남은 체력: {currentHp}");

        UpdateHPUI();

        if (currentHp <= 0)
        {
            currentHp = 0;
            BattleManager.instance.RemoveUnit(this);
        }
    }
}