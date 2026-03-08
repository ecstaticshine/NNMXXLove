using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemManager : MonoBehaviour
{
    private void Awake()
    {
        // 이미 EventSystem이 존재하면 자신을 파괴
        if (FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
        }
    }
}