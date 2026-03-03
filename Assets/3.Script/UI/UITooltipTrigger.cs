using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("Localization Keys")]
    [SerializeField] protected string nameKey;        // 예: "Stamina_Name"
    [SerializeField] protected string descriptionKey; // 예: "Stamina_Desc"

    [SerializeField] protected Sprite displayIcon;

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (DataManager.Instance == null || DetailInfoPopup.Instance == null) return;

        ShowDetail();
    }

    protected virtual void ShowDetail()
    {
        // 텍스트는 로컬라이징 키를 통해 가져옵니다.
        string title = DataManager.Instance.GetLocalizedText(nameKey);
        string desc = DataManager.Instance.GetLocalizedText(descriptionKey);

        // 아이콘이 있으면 같이 넘겨주고, 없으면 null을 넘깁니다.
        DetailInfoPopup.Instance.SetupCustom(title, desc, displayIcon);
    }
}
