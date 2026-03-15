using UnityEngine;
using System.Threading.Tasks;

public class AddressableTest : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("[테스트] 시작");

        ItemData item = await DataManager.Instance.GetItemDataAsync(1001);

        if (item != null)
            Debug.Log($"[테스트] 로드 성공! 아이템 이름: {item.itemNameKey}");
        else
            Debug.LogError("[테스트] 로드 실패!");
    }
}