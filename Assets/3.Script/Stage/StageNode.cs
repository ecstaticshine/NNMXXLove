using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageNode : MonoBehaviour
{
    public int stageID; 
    public int preStageID;
    public Vector2 nodePosition; // 노드 위치
    public bool isUnlocked;

    public TMP_Text nodeStageNameText;
    public Button nodeButton;
    public Image nodeImage;
    

    public void Setup(int worldIndex, int stageId, int preStageId, float posX, float posY, bool isPreStagecleared)
    {
        stageID = stageId;
        preStageID = preStageId;
        nodePosition = new Vector2(posX, posY);
        
        //노드 스테이지명 설정
        nodeStageNameText.text = string.Format($"{worldIndex}-{stageID}");

        //노드 위치 설정
        GetComponent<RectTransform>().anchoredPosition = nodePosition;

        // 전 스테이지 클리어 됬는지 확인
        isUnlocked = isPreStagecleared || preStageID == -1;

        // 열렸으면
        if (isUnlocked)
        {
            nodeImage.color = Color.white; // 락 풀린 것처럼 보이게
            nodeButton.interactable = true;
        }
        else
        {
            nodeImage.color = Color.gray; // 락 안 풀린 것처럼 보이게
            nodeButton.interactable = false;
        }
        //셋업 시, 자기의 번호가 붙은 스테이지 상세페이지를 클릭할 수 있는 리스너를 추가
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnclickStageNode);
    }

    public void OnclickStageNode()
    {
        Debug.Log($"[StageNode] 스테이지 {stageID} 클릭됨. 매니저에게 상세창 요청 중...");
        StageManager.Instance.OpenStageDetail(stageID);
    }
}
