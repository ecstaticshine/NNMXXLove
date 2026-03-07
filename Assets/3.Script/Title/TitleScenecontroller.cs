
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TitleSceneController : MonoBehaviour
{
    [Header("Circle Settings")]
    public RectTransform circleContainer;
    public int maxCircles = 50;
    public float spawnInterval = 0.05f;

    public float sizeStart = 10f;      // 시작 크기 (멀리서 작게)
    public float sizeEnd = 300f;       // 최대 크기 (카메라 앞까지 날아옴)
    public float burstSize = 450f;     // 터질 때 크기
    public float flyDuration = 1.5f;   // 날아오는 시간
    public float burstDuration = 0.15f; // 터지는 시간

    [Header("Rain Color Settings")]
    public Color[] rainColors = new Color[]
    {
        new Color(1.0f, 0.8f, 0.8f),  // 파스텔 빨강
        new Color(1.0f, 0.9f, 0.7f),  // 파스텔 주황
        new Color(1.0f, 1.0f, 0.8f),  // 파스텔 노랑
        new Color(0.8f, 1.0f, 0.8f),  // 파스텔 초록
        new Color(0.7f, 0.9f, 1.0f),  // 파스텔 파랑
        new Color(0.8f, 0.8f, 1.0f),  // 파스텔 남색
        new Color(0.9f, 0.8f, 1.0f),  // 파스텔 보라
    };

    [Header("Title Settings")]
    public CanvasGroup titleGroup;
    public Image titleImage;        // 타이틀 이미지 컴포넌트 연결
    public Sprite titleKorean;      // 한국어 타이틀 이미지
    public Sprite titleJapanese;    // 일본어 타이틀 이미지
    public float rainDuration = 3f;
    public float fadeInDuration = 1.5f;

    [Header("Tap to Start")]
    public CanvasGroup tapToStartGroup; // "- Tap to Start -" 텍스트에 CanvasGroup 추가
    public float blinkSpeed = 2f;       // 깜빡임 속도

    private bool isReady = false;       // 타이틀 다 뜬 후에만 터치 받기

    [Header("Prologue")]
    public StoryData prologueStoryData; // 인스펙터에서 프롤로그 StoryData 연결

    private float screenHalfWidth;
    private float screenHalfHeight;

    private void Start()
    {

        AudioManager.Instance.PlayBGM("Fantasy_Daily");

        screenHalfWidth = circleContainer.rect.width / 2f;
        screenHalfHeight = circleContainer.rect.height / 2f;

        // 언어에 맞게 타이틀 이미지 설정
        int lang = PlayerPrefs.GetInt("Language", (int)DataManager.Language.KO);
        titleImage.sprite = (lang == (int)DataManager.Language.JP) ? titleJapanese : titleKorean;
        titleImage.SetNativeSize();

        titleGroup.alpha = 0f;
        StartCoroutine(TitleSequence());
    }

    private IEnumerator TitleSequence()
    {
        StartCoroutine(SpawnCircles());
        yield return new WaitForSeconds(rainDuration);
        yield return StartCoroutine(FadeInTitle());

        // tapToStart 페이드인 후 깜빡임 시작
        yield return StartCoroutine(FadeInTapToStart());
        isReady = true;
        StartCoroutine(BlinkTapToStart());
    }

    private IEnumerator FadeInTapToStart()
    {
        float elapsed = 0f;
        float duration = 0.8f;
        tapToStartGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tapToStartGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        tapToStartGroup.alpha = 1f;
    }

    private IEnumerator SpawnCircles()
    {
        while (true)
        {
            if (circleContainer.childCount < maxCircles)
                CreateCircle();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void Update()
    {
        if (!isReady) return;

        // New Input System 방식
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            isReady = false;
            StartCoroutine(GoToHome());
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isReady = false;
            StartCoroutine(GoToHome());
        }
    }

    private IEnumerator GoToHome()
    {
        // 살짝 페이드아웃 후 홈으로
        float elapsed = 0f;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            titleGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            tapToStartGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        bool hasSeenPrologue = DataManager.Instance.userData.stageHistory
    .Exists(x => x.stageID == "Prologue" && x.isStoryRead);

        if (!hasSeenPrologue)
        {
            // 프롤로그 StoryData 세팅 후 스토리 씬으로
            DataManager.Instance.selectedStoryData = prologueStoryData;

            GlobalUIManager.Instance.ChangeState(SceneState.Story, true);
        }
        else
        {
            GlobalUIManager.Instance.ChangeState(SceneState.Home, true);
        }
    }

    private void CreateCircle()
    {
        GameObject obj = new GameObject("Circle");
        obj.transform.SetParent(circleContainer, false);

        Image img = obj.AddComponent<Image>();

        // 원형으로 만들기
        img.sprite = CreateCircleSprite();

        Color baseColor = rainColors[Random.Range(0, rainColors.Length)];
        baseColor.a = Random.Range(0.4f, 0.8f);
        img.color = baseColor;

        RectTransform rt = obj.GetComponent<RectTransform>();

        // 랜덤 위치에서 시작 (화면 전체에 분산)
        float randomX = Random.Range(-screenHalfWidth * 0.8f, screenHalfWidth * 0.8f);
        float randomY = Random.Range(-screenHalfHeight * 0.8f, screenHalfHeight * 0.8f);
        rt.anchoredPosition = new Vector2(randomX, randomY);
        rt.sizeDelta = new Vector2(sizeStart, sizeStart);

        StartCoroutine(FlyAndBurst(rt, img));
    }

    private IEnumerator FlyAndBurst(RectTransform rt, Image img)
    {
        // 1. 작게 시작해서 점점 커지면서 날아오기
        float elapsed = 0f;
        Color startColor = img.color;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;

            // 크기: 작게 → 크게 (ease in)
            float currentSize = Mathf.Lerp(sizeStart, sizeEnd, t * t);
            rt.sizeDelta = new Vector2(currentSize, currentSize);

            // 날아오면서 살짝 밝아지기
            img.color = new Color(startColor.r, startColor.g, startColor.b,
                Mathf.Lerp(startColor.a * 0.5f, startColor.a, t));

            yield return null;
        }

        // 2. 팡 터지기 (순간적으로 확 커졌다가 페이드아웃)
        elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / burstDuration;

            float currentSize = Mathf.Lerp(sizeEnd, burstSize, t);
            rt.sizeDelta = new Vector2(currentSize, currentSize);

            img.color = new Color(startColor.r, startColor.g, startColor.b,
                Mathf.Lerp(startColor.a, 0f, t));

            yield return null;
        }

        if (rt != null)
            Destroy(rt.gameObject);
    }

    // 코드로 원형 스프라이트 생성
    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                // 외곽 부드럽게 처리
                float alpha = 1f - Mathf.Clamp01((dist - (radius - 4f)) / 4f);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator FadeInTitle()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            titleGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        titleGroup.alpha = 1f;
    }

private IEnumerator BlinkTapToStart()
{
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            tapToStartGroup.alpha = Mathf.Abs(Mathf.Sin(t * blinkSpeed));
            yield return null;
        }
    }
}