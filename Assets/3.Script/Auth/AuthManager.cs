using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using DG.Tweening;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;

    public bool IsLoggedIn => currentUser != null;
    public bool IsGuest => currentUser != null && currentUser.IsAnonymous;
    public string UserID => currentUser?.UserId ?? SystemInfo.deviceUniqueIdentifier;

    // 로그인 상태 변경 시 UI에 알림
    public static event System.Action OnAuthStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitFirebase());
    }

    private IEnumerator InitFirebase()
    {
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
            currentUser = auth.CurrentUser;

            if (currentUser == null)
            {
                var guestTask = LoginAsGuest();
                yield return new WaitUntil(() => guestTask.IsCompleted);
            }
            else
            {
                Debug.Log($"[Firebase] 자동 로그인 성공! ID: {currentUser.UserId}");

                // 타임아웃 추가
                var syncTask = SyncUserData();
                float timeout = 10f;
                float elapsed = 0f;
                while (!syncTask.IsCompleted && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!syncTask.IsCompleted)
                {
                    Debug.LogWarning("[Firebase] Firestore 타임아웃 - 로컬 데이터 사용");
                }
            }

            DataManager.Instance.LoadData();
            DataManager.Instance.LoadGameDataByWorld(DataManager.Instance.currentWorldIndex);

            OnAuthStateChanged?.Invoke();
        }
        else
        {
            Debug.LogError($"[Firebase] 초기화 실패: {task.Result}");
        }
    }

    // 게스트 로그인
    public async Task LoginAsGuest()
    {
        try
        {
            var result = await auth.SignInAnonymouslyAsync();
            currentUser = result.User;
            Debug.Log($"[Firebase] 게스트 로그인 성공! ID: {currentUser.UserId}");
            await SyncUserData();
            OnAuthStateChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firebase] 게스트 로그인 실패: {e.Message}");
        }
    }

    // 로그아웃
    public void Logout()
    {
        auth.SignOut();
        currentUser = null;
        Debug.Log("[Firebase] 로그아웃 완료");
        OnAuthStateChanged?.Invoke();
    }

    // 구글 로그인 (추후 연결)
    public void LoginWithGoogle()
    {
        Debug.Log("구글 로그인 - 추후 지원 예정");
    }

    // Firestore에 UserData 저장
    public async Task SaveUserDataToFirestore()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return;
#endif
        if (currentUser == null) return;
        try
        {
            string json = JsonUtility.ToJson(DataManager.Instance.userData);
            DocumentReference docRef = db.Collection("users").Document(currentUser.UserId);
            await docRef.SetAsync(new { data = json, updatedAt = System.DateTime.UtcNow.ToString() });
            Debug.Log("[Firebase] 데이터 저장 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firebase] 저장 실패: {e.Message}");
        }
    }

    // Firestore에서 UserData 불러오기
    public async Task<bool> LoadUserDataFromFirestore()
    {
        if (currentUser == null) return false;
        try
        {
            DocumentReference docRef = db.Collection("users").Document(currentUser.UserId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                string json = snapshot.GetValue<string>("data");
                DataManager.Instance.userData = JsonUtility.FromJson<UserData>(json);
                Debug.Log("[Firebase] 데이터 불러오기 완료!");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firebase] 불러오기 실패: {e.Message}");
        }
        return false;
    }

    private async Task SyncUserData()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // PC/에디터에서는 Firestore 스킵, 로컬만 사용
        Debug.Log("[Firebase] PC 빌드 - Firestore 스킵, 로컬 데이터 사용");
        return;
#endif
        bool hasCloudData = await LoadUserDataFromFirestore();
        if (!hasCloudData)
        {
            await SaveUserDataToFirestore();
        }
    }

    // 현재 로그인 상태 텍스트 반환
    public string GetLoginStatusText()
    {
        if (currentUser == null) return DataManager.Instance.GetLocalizedText("UI_LOGIN_NONE");
        if (currentUser.IsAnonymous) return $"{DataManager.Instance.GetLocalizedText("UI_LOGIN_GUEST")}\nID: {currentUser.UserId[..8]}...";
        return $"{DataManager.Instance.GetLocalizedText("UI_LOGIN_GOOGLE")}\n{currentUser.Email}";
    }

    public async Task DeleteAndReset()
    {
        try
        {
            // 1. Firebase 익명 계정 삭제
            if (currentUser != null)
            {
                try
                {
                    await currentUser.DeleteAsync();
                    Debug.Log("[Firebase] 계정 삭제 완료!");
                }
                catch (System.Exception e)
                {
                    // 삭제 실패해도 그냥 진행
                    Debug.LogWarning($"[Firebase] 계정 삭제 실패 (무시하고 진행): {e.Message}");
                }
            }

            currentUser = null;

            // 2. 로컬 데이터 초기화
            PlayerPrefs.DeleteKey("SaveFile");
            PlayerPrefs.Save();

            Debug.Log("[Firebase] 계정 리셋 완료!");

            // DontDestroy 친구들 다 파괴
            RestartToTitle();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firebase] 리셋 실패: {e.Message}");
        }
    }

    // ── 타이틀 화면 가기 ──────────────────────────────────────────
    public void RestartToTitle()
    {
        Time.timeScale = 1f;
        currentUser = null;
        Instance = null;

        PlayerPrefs.DeleteKey("SaveFile");
        PlayerPrefs.Save();

        DataManager.Instance.ResetForNewSession();

        DOTween.KillAll();

        Destroy(gameObject);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void RestartToTitleForLanguage()
    {
        Time.timeScale = 1f;
        // SaveFile은 삭제하지 않음!

        DOTween.KillAll();

        Instance = null;
        Destroy(gameObject);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}