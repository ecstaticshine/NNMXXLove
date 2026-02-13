using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Unit : MonoBehaviour
{
    public UnitData data;
    public List<Buff> activeBuffs = new List<Buff>();

    [Header("UI Reference")]
    [SerializeField] protected Slider hpBar;
    [SerializeField] protected GameObject damageBubble;
    [SerializeField] protected TMP_Text damageText;

    [Header("Shield")]
    [SerializeField] protected GameObject shieldIcon;
    [SerializeField] protected TMP_Text shieldText;
    [SerializeField] protected GameObject attackIcon;
    [SerializeField] protected TMP_Text AtkText;

    [Header("Heal Effect")]
    [SerializeField] protected GameObject leafPrefab;
    [SerializeField] protected GameObject healBubble;
    [SerializeField] protected TMP_Text healText;

    [Header("Unit Stats")]
    [SerializeField] public int level;
    [SerializeField] protected int currentHp;
    [SerializeField] protected int currentAttack;
    [SerializeField] protected int currentSpeed;
    [SerializeField] protected int multiplier;
    [SerializeField] protected int finalSkillMultiplier; // 최종 스킬 배율

    [SerializeField] protected int maxHp;   // 현재 유닛의 실제 최대 체력을 저장할 변수

    [SerializeField] private int slotIndex;     // 유닛이 위치한 슬롯

    [Header("Shield System")]
    public int shieldCount = 0; // 쉴드 횟수
    public int shieldAmount = 0; // 방어 가능한 데미지 상한선 (내구도)

    [Header("Bonus Stats")]
    [SerializeField] private int bonusAttackFromShield = 0;

    private CanvasGroup canvasGroup;

    // 죽었을 때
    private bool isDead = false;

    protected virtual void Awake()
    {
        InitUnitStat();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    protected virtual void Start()
    {

        Debug.Log($"{data.unitNameKey} 등장! 공격 범위는 {data.skillArea}입니다.");
    }

    public virtual void InitUnitStat()
    {
        // 데이터가 없으면 리턴
        if (data == null) return;

        currentHp = data.baseHp;
        currentAttack = data.baseAttack;
        currentSpeed = data.baseSpeed;

        // 레벨에 따라서 몬스터도 성장하게 변경
        // 적 캐릭터가 아닌 이상, 레어도가 없기에 일단 아군 캐릭터가 더 강하게 적용
        float growthFactor = 1f + (level - 1) * 0.05f;
        currentHp = Mathf.RoundToInt(currentHp * growthFactor);
        currentAttack = Mathf.RoundToInt(currentAttack * growthFactor);

        // CSV에서 가져온 Multiplier 적용
        currentHp = Mathf.RoundToInt(currentHp * multiplier);

        Debug.Log($"{data.unitNameKey} 초기화 - 최종 HP: {currentHp}, Multiplier: {multiplier}");

        maxHp = currentHp;

        if (hpBar != null)
        {
            hpBar.maxValue = currentHp;
            hpBar.value = currentHp;
        }

        Debug.Log($"[Monster] {data.unitNameKey} 세팅 완료! HP: {currentHp}");

    }

    public int GetCurrentAttack()
    {
        return currentAttack + bonusAttackFromShield;
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
            hpBar.DOValue(currentHp, 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLink(hpBar.gameObject);
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
        showHealPopup(healAmount);
        UpdateHPUI();

        // 힐 받는 연출
        transform.DOScale(1.05f, 0.15f).SetLoops(2, LoopType.Yoyo).SetLink(gameObject);
    }

    public void TakeDamage(int damage)
    {
        int directDamage = 0;  // 쉴드 계산 후 Damage

        if (shieldCount > 0)
        {
            if (shieldAmount >= damage)
            {
                shieldCount--;
                UpdateShieldUI();
                //TODO : Blocked 추가
                Debug.Log("쉴드가 공격을 완전히 흡수했습니다.");
                return; // 데미지 0
            }
            else
            {
                directDamage = damage - shieldAmount;
                shieldCount--;
                UpdateShieldUI();
            }

        }
        else
        {
            directDamage = damage;
        }

        if (directDamage > 0)
        {
            currentHp -= directDamage;
            ShowDamagePopup(directDamage);
            GetComponentInChildren<UnitAnimationController>()?.PlayHit();
            DamageUpdateUI();
        }
    }

    private void DamageUpdateUI()
    {
        UpdateHPUI();

        if (currentHp <= 0 && !isDead)
        {
            isDead = true;
            currentHp = 0;
            BattleManager.instance.RemoveUnit(this);

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0, 0.5f).OnComplete(() => {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    protected virtual void showHealPopup(int amount)
    {
        if (healBubble == null || healText == null || leafPrefab == null) return;

        // 1. 초기화 및 데이터 세팅
        healText.text = $"+{amount}";
        healText.alpha = 1f;
        healBubble.SetActive(true);
        leafPrefab.SetActive(true);

        // CanvasGroup 컴포넌트 가져오기 (없으면 추가 권장)
        CanvasGroup leafCG = leafPrefab.GetComponent<CanvasGroup>();
        CanvasGroup bubbleCG = healBubble.GetComponent<CanvasGroup>();
        if (leafCG != null) leafCG.alpha = 1f;
        if (bubbleCG != null) bubbleCG.alpha = 0f; // 텍스트는 처음엔 투명하게

        // 위치 초기화
        leafPrefab.transform.localPosition = new Vector3(0, 200, 0); // 머리 위 높은 곳
        healBubble.transform.localPosition = new Vector3(0, 100, 0); // 텍스트 대기 위치
        leafPrefab.transform.localScale = Vector3.one * 0.5f;

        // 2. DOTween Sequence 시작
        Sequence healSeq = DOTween.Sequence().SetLink(gameObject); ;

        // A. 나뭇잎 연출: 좌우로 흔들리며(살랑살랑) 내려옴
        healSeq.Append(leafPrefab.transform.DOLocalMoveY(120, 1.0f).SetEase(Ease.OutQuad)) // 하강
               .Join(leafPrefab.transform.DOLocalMoveX(30, 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine)) // 좌우 흔들림
               .Join(leafPrefab.transform.DORotate(new Vector3(0, 0, 20), 0.5f).SetLoops(2, LoopType.Yoyo)) // 회전
               .Join(leafPrefab.transform.DOScale(1.0f, 0.5f));

        // B. 나뭇잎이 특정 높이에 도달했을 때 텍스트 연출 시작 (Insert 사용)
        healSeq.Insert(0.7f, healBubble.transform.DOLocalMoveY(160, 0.8f).SetRelative().SetEase(Ease.OutBack)) // 텍스트 상승
               .Insert(0.7f, bubbleCG.DOFade(1f, 0.3f)); // 텍스트 나타남

        // C. 마무리: 둘 다 서서히 사라짐
        healSeq.AppendInterval(0.2f)
               .Append(leafCG.DOFade(0, 0.5f))
               .Join(bubbleCG.DOFade(0, 0.5f))
               .OnComplete(() => {
                   leafPrefab.SetActive(false);
                   healBubble.SetActive(false);
               });
    }

    public void AddShield(int count, int amount)
    {
        if (shieldAmount > 0)
        {
            //버프가 있는 상태에서 버프를 또 걸면 공격력이 오르게 함.
            float bonusRate = 0.1f;
            int addedAtk = Mathf.RoundToInt(amount * bonusRate);

            bonusAttackFromShield += addedAtk; // 보너스 누적


            // [추가] 공격력 아이콘 및 텍스트 활성화/갱신
            if (attackIcon != null)
            {
                attackIcon.SetActive(true);
                // 연출: 아이콘이 뿅 하고 커졌다 작아지는 효과 (DOTween)
                attackIcon.transform.DOKill();
                attackIcon.transform.localScale = Vector3.one;
                attackIcon.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f);
            }

            if (AtkText != null)
            {
                AtkText.text = $"+{bonusAttackFromShield}"; // 누적된 보너스 표시
            }

        }

        if (amount > this.shieldAmount || this.shieldCount <= 0)
        {
            this.shieldCount = count;
            this.shieldAmount = amount;
        }

        if (shieldIcon != null) shieldIcon.SetActive(true);
        if (shieldText != null)
        {
            shieldText.gameObject.SetActive(true);
            shieldText.text = this.shieldAmount.ToString();
        }

        UpdateShieldUI();

        Debug.Log($"{data.unitNameKey}에게 {count}회(내구도 {amount}) 쉴드 생성!");


    }

    private void UpdateShieldUI()
    {
        if (shieldCount <= 0)
        {
            shieldIcon.SetActive(false);
            shieldText.gameObject.SetActive(false);

            // [추가] 쉴드가 깨지면 공격력 UI도 함께 숨깁니다.
            if (attackIcon != null) attackIcon.SetActive(false);

            // 쉴드가 깨지면 공격력 보너스도 증발
            if (bonusAttackFromShield > 0)
            {
                Debug.Log($"{data.unitNameKey}의 쉴드가 사라져 추가 공격력이 환원되었습니다.");
                bonusAttackFromShield = 0;
            }
            return;

        }
        // shieldText나 개수 텍스트 갱신 로직
        shieldIcon.GetComponentInChildren<TMP_Text>().text = shieldCount.ToString();
    }

    protected virtual void ShowDamagePopup(int amount)
    {
        if ( damageBubble == null|| damageText == null) return;

        damageBubble.transform.DOKill(); // 이전 트윈이 실행 중이면 종료
        damageBubble.transform.localScale = Vector3.zero;
        damageBubble.transform.localPosition = new Vector3(0, 100, 0);

        damageText.text = amount.ToString();
        damageBubble.gameObject.SetActive(true); // 활성화

        //연출
        damageBubble.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetLink(damageBubble);
        damageBubble.transform.DOLocalMoveY(100f, 0.6f).SetRelative().SetLink(damageBubble).OnComplete(() =>
        {
            // 사라질 때 살짝 작아지면서 사라지면 더 예뻐요
            damageBubble.transform.DOScale(0f, 0.2f).OnComplete(() => {
                damageBubble.SetActive(false);
            })
            .SetLink(damageBubble);
        });
    }

    public virtual List<string> GetSynergyTags()
    {
        List<string> tags = new List<string>();
        // 모든 유닛은 기본적으로 데이터에 정의된 defaultTag를 가짐
        if (data != null && !string.IsNullOrEmpty(data.defaultTag))
        {
            tags.Add(data.defaultTag);
        }
        return tags;
    }

    protected virtual void OnDestroy()
    {
        transform.DOKill();
    }

}