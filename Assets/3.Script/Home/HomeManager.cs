using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeManager : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image[] characterImages;

    private List<GameObject> spawnedCharacters = new List<GameObject>();

    private List<PartyMember> currentParty = new List<PartyMember>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(InitHome());

    }
    private void OnDestroy()
    {
        DOTween.KillAll();
    }

    private IEnumerator InitHome()
    {
        yield return new WaitUntil(() =>
            DataManager.Instance != null &&
            DataManager.Instance.userData != null &&
            DataManager.Instance.userData.stageHistory != null);

        DOTween.Init();
        yield return null; // 한 프레임 대기

        Debug.Log("[HomeManager] 데이터 준비 완료, 캐릭터 세팅 시작");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM("Summer_ice_flower");

        // 기존 캐릭터 이미지 초기화
        foreach (var img in characterImages)
        {
            img.DOKill(); // DOTween 트윈 제거
            img.gameObject.SetActive(false);
        }

        // 1. 배경 설정
        WorldDataInfo info = DataManager.Instance.GetCurrentWorldInfo();
        Debug.Log($"[HomeManager] WorldInfo: {info != null}");

        if (info != null)
        {
            backgroundImage.sprite = Resources.Load<Sprite>($"Backgrounds/home_{info.background}");
        }
        // 2. 파티 멤버 중 5명 무작위 선택 및 생성
        List<PartyMember> currentParty = DataManager.Instance.GetCurrentParty();
        List<PartyMember> selectedMembers = GetRandomFiveMembers(currentParty);

        Debug.Log($"[HomeManager] 파티 인원: {currentParty.Count}");

        for (int i = 0; i < characterImages.Length; i++)
        {
            if (i < selectedMembers.Count)
            {
                characterImages[i].gameObject.SetActive(true);

                // 데이터 로드
                UnitData data = DataManager.Instance.GetPlayerData(selectedMembers[i].unitID);
                Debug.Log($"[HomeManager] unitID:{selectedMembers[i].unitID}, data:{data != null}, sprite:{data?.unitBattleSD != null}");

                if (data == null || data.unitBattleSD == null)
                {
                    Debug.LogError($"[HomeManager] 데이터 또는 스프라이트 없음! unitID:{selectedMembers[i].unitID}");
                    continue; // 에러 나도 다음으로 진행
                }

                // 이미지 교체 (Battle SD 이미지를 사용하거나 Portrait 사용)
                characterImages[i].sprite = data.unitBattleSD;
                characterImages[i].transform.DOKill();
                // 여기에 위에서 만든 로밍(Roam) 루틴을 실행시키면 됩니다.
                StartCoroutine(RoamRoutine(characterImages[i].transform));
                Debug.Log($"[HomeManager] RoamRoutine 시작!");
            }
            else
            {
                // 파티원이 5명 미만일 경우 남는 슬롯 비활성화
                characterImages[i].gameObject.SetActive(false);
            }
        }
    }
    private List<PartyMember> GetRandomFiveMembers(List<PartyMember> fullList)
    {
        // 원본 리스트 복사 (원본 보존을 위함)
        List<PartyMember> tempList = new List<PartyMember>(fullList);
        List<PartyMember> result = new List<PartyMember>();

        // 뽑고 싶은 숫자와 실제 리스트 크기 중 작은 값을 선택
        int count = Mathf.Min(5, tempList.Count);

        for (int i = 0; i < count; i++)
        {
            int rnd = UnityEngine.Random.Range(0, tempList.Count);
            result.Add(tempList[rnd]);
            tempList.RemoveAt(rnd);
        }
        return result;
    }

    private IEnumerator RoamRoutine(Transform trans)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));

        while (true)
        {
            float currentX = trans.localPosition.x;
            // 목적지를 너무 멀지 않게 설정하면 더 자주 통통 튀는 느낌을 줍니다.
            float targetX = UnityEngine.Random.Range(-600f, 600f);
            float distance = Mathf.Abs(targetX - currentX);

            // [수정] 점프 간격을 좁게 설정 (70유닛당 1번 점프)하여 더 많이 튀게 함
            int jumpCount = Mathf.Max(1, Mathf.FloorToInt(distance / 70f));

            // [수정] 분모를 250으로 키워 이동 속도를 대폭 상향 (기존 100~150)
            float duration = distance / 250f;

            // 한 번 점프할 때 걸리는 시간
            float singleJumpDuration = duration / jumpCount;

            // 방향 전환
            float lookDir = targetX > currentX ? -1f : 1f;
            trans.localScale = new Vector3(lookDir, 1f, 1f);

            for (int i = 0; i < jumpCount; i++)
            {
                // 다음 점프 지점 계산
                float nextX = Mathf.Lerp(currentX, targetX, (float)(i + 1) / jumpCount);

                // 점프를 시작하자마자 소리 재생!
                AudioManager.Instance.PlaySE("Jump_Sound");

                // 한 번의 점프 실행
                yield return trans.DOLocalJump(new Vector3(nextX, -300f, 0f), 30f, 1, singleJumpDuration)
                    .SetEase(Ease.Linear)
                    .WaitForCompletion();
            }

            // [수정] 대기 시간도 조금 줄여서 더 활발하게 움직이게 함
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
        }
    }
}
