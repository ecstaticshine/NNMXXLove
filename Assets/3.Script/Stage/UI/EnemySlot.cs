using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemySlot : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private GameObject levelArea;
    [SerializeField] private GameObject bossMark;

    public void SetSlot(UnitData data, int level)
    {
        if (data == null) return;

        // SO에서 초상화 가져오기
        portrait.sprite = data.unitPortrait;

        // 레벨 표시
        levelArea.GetComponentInChildren<TMP_Text>().text = $"{level}";


        // 만약 몬스터 타입이나 레어리티에 따라 연출을 다르게 하고 싶다면 여기서 처리!
        // 예: if (data.rarity == Rarity.EL) bossMark.SetActive(true);
    }
}
