#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class DebugWindow : EditorWindow
{
    [MenuItem("Debug/Open Debug Window")]
    static void Open() => GetWindow<DebugWindow>("Debug");

    void OnGUI()
    {
        GUILayout.Label("== 데이터 ==", EditorStyles.boldLabel);
        if (GUILayout.Button("SaveFile 삭제")) { PlayerPrefs.DeleteKey("SaveFile"); PlayerPrefs.Save(); }
        if (GUILayout.Button("LoginMethod 삭제")) { PlayerPrefs.DeleteKey("LoginMethod"); PlayerPrefs.Save(); }
        if (GUILayout.Button("전체 초기화")) { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); }

        EditorGUILayout.Space();
        GUILayout.Label("== 씬 이동 ==", EditorStyles.boldLabel);
        if (GUILayout.Button("타이틀로")) UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene
            = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
    }
}
#endif