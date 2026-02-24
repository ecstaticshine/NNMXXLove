using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class StaminaTooltipTrigger : UITooltipTrigger
{
    private void Update()
    {
        if (isHovering) Show(); // 마우스를 올리고 있을 때만 매 프레임 갱신
    }

    protected override void Show()
    {
        string title = DataManager.Instance.GetLocalizedText(nameKey);
        string baseDesc = DataManager.Instance.GetLocalizedText(descriptionKey);

        // 시간을 계산해서 문자열로 만드는 로직 (전달해드린 코드 활용)
        string timeInfo = GetStaminaTimeText();

        TooltipManager.Instance.ShowTooltip(title, string.Format(baseDesc, timeInfo));
    }

    private string GetStaminaTimeText()
    {
        var userData = DataManager.Instance.userData;
        string timeStatus;

        if (userData.stamina >= DataManager.Instance.maxStamina)
        {
            timeStatus = "이미 가득 찼습니다."; // 이 부분도 키값으로 관리 권장
        }
        else
        {
            // 남은 시간 계산 로직
            DateTime lastTime = DateTime.Parse(userData.lastStaminaUpdateTime);
            TimeSpan nextRegenTime = TimeSpan.FromSeconds(DataManager.Instance.staminaRegenSeconds) - (DateTime.Now - lastTime);

            double elapsedSeconds = (DateTime.Now - lastTime).TotalSeconds;
            double nextSeconds = DataManager.Instance.staminaRegenSeconds - elapsedSeconds;

            // 전체 풀 회복까지 남은 시간 계산
            int missingStamina = DataManager.Instance.maxStamina - userData.stamina;
            int totalSecondsLeft = (missingStamina - 1) * DataManager.Instance.staminaRegenSeconds + (int)nextRegenTime.TotalSeconds;

            TimeSpan totalSpan = TimeSpan.FromSeconds(totalSecondsLeft);
            timeStatus = string.Format("{0:D2}분 {1:D2}초", (int)totalSpan.TotalMinutes, totalSpan.Seconds);
        }

        return timeStatus;
    }
}
