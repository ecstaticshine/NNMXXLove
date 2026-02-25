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

        // 만약 인벤토리가 비어있다면 테스트를 위해 기본 캐릭터 2개를 넣어줌
        if (characterInventory.Count == 0)
        {
            characterInventory.Add(new CharacterInfo { unitID = 1, level = 1 });
            characterInventory.Add(new CharacterInfo { unitID = 2, level = 1 });
        }

        foreach (CharacterInfo info in characterInventory)
        {
            // 아이콘 생성
            GameObject go = Instantiate(unitIconPrefab, content);
            UnitIcon iconScript = go.GetComponent<UnitIcon>();

            // 3. 실제 데이터(ScriptableObject)를 가져와서 UI에 세팅
            UnitData data = DataManager.Instance.GetPlayerData(info.unitID);
            if (data != null)
            {
                // 여기서 에러 방지를 위해 확실히 데이터를 넣어줍니다!
                iconScript.SetUnitIcon(data, info);
            }
        }
    }
}