using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DetailInfoPopup : MonoBehaviour
{
    public static DetailInfoPopup Instance = null;

    [Header("UI Components")]
    [SerializeField] private GameObject closeArea;
    [SerializeField] private GameObject popupArea;
    [SerializeField] private Image mainIcon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    // 1. 아이템 정보를 위한 Setup
    public void Setup(ItemData itemData)
    {
        ResetUI();
        nameText.text = DataManager.Instance.GetLocalizedText(itemData.itemNameKey);
        descText.text = DataManager.Instance.GetLocalizedText(itemData.descriptionKey);
        mainIcon.sprite = itemData.itemIcon;
        mainIcon.color = Color.white;
        closeArea.SetActive(true);
        popupArea.SetActive(true);
        OpenAnimation();
    }

    // 2. 유닛 정보를 위한 Setup
    public void Setup(UnitData unitData)
    {
        ResetUI();
        nameText.text = DataManager.Instance.GetLocalizedText(unitData.unitNameKey);
        descText.text = DataManager.Instance.GetLocalizedText(unitData.descriptionKey);
        mainIcon.sprite = unitData.unitPortrait;
        mainIcon.color = Color.white;
        closeArea.SetActive(true);
        popupArea.SetActive(true);
    }

    private void ResetUI()
    {
        nameText.text = string.Empty;
        descText.text = string.Empty;

        if (mainIcon != null)
        {
            mainIcon.sprite = null;
            mainIcon.color = new Color(1, 1, 1, 0); // 투명하게
        }
    }

    public void Close()
    {

        closeArea.SetActive(false);
        popupArea.SetActive(false);

    }

    private void OpenAnimation()
    {
        popupArea.SetActive(true);

        // PopupArea만 살짝 커지면서 나타나는 연출
        if (popupArea != null)
        {
            popupArea.transform.localScale = Vector3.one * 0.8f;
            popupArea.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack);
        }
    }

}
