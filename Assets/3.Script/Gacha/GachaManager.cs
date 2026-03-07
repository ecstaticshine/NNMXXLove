using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance = null;

    [Header("Data")]
    public List<GachaData> allGachaDatas;

    [Header("UI References (오른쪽 콘텐츠 영역)")]
    public VideoPlayer gachaVideo;
    public TMP_Text timeLimitText;
    public Image mainBannerImage; // 메인 일러스트용

    [Header("Banner List")]
    public Transform bannerParent;
    public GameObject bannerPrefab;

    private GachaData currentSelectedData; // 현재 선택된 가챠 정보

    public GachaResultUI resultUI; // 가챠 결과

    private System.Random rng = new System.Random();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("kinematic_01");
        }

        GachaData[] loadedGachas = Resources.LoadAll<GachaData>("Data/GachaDatas");
        allGachaDatas = new List<GachaData>(loadedGachas);

        DataManager.Instance.LoadAllUnitDatas();


        // 1. 왼쪽 배너 생성 (이건 데이터만큼 뽑아야 하니 유지)
        foreach (GachaData data in allGachaDatas)
        {
            //가챠 캐릭터 풀 적용
            data.InitGachaPool(DataManager.Instance.allUnitDatas);

            Debug.Log($"{data.gachaTitle} 체크 -> TL:{data.tlPool.Count}, PL:{data.plPool.Count}, L:{data.lPool.Count}");

            GameObject go = Instantiate(bannerPrefab, bannerParent);
            go.GetComponent<GachaBannerButton>().Setup(data);
        }

        // 2. 초기 가챠 설정
        if (allGachaDatas.Count > 0) UpdateGachaDisplay(allGachaDatas[0]);
    }

    // 프리팹 생성 대신 "내용 교체" 함수
    public void UpdateGachaDisplay(GachaData data)
    {
        currentSelectedData = data;

        // 비주얼 교체
        gachaVideo.clip = data.bgVideo;
        gachaVideo.Play();

        if (mainBannerImage != null) mainBannerImage.sprite = data.mainBannerSprite;

        // 텍스트 교체
        timeLimitText.text = $"{data.startDateTime} ~ {data.endDateTime}";
        // descriptionText.text = data.description; // 필요한 설명값

        Debug.Log($"{data.gachaTitle} 가챠로 화면 갱신 완료!");
    }

    public string Pull(GachaData data)
    {


        int tlRate = (int)data.rates[0] * 100; // 300
        int plRate = (int)data.rates[1] * 100; // 2700

        Debug.Log($"[확률 확인] TL:{data.rates[0]}, PL:{data.rates[1]}, L:{data.rates[2]}");
        Debug.Log($"[확률 확인] tlRate:{tlRate}, plRate:{plRate}");
        Debug.Log($"[확률 확인] rand 범위: 0~9999, TL범위: 0~{tlRate - 1}, PL범위: {tlRate}~{tlRate + plRate - 1}");

        Debug.Log($"[가챠] tlRate: {tlRate}, plRate: {plRate}, Sum: {tlRate + plRate}");

        var pityData = DataManager.Instance.userData.gachaPityList.Find(x => x.gachaID == data.gachaID);
        if (pityData == null)
        {
            pityData = new GachaSaveData { gachaID = data.gachaID, currentPity = 0 };
            DataManager.Instance.userData.gachaPityList.Add(pityData);
        }

        if (DataManager.Instance.userData.diamond < data.costPerPull) return "NotEnoughDiamond";

        DataManager.Instance.userData.diamond -= data.costPerPull;
        pityData.currentPity++;

        int resultID = 0;

        // 1. 천장 체크
        if (pityData.currentPity >= data.maxPity)
        {
            resultID = data.pickupUnitID;
            pityData.currentPity = 0;
        }
        else
        {
            int totalRate = 10000;                   // 10000 (100%)

            // 2. 0 ~ 9999 사이의 정수 생성
            int rand = rng.Next(0, totalRate);

            // 3. 누적 확률 비교
            if (rand < tlRate)
            {
                // 0 ~ 299 범위 (정확히 3%)
                resultID = data.tlPool[UnityEngine.Random.Range(0, data.tlPool.Count)];
            }
            else if (rand < (tlRate + plRate))
            {
                // 300 ~ 2999 범위 (정확히 27%)
                resultID = data.plPool[UnityEngine.Random.Range(0, data.plPool.Count)];
            }
            else
            {
                // 3000 ~ 9999 범위 (정확히 70%)
                resultID = data.lPool[UnityEngine.Random.Range(0, data.lPool.Count)];
            }
        }

        GlobalUIManager.Instance.RefreshCurrentUI();

        DataManager.Instance.AddCharacter(resultID);

        return resultID.ToString();
    }

    public void OnPullButtonClick(int count)
    {
        if (currentSelectedData == null) return;

        List<int> finalResultIDs = new List<int>();

        for (int i = 0; i < count; i++)
        {
            string res = Pull(currentSelectedData);
            // 다이아 부족 체크
            if (res == "NotEnoughDiamond")
            {
                Debug.Log("다이아가 부족합니다!");
                break;
            }

            if (int.TryParse(res, out int unitID))
            {
                finalResultIDs.Add(unitID);
            }
        }

        if (finalResultIDs.Count > 0)
        {
            resultUI.ShowResult(finalResultIDs, currentSelectedData.pickupUnitID);

            DataManager.Instance.SaveData();
            DataManager.Instance.OnDataChanged?.Invoke();
        }
    }
}

