
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image tagIcon;        // 장착할 태그 이미지
    [SerializeField] private TMP_Text tagNameText; // 태그 이름
    [SerializeField] private TMP_Text tagEffectText; // 태그 효과 (예: ATK +500)

    public Button confirmBtn;
    public Button cancelBtn;

    // 팝업을 열 때 호출할 함수
    public void Setup(ItemData data)
    {
        if (data == null) return;

        // 아이콘 설정
        tagIcon.sprite = data.itemIcon; // ItemData에 아이콘 변수가 있다고 가정

        // 이름 설정 (로컬라이징 적용)
        tagNameText.text = DataManager.Instance.GetLocalizedText(data.itemNameKey);

        // 효과 텍스트 설정 (effectStatType과 effectValue 활용)
        tagEffectText.text = $"{data.effectStatType} : +{data.effectValue}";

        // 팝업 활성화
        gameObject.SetActive(true);
    }
}
