using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageMapManager : MonoBehaviour
{
    public GameObject stageButtonPrefab; // 스테이지 버튼 프리팹
    public Transform contentTransform;  // 지도가 있는 Content 오브젝트

    void Start()
    {
        //GenerateStageNodes("World_1"); // 1번 월드 생성 시작
    }

    //public void GenerateStageNodes(string worldID)
    //{
    //    // 1. CSV에서 해당 월드의 모든 스테이지 정보를 리스트로 가져옴
    //    List<StageData> stageList = StageDataManager.instance.GetStagesInWorld(worldID);

    //    foreach (var data in stageList)
    //    {
    //        // 2. 버튼 생성
    //        GameObject btn = Instantiate(stageButtonPrefab, contentTransform);

    //        // 3. 좌표 설정 (RectTransform 사용)
    //        RectTransform rt = btn.GetComponent<RectTransform>();
    //        rt.anchoredPosition = new Vector2(data.posX, data.posY);

    //        // 4. 버튼 초기화 (텍스트 설정, 해금 여부 체크 등)
    //        btn.GetComponent<StageButton>().Setup(data);
    //    }
    //}
}
