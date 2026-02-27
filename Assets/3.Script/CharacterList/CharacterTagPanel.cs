using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterTagPanel : MonoBehaviour
{
    [Header("Equipped Tag Slots")]
    public Image[] slotImages;       // 상단 4개의 슬롯 이미지
    public Button[] slotButtons;     // 상단 4개의 슬롯 버튼 (해제용)
    public Sprite emptySlotSprite;   // 빈 슬롯 이미지

    [Header("UI References")]
    public Transform content;        // 아이템 프리팹이 쌓일 부모
    public GameObject tagItemPrefab; // 인벤토리 아이템 프리팹 (아이콘 + 수량)

    [Header("Stat UI")]
    public TMP_Text tagEffectText;  // Tag Effect 표시

    [Header("Popup")]
    public ConfirmPopup confirmPopup; // "아이템이 사라집니다" 팝업

    public static CharacterTagPanel Instance;

    private CharacterInfo currentSelectedInfo;  // 장착할 캐릭터
    private int selectedTagItemID;     // 장착할 아이템 ID 임시 저장

    private void Awake()
    {
        Instance = this;
    }

    // 패널이 켜질 때 호출될 함수
    public void Init(CharacterInfo character)
    {
        currentSelectedInfo = character;
        confirmPopup.gameObject.SetActive(false);
        RefreshUI();
    }

    public void RefreshUI()
    {
        RefreshEquippedSlots(); // 캐릭터가 장착한 태그 표시
        RefreshInventoryList(); // 유저가 가지고 있는 태그 표시
        UpdateTagStats();   // 태그 능력치 갱신
    }

    // 1. 현재 캐릭터가 장착한 4개의 태그 표시
    private void RefreshEquippedSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            int index = i; // 클로저 문제 방지
            string tagID = currentSelectedInfo.equippedTags[i];

            if (string.IsNullOrEmpty(tagID))
            {
                slotImages[i].sprite = emptySlotSprite;
                // Remove 버튼이나 슬롯 버튼 비활성화
                slotButtons[i].gameObject.SetActive(false);
            }
            else
            {
                int itemID = int.Parse(tagID);
                slotImages[i].sprite = DataManager.Instance.GetItemData(itemID).itemIcon;

                // Remove 버튼 활성화 및 삭제 함수 연결
                slotButtons[i].gameObject.SetActive(true);
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnClickRemoveTag(index));
            }
        }
    }

    // 2. 보유 중인 태그 아이템 리스트 생성
    private void RefreshInventoryList()
    {
        foreach (Transform child in content) Destroy(child.gameObject);

        // 유저 인벤토리에서 '태그 아이템'만 필터링 (가정)
        var tagItems = DataManager.Instance.GetOwnedTagItems();

        foreach (var item in tagItems)
        {
            GameObject go = Instantiate(tagItemPrefab, content);
            ItemIcon iconScript = go.GetComponent<ItemIcon>();

            // 아이템 데이터 로드 및 셋업
            ItemData data = DataManager.Instance.GetItemData(item.itemID);
            iconScript.Setup(data, item.count); // 기존 Setup 함수 활용

            // 2. 버튼 클릭 시 장착 요청 연결 (변수명 유지)
            Button btn = go.GetComponent<Button>();
            int itemID = item.itemID;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickEquipRequest(itemID));
            }
        }
    }

    // 3. 장착 요청 (팝업 띄우기)
    public void OnClickEquipRequest(int tagID)
    {
        // 1. 빈 슬롯이 있는지 먼저 체크
        int emptyIdx = Array.FindIndex(currentSelectedInfo.equippedTags, string.IsNullOrEmpty);

        if (emptyIdx == -1)
        {
            Debug.Log("슬롯이 꽉 찼습니다.");
            return;
        }

        // 2. 장착할 ID 저장 후 팝업 띄우기
        selectedTagItemID = tagID;

        // 3. ItemData를 가져와서 팝업 셋업 후 띄우기
        ItemData data = DataManager.Instance.GetItemData(tagID);
        if (data != null)
        {
            // 팝업에 정보를 채우고 활성화
            confirmPopup.Setup(data, () => OnConfirmEquip(tagID, emptyIdx));
        }

    }

    private void OnConfirmEquip(int tagID, int slotIndex)
    {

        DataManager.Instance.EquipTag(currentSelectedInfo, tagID, slotIndex);
        confirmPopup.gameObject.SetActive(false);
        RefreshUI();

    }

    // 슬롯 클릭 시 해제 (삭제)
    private void OnClickRemoveTag(int slotIndex)
    {
        // DataManager에 만들어둔 삭제 함수 호출 (변수명 유지)
        DataManager.Instance.RemoveTag(currentSelectedInfo, slotIndex);
        RefreshUI();
    }

    private void UpdateTagStats()
    {
        if (tagEffectText == null) return;

        // DataManager에서 합산 데이터 가져오기
        var stats = DataManager.Instance.GetTotalTagStats(currentSelectedInfo.unitID);

        // 텍스트 형식에 맞춰 출력
        tagEffectText.text = $"Tag Effect : HP + {stats.hp}, ATK + {stats.atk}, SPD + {stats.spd}";
    }
}
