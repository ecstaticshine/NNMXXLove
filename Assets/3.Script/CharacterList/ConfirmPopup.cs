
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private Transform iconParent; // 아이콘이 생성될 부모 위치
    [SerializeField] private GameObject itemIconPrefab; // 인벤토리에서 쓰는 그 프리팹

    private GameObject currentIcon;

    public Button confirmBtn;
    public Button cancelBtn;

    // 팝업을 열 때 호출할 함수
    public void Setup(ItemData data, UnityAction confirmAction)
    {
        if (currentIcon != null) Destroy(currentIcon);

        // 프리팹 생성
        currentIcon = Instantiate(itemIconPrefab, iconParent);
        ItemIcon iconScript = currentIcon.GetComponent<ItemIcon>();
        iconScript.Setup(data, 1);

        // 2. 버튼 리스너 연결 (이건 꼭 필요!)
        confirmBtn.onClick.RemoveAllListeners();
        confirmBtn.onClick.AddListener(confirmAction);

        cancelBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.AddListener(() => gameObject.SetActive(false));

        gameObject.SetActive(true);
    }
}
