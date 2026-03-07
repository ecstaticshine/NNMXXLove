using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestartPopup : MonoBehaviour
{
    public GameObject darkArea;

    public void OnClickRestartNow()
    {
        PlayerPrefs.Save();
        GlobalUIManager.Instance.ClearStateStack();
        SceneManager.LoadScene("TitleScene"); // 타이틀씬 이름으로 바꿔주세요
    }

    public void OnClickLater()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // DarkArea 클릭 시 닫기 연결
        if (darkArea != null)
        {
            Button darkBtn = darkArea.GetComponent<Button>();
            if (darkBtn != null)
            {
                darkBtn.onClick.RemoveAllListeners();
                darkBtn.onClick.AddListener(OnClickLater);
            }
        }
    }
}