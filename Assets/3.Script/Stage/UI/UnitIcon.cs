using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitIcon : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private Image backboard;
    [SerializeField] private GameObject bossMark;

    [Header("Ally Frames (Love Series)")]
    public Sprite[] allyFrames;  // L, PL, TL, EL 순서대로

    [Header("Enemy Frames (Level Series)")]
    public Sprite[] enemyFrames; // L, PL, TL, EL 순서대로

    public void SetUnitIcon(UnitData data, int level)
    {
        if (data == null) return;

        // SO에서 초상화 가져오기
        unitIcon.sprite = data.unitPortrait;

        // 레벨 표시
        levelText.text = $"{level}";

        // 등급(Rarity)에 따른 연출 처리
        UpdateRarityUI(data.isEnemy, data.rarity);
    }

    private void UpdateRarityUI(bool isEnemy, Rarity rarity)
    {
        // 보스 마크는 EL(Elite Level)일 때만 활성화
        if (bossMark != null)
        {
            bossMark.SetActive(rarity == Rarity.EL);
        }

        // 프레임 스프라이트 바꾸기
        int rarityIndex = (int)rarity;

        Sprite[] targetFrames = isEnemy ? enemyFrames : allyFrames;

        if (targetFrames != null && rarityIndex < targetFrames.Length)
        {
            rarityFrame.sprite = targetFrames[rarityIndex];
        }

        // 백보드 색상 적용
        if (backboard != null)
        {
            backboard.color = GetBoardColor(isEnemy, rarity);
        }
    }

    private Color GetBoardColor(bool isEnemy, Rarity rarity)
    {
        string hex;

        if (isEnemy)
        {
            hex = rarity switch
            {
                Rarity.L => "#5F6267",
                Rarity.PL => "#546071",
                Rarity.TL => "#533960",
                Rarity.EL => "#37363E",
                _ => "#5F6267"
            };

        }
        else
        {
            hex = rarity switch
            {
                Rarity.L => "#7A6B6B", 
                Rarity.PL => "#6B5471",
                Rarity.TL => "#854552",
                Rarity.EL => "#4A363E",
                _ => "#5F6267"
            };
        }

        if (ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
        return Color.gray; // 예외 시 기본 회색
    }
}
