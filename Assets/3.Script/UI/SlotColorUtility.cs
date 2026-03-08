using UnityEngine;
using UnityEngine.UI;

public static class SlotColorUtility
{
    // 태그별 색상 정의
    public static Color GetColorByTag(string tag)
    {
        switch (tag)
        {
            case "Direct": return new Color(1f, 0.3f, 0.3f, 0.6f);
            case "Splash": return new Color(0.3f, 0.5f, 1f, 0.6f);
            case "Dot": return new Color(0.3f, 1f, 0.3f, 0.6f);
            default: return new Color(1f, 1f, 1f, 0.4f); // 기본/없을 때
        }
    }

}
