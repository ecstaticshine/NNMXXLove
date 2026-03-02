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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        

        GachaData[] loadedGachas = Resources.LoadAll<GachaData>("Data/GachaDatas");
        allGachaDatas = new List<GachaData>(loadedGachas);

        DataManager.Instance.LoadAllUnitDatas();


        // 1. 왼쪽 배너 생성 (이건 데이터만큼 뽑아야 하니 유지)
        foreach (GachaData data in allGachaDatas)
        {
            //가챠 캐릭터 풀 적용
            data.InitGachaPool(DataManager.Instance.allUnitDatas);

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
        var pityData = DataManager.Instance.userData.gachaPityList.Find(x => x.gachaID == data.gachaID);
        if (pityData == null)
        {
            pityData = new GachaSaveData { gachaID = data.gachaID, currentPity = 0 };
            DataManager.Instance.userData.gachaPityList.Add(pityData);
        }

        if (DataManager.Instance.userData.diamond < 100) return "NotEnoughDiamond";

        DataManager.Instance.userData.diamond -= 100;
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
            // 2. 확률 계산
            float rand = UnityEngine.Random.Range(0f, 100f);
            if (rand <= data.rates[0]) // TL
                resultID = data.tlPool[UnityEngine.Random.Range(0, data.tlPool.Count)];
            else if (rand <= data.rates[0] + data.rates[1]) // PL
                resultID = data.plPool[UnityEngine.Random.Range(0, data.plPool.Count)];
            else // L
                resultID = data.lPool[UnityEngine.Random.Range(0, data.lPool.Count)];
        }

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
            resultUI.ShowResult(finalResultIDs);

            DataManager.Instance.SaveData();
            DataManager.Instance.OnDataChanged?.Invoke();
        }
    }
}

