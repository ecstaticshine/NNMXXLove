using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItemSlot : MonoBehaviour
{
    [Header("UI Elements")]
    public ItemIcon itemIcon;          // 왼쪽 아이콘 프리팹
    public TMP_Text itemNameText;      // "일반 강화석"
    public TMP_Text expValueText;      // "EXP + 500"
    public TMP_InputField countInput;  // 숫자가 표시되는 입력창
    public Button plusButton;
    public Button minusButton;
    public Button useButton;

    [Header("Data")]
    private int currentCount = 1;      // 현재 선택된 수량
    private int maxPossession = 0;     // 유저가 보유한 최대 개수
    private int unitID;                // 이그니 같은 대상 캐릭터 ID
    private int itemID;                // 아이템 ID

    private CharacterUpgradePanel parentPanel;
    public void Setup(ItemInventoryData invData, CharacterInfo targetChar, CharacterUpgradePanel parent)
    {
        // 전달받은 값들 저장
        this.unitID = targetChar.unitID;    // 캐릭터 정보에서 ID 추출
        this.itemID = invData.itemID;
        this.maxPossession = invData.count;
        this.parentPanel = parent;         // 나중에 리스트 새로고침용

        // 1. 초기 데이터 세팅 (DataManager 활용)
        ItemData data = DataManager.Instance.GetItemData(itemID);
        if (data == null) return;

        itemIcon.Setup(data, maxPossession);
        itemNameText.text = DataManager.Instance.GetLocalizedText(data.itemNameKey);

        string statName = data.effectStatType;
        int statValue = Mathf.FloorToInt(data.effectValue);

        expValueText.text = $"{statName} + {statValue}";

        // 2. 초기 수량 설정
        currentCount = 1;
        UpdateCountUI();

        // 3. 버튼 리스너 (중복 방지를 위해 기존 리스너 제거 후 등록)
        plusButton.onClick.RemoveAllListeners();
        plusButton.onClick.AddListener(() => ChangeCount(1));

        minusButton.onClick.RemoveAllListeners();
        minusButton.onClick.AddListener(() => ChangeCount(-1));

        useButton.onClick.RemoveAllListeners();
        useButton.onClick.AddListener(OnUseClick);

        countInput.onEndEdit.RemoveAllListeners();
        countInput.onEndEdit.AddListener(OnInputEdit);
    }

    void ChangeCount(int amount)
    {
        currentCount = Mathf.Clamp(currentCount + amount, 1, maxPossession);
        UpdateCountUI();
    }

    void OnInputEdit(string value)
    {
        if (int.TryParse(value, out int result))
        {
            currentCount = Mathf.Clamp(result, 1, maxPossession);
        }
        UpdateCountUI();
    }

    void UpdateCountUI()
    {
        countInput.text = currentCount.ToString();
        // 개수가 0개면 사용 버튼 비활성화 등의 처리
        useButton.interactable = maxPossession > 0;
    }

    void OnUseClick()
    {
        // 실제 데이터 반영 (경험치 지급 및 아이템 차감)
        DataManager.Instance.UseExpItem(unitID, itemID, currentCount);

        // 사용 후 보유량 갱신
        maxPossession -= currentCount;

        // 데이터 변경된 걸 알려주기
        DataManager.OnUserDataChanged?.Invoke();

        // 3. 수량 초기화 로직 추가
        if (maxPossession <= 0)
        {
            // 아이템이 아예 없으면 0으로 세팅 (또는 리스트 새로고침)
            currentCount = 0;
            parentPanel.RefreshList(); // 아이템이 없으면 리스트에서 제거하는 게 깔끔합니다.
        }
        else
        {
            // 아이템이 남아있으면 수량을 다시 1로 초기화!
            currentCount = 1;
        }

        UpdateCountUI();
        itemIcon.Setup(DataManager.Instance.GetItemData(itemID), maxPossession);
    }
}
