using UnityEngine;
using UnityEngine.UI;

public class GachaBannerButton : MonoBehaviour
{
    [Header("UI References")]
    public Image bannerImage;      // 배너 작은 이미지 (있다면)
    public Button clickButton;    // 버튼 컴포넌트

    private GachaData myData;      // 이 버튼이 들고 있는 데이터

    // GachaManager에서 배너를 생성할 때 호출하는 함수
    public void Setup(GachaData data)
    {
        myData = data;

        // 1. UI 셋팅
        if (bannerImage != null) bannerImage.sprite = data.mainBannerSprite;

        // 2. 버튼 클릭 이벤트 연결
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(OnBannerClicked);
    }

    private void OnBannerClicked()
    {
        // GachaManager에게 내가 가진 데이터를 넘겨주며 화면 갱신 요청
        GachaManager.Instance.UpdateGachaDisplay(myData);

        Debug.Log($"{myData.gachaTitle} 배너 선택됨");
    }
}
