using UnityEngine;
using TMPro; // TextMeshPro를 사용하신다면 필수

public class UILocalizeText : MonoBehaviour
{
    [SerializeField] private string localizationKey; // 예: "Menu_Character"
    private TMP_Text targetText;

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        //DataManager 로딩 시점과의 충돌을 방지하기 위해 한 번 더!
        UpdateText();
    }

    private void OnEnable()
    {
        UpdateText();
        // 언어 변경 시 실시간 반영을 원한다면 DataManager의 이벤트에 등록
        if (DataManager.Instance != null)
            DataManager.Instance.OnDataChanged += UpdateText;
    }

    private void OnDisable()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.OnDataChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (DataManager.Instance == null || targetText == null) return;
        targetText.text = DataManager.Instance.GetLocalizedText(localizationKey);
    }
}