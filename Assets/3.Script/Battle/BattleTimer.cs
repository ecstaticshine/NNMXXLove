using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BattleTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text time_text;
    private float remainTime = 10f;
    private bool isTimerRunning = false;

    public Action OnTimerOut;

    private void Update()
    {
        //퍼즈 걸리거나 내 턴 아니면 냅두기
        if (!isTimerRunning || Time.timeScale == 0) return;

        if (remainTime > 0)
        {
            remainTime -= Time.deltaTime;

            UpdateDisplayTime();
        }
        else
        {
            remainTime = 0;
            isTimerRunning = false;
            UpdateDisplayTime();

            // 이벤트 알림
            OnTimerOut?.Invoke();
        }
    }

    // 버튼용
    public void StartTimer() => isTimerRunning = true;
    public void StopTimer() => isTimerRunning = false;

    private void UpdateDisplayTime()
    {
        time_text.text = Mathf.CeilToInt(remainTime).ToString();
    }
}
