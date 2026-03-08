using System.Collections.Generic;
using UnityEngine;

public class CharacterListUI : MonoBehaviour
{
    public static CharacterListUI Instance { get; private set; }

    public GameObject unitIconPrefab; // 아까 만든 UnitIcon 프리팹
    public Transform content;         // ScrollView의 Content 오브젝트

    void Start()
    {
        RefreshList();
    }

    public void RefreshList()
    {

        // 1. 초기화
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // 2. DataManager 인벤토리 사용
        List<CharacterInfo> characterInventory = DataManager.Instance.userInventory;

        foreach (CharacterInfo info in characterInventory)
        {
            // 3. 실제 데이터(ScriptableObject)를 가져옴
            UnitData data = DataManager.Instance.GetPlayerData(info.unitID);

            if (data != null)
            {
                // [핵심] UI에 넘기기 전에 반드시 점수를 먼저 계산합니다!
                info.InitializePoint(data, info.currentBreakthrough);

                // 아이콘 생성 및 세팅
                GameObject go = Instantiate(unitIconPrefab, content);
                UnitIcon iconScript = go.GetComponent<UnitIcon>();

                iconScript.SetUnitIcon(data, info);
            }
        }
    }
}