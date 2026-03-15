using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

public class LoadingSceneController : MonoBehaviour
{
    public static string targetScene;
    public static List<string> preloadKeys = new List<string>();

    [Header("UI")]
    public Slider progressBar;
    public TMP_Text loadingText;

    private void Start()
    {
        StartCoroutine(LoadSceneAsync());    
    }

    public static void LoadScene(string sceneName, List<string> keys = null)
    {
        targetScene = sceneName;
        preloadKeys = keys ?? new List<string>();
        SceneManager.LoadScene("LoadingScene");
    }

    private IEnumerator LoadSceneAsync()
    {
        float displayProgress = 0f;

        if(preloadKeys.Count > 0)
        {
            loadingText.text = "에셋 로드 중...";

            var handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(
                preloadKeys, null, Addressables.MergeMode.Union);

            while (!handle.IsDone)
            {
                displayProgress = handle.PercentComplete * 0.5f; // 전체의 50%
                progressBar.value = displayProgress;
                loadingText.text = $"에셋 로드 중... {Mathf.RoundToInt(displayProgress * 200)}%";
                yield return null;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
                Debug.LogWarning("[Loading] 일부 에셋 로드 실패");
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress/0.9f);

            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.deltaTime);

            progressBar.value = displayProgress;
            loadingText.text = $"Now Loading... {Mathf.RoundToInt(displayProgress * 100)}%";

            // 90% 이상이면 씬 전환
            if (displayProgress >= 0.99f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
