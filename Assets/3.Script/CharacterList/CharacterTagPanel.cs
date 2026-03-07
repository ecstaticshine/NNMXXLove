using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterTagPanel : MonoBehaviour
{
    [Header("Equipped Tag Slots")]
    public ItemIcon[] tagSlots;       // 상단 4개의 슬롯
    public Button[] slotButtons;     // 상단 4개의 슬롯 버튼 (해제용)
    public Sprite emptySlotSprite;   // 빈 슬롯 이미지

    [Header("UI References")]
    public Transform content;        // 아이템 프리팹이 쌓일 부모
    public GameObject tagItemPrefab; // 인벤토리 아이템 프리팹 (아이콘 + 수량)

    [Header("Stat UI")]
    public TMP_Text tagEffectText;  // Tag Effect 표시

    [Header("Popup")]
    public ConfirmPopup confirmPopup; // "아이템이 사라집니다" 팝업

    [Header("Pool Management")]
    private List<ItemIcon> inventoryPool = new List<ItemIcon>();

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
                // Remove 버튼이나 슬롯 버튼 비활성화
                tagSlots[i].gameObject.SetActive(false);
            }
            else
            {
                int itemID = int.Parse(tagID);
                ItemData data = DataManager.Instance.GetItemData(itemID);

                tagSlots[i].Setup(data, 1);
                tagSlots[i].isEquippedSlot = true;
                tagSlots[i].gameObject.SetActive(true);

                // Remove 버튼 활성화 및 삭제 함수 연결
                slotButtons[i].gameObject.SetActive(true);
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => {
                    // 이미 장착된 상태이므로, 클릭 시 상세 설명 팝업을 띄우기
                    OnClickRemoveTag(index);
                });

                // 슬롯 아이콘 클릭 → 상세 정보
                Button iconBtn = tagSlots[i].GetComponent<Button>();
                if (iconBtn != null)
                {
                    iconBtn.onClick.RemoveAllListeners();
                    iconBtn.onClick.AddListener(() => DetailInfoPopup.Instance.Setup(data));
                }
            }
        }
    }

    // 2. 보유 중인 태그 아이템 리스트 생성
    private void RefreshInventoryList()
    {
        foreach (var item in inventoryPool)
        {
            if (item != null) item.gameObject.SetActive(false);
        }

        // 유저 인벤토리에서 '태그 아이템'만 필터링 (가정)
        var tagItems = DataManager.Instance.GetOwnedTagItems();

        for (int i = 0; i < tagItems.Count; i++)
        {
            ItemIcon iconScript;

            // 2. 풀에 이미 생성된 게 있다면 재사용, 부족하면 생성합니다.
            if (i < inventoryPool.Count)
            {
                iconScript = inventoryPool[i];
            }
            else
            {
                GameObject go = Instantiate(tagItemPrefab, content);
                iconScript = go.GetComponent<ItemIcon>();
                inventoryPool.Add(iconScript);
            }

            // 3. 데이터를 셋업하고 다시 활성화합니다.
            iconScript.gameObject.SetActive(true);
            ItemData data = DataManager.Instance.GetItemData(tagItems[i].itemID);
            iconScript.isEquippedSlot = false;
            iconScript.Setup(data, tagItems[i].count);

            // 4. 버튼 이벤트 연결 (중복 등록 방지를 위해 RemoveAllListeners 필수!)
            Button btn = iconScript.GetComponent<Button>();
            int itemID = tagItems[i].itemID;
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
