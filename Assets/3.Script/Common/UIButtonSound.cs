using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    public string sfxName = "Button_Click"; // 기본 소리 이름

    void Awake()
    {
        // 버튼 컴포넌트를 가져와서 클릭 이벤트를 자동으로 연결해요!
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }

    void PlaySound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(sfxName);
        }
    }
}
