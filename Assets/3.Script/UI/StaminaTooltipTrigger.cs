using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class StaminaTooltipTrigger : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string nameKey = "STAMINA_NAME";
    [SerializeField] private string descriptionKey = "STAMINA_DESC";

    public void OnPointerClick(PointerEventData eventData)
    {
        ShowStaminaDetail();
    }

    private void ShowStaminaDetail()
    {
        string title = DataManager.Instance.GetLocalizedText(nameKey);
        string baseDesc = DataManager.Instance.GetLocalizedText(descriptionKey);

        // 시간을 계산해서 문자열로 만드는 로직 (전달해드린 코드 활용)
        string timeInfo = GetStaminaTimeText();

        DetailInfoPopup.Instance.SetupCustom(title, string.Format(baseDesc, timeInfo));
    }

    private string GetStaminaTimeText()
    {
        var userData = DataManager.Instance.userData;
        string timeStatus;

        if (userData.stamina >= DataManager.Instance.maxStamina)
        {
            timeStatus = DataManager.Instance.GetLocalizedText("Stamina_Full"); // "이미 가득 찼습니다."
        }
        else
        {
            // 남은 시간 계산 로직
            DateTime lastTime = DateTime.Parse(userData.lastStaminaUpdateTime);
            TimeSpan timeSinceLastUpdate = TimeSpan.FromSeconds(DataManager.Instance.staminaRegenSeconds) - (DateTime.Now - lastTime);

            double nextSeconds = DataManager.Instance.staminaRegenSeconds - timeSinceLastUpdate.TotalSeconds;
            if (nextSeconds < 0) nextSeconds = 0;

            // 전체 풀 회복까지 남은 시간 계산
            int missingStamina = DataManager.Instance.maxStamina - userData.stamina;
            int totalSecondsLeft = (missingStamina - 1) * DataManager.Instance.staminaRegenSeconds + (int)timeSinceLastUpdate.TotalSeconds;

            TimeSpan totalSpan = TimeSpan.FromSeconds(totalSecondsLeft);
            timeStatus = string.Format("{0:D2}분 {1:D2}초", (int)totalSpan.TotalMinutes, totalSpan.Seconds);
        }

        return timeStatus;
    }
}
