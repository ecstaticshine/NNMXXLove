using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "GachaData", menuName = "Gacha/New Gacha")]
public class GachaData : ScriptableObject
{
    [Header("기본 정보")]
    public int gachaID;
    public string gachaTitle;

    [Header("캐릭터 풀")]
    public int pickupUnitID;        // 픽업 캐릭터 ID
    [HideInInspector] public List<int> tlPool;          // TL 캐릭터 ID 리스트
    [HideInInspector] public List<int> plPool;           // PL 캐릭터 ID 리스트
    [HideInInspector] public List<int> lPool;            // L 캐릭터 ID 리스트

    [Header("기간 설정 (형식: yyyy-MM-dd HH:mm)")]
    public string startDateTime; // 예: "2026-03-03 00:00"
    public string endDateTime;   // 예: "2026-03-09 10:59"

    [Header("가챠 연출 및 비용")]
    public VideoClip bgVideo;       // 가챠 배경 영상
    public Sprite bannerSprite;     // 배너 이미지
    public Sprite mainBannerSprite; // 메인 배너 이미지
    public int costPerPull = 100;   // 1회 가격

    [Header("확률 및 천장")]
    public float[] rates = { 3f, 27f, 70f }; // SSR, SR, R 확률
    public int maxPity = 50;        // UI 표시 및 로직용 천장 값

    [Header("Localization Keys")]   // 나중에 다국어를 고려한다면
    public string descriptionKey;

    private bool isPoolLoaded = false;

    public bool IsActive()
    {
        string format = "yyyy-MM-dd HH:mm";
        bool startValid = DateTime.TryParseExact(startDateTime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start);
        bool endValid = DateTime.TryParseExact(endDateTime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end);

        if (startValid && endValid)
        {
            return DateTime.Now >= start && DateTime.Now <= end;
        }
        return false;
    }


    public void InitGachaPool(List<UnitData> allUnits)
    {
        if (isPoolLoaded) return;

        tlPool.Clear(); plPool.Clear(); lPool.Clear();

        foreach (var unit in allUnits)
        {
            // UnitData에 등급(Rarity) 정보가 있다고 가정
            switch (unit.rarity)
            {
                case Rarity.TL: tlPool.Add(unit.unitID); break;
                case Rarity.PL: plPool.Add(unit.unitID); break;
                case Rarity.L: lPool.Add(unit.unitID); break;
            }
        }
        isPoolLoaded = true;
        Debug.Log($"{gachaTitle} 풀 생성 완료! (SSR:{tlPool.Count}, SR:{plPool.Count}, R:{lPool.Count})");
    }
}