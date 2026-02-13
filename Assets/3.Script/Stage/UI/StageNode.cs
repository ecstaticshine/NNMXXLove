using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageNode : MonoBehaviour
{
    public string stageID; 
    public string preStageID;
    public Vector2 nodePosition; // 노드 위치
    public bool isUnlocked;

    public TMP_Text nodeStageNameText;
    public Button nodeButton;
    public Image nodeImage;
    

    public void Setup(StageDetailData data, bool isUnlocked)
    {
        this.stageID = data.stageID;      // "W01S01"
        this.preStageID = data.prevStageID; // "None" Or "W01S01"
        this.nodePosition = data.nodePos;
        this.isUnlocked = isUnlocked;

        //노드 스테이지명 설정
        string worldNum = data.stageID.Substring(1, 2); // "01"
        string stageNum = data.stageID.Substring(4, 2); // "01"
        nodeStageNameText.text = $"{int.Parse(worldNum)}-{int.Parse(stageNum)}";

        //노드 위치 설정
        GetComponent<RectTransform>().anchoredPosition = nodePosition;

        //노드 시각적으로 상태 표시
        UpdateVisualState(isUnlocked);

        //셋업 시, 자기의 번호가 붙은 스테이지 상세페이지를 클릭할 수 있는 리스너를 추가
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnclickStageNode);
    }

    private void UpdateVisualState(bool isUnlocked)
    {
        // 이미지 투명도나 색상으로 잠금 상태 표현
        nodeImage.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
        nodeButton.interactable = isUnlocked;

        // 잠긴 노드라면 텍스트도 어둡게
        if (nodeStageNameText != null)
            nodeStageNameText.color = isUnlocked ? Color.white : Color.gray;
    }

        public void OnclickStageNode()
    {
        Debug.Log($"[StageNode] 스테이지 {stageID} 클릭됨. 매니저에게 상세창 요청 중...");
        StageManager.Instance.OpenStageDetail(this.stageID);
    }
}
