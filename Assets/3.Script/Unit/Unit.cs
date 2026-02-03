using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public UnitData data;
    public List<Buff> activeBuffs = new List<Buff>();

    [Header("UI Reference")]
    [SerializeField] protected Slider hpBar;

    [SerializeField] protected int currentHp;
    [SerializeField] protected int currentAttack;
    [SerializeField] protected int currentSpeed;
    [SerializeField] protected int finalSkillMultiplier; // 최종 스킬 배율

    [SerializeField] protected int maxHp;   // 현재 유닛의 실제 최대 체력을 저장할 변수



    [SerializeField] private int slotIndex;     // 유닛이 위치한 슬롯

    [Header("Shield System")]
    public int shieldCount = 0; // 쉴드 횟수
    public int shieldAmount = 0; // 방어 가능한 데미지 상한선 (내구도)

    protected virtual void Awake()
    {
        InitUnitStat();
    }

    protected virtual void Start()
    {

        Debug.Log($"{data.unitName} 등장! 공격 범위는 {data.skillArea} +{data.skillRange}칸입니다.");
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

    public int GetCurrentHP()
    {
        return currentHp;
    }

    public int GetMaxHP()
    {
        return maxHp;
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
    public void Heal(int healAmount)
    {
        // 이미 죽은 유닛은 치료하지 않음
        if (currentHp <= 0) return;

        currentHp += healAmount;

        if (currentHp >= maxHp)
        {
            currentHp = maxHp;
        }
        UpdateHPUI();

        // 힐 받는 연출
        transform.DOScale(1.1f, 0.2f).SetLoops(2, LoopType.Yoyo);
    }

    public void TakeDamage(int damage)
    {
        if (shieldCount > 0)
        {
            if (shieldAmount >= damage)
            {
                shieldCount--;
                Debug.Log("쉴드가 공격을 완전히 흡수했습니다.");
                return; // 데미지 0
            }
            else
            {
                int pierceDamage = damage - shieldAmount;
                shieldCount--;
                currentHp -= pierceDamage;
                Debug.Log($"{data.unitName}이(가) {damage}의 피해를 입었습니다. 남은 체력: {currentHp}");
                DamageUpdateUI();
                return;
            }

        }
        currentHp -= damage;
        DamageUpdateUI();

    }

    private void DamageUpdateUI()
    {
        UpdateHPUI();

        if (currentHp <= 0)
        {
            currentHp = 0;
            BattleManager.instance.RemoveUnit(this);
        }
    }

    public void AddShield(int count, int amount)
    {
        this.shieldCount = count;
        this.shieldAmount = amount;
        Debug.Log($"{data.unitName}에게 {count}회(내구도 {amount}) 쉴드 생성!");
    }
}