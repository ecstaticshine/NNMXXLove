using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Localization Keys")]
    [SerializeField] protected string nameKey;        // 예: "Stamina_Name"
    [SerializeField] protected string descriptionKey; // 예: "Stamina_Desc"

    protected bool isHovering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DataManager.Instance == null || TooltipManager.Instance == null) return;

        isHovering = true;
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    // 실제 표시 로직 (자식에서 오버라이드 가능)
    protected virtual void Show()
    {
        string title = DataManager.Instance.GetLocalizedText(nameKey);
        string desc = DataManager.Instance.GetLocalizedText(descriptionKey);
        TooltipManager.Instance.ShowTooltip(title, desc);
    }
}
