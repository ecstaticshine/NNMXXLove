using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ScrollController : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 0.5f;


    private float step = 0.25f;

    public void OnClickLeft()
    {
        float targetPos = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - step);
        scrollRect.DOHorizontalNormalizedPos(targetPos, scrollSpeed).SetEase(Ease.OutQuad);
    }

    public void OnClickRight()
    {
        float targetPos = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + step);
        scrollRect.DOHorizontalNormalizedPos(targetPos, scrollSpeed).SetEase(Ease.OutQuad);
    }
}
