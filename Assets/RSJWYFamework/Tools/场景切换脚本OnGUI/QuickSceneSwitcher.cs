using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 快速测试专用的场景切换器。
/// 自动获取 Build Settings 中的场景并生成列表。
/// </summary>
public class QuickSceneSwitcher : MonoBehaviour
{
    private static QuickSceneSwitcher _instance;

    // 是否显示UI面板的开关
    private bool _showUI = true;
    private Vector2 _scrollPosition;

    void Awake()
    {
        // 确保脚本在切换场景时不会被销毁，保持面板常驻
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 默认按 F1 键可以隐藏/显示这个测试面板，避免遮挡你的项目画面
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _showUI = !_showUI;
        }
    }

    void OnGUI()
    {
        if (!_showUI) return;

        // 设置面板的基础样式和位置
        GUILayout.BeginArea(new Rect(10, 10, 200, Screen.height - 20));
        
        // 面板背景色 (半透明黑)
        GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(0, 0, 200, Screen.height - 20), "");
        GUI.backgroundColor = Color.white;

        GUILayout.Label(" 快速场景切换 (按F1隐藏)", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Space(10);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        int sceneCount = SceneManager.sceneCountInBuildSettings;

        if (sceneCount == 0)
        {
            GUILayout.Label("请先将场景添加到\nBuild Settings中！", new GUIStyle(GUI.skin.label) { normal = new GUIStyleState() { textColor = Color.red } });
        }
        else
        {
            for (int i = 0; i < sceneCount; i++)
            {
                // 获取场景路径并提取场景名称
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                // 高亮当前所在的场景
                bool isCurrentScene = SceneManager.GetActiveScene().buildIndex == i;
                GUI.color = isCurrentScene ? Color.green : Color.white;

                if (GUILayout.Button(isCurrentScene ? $"[当前] {sceneName}" : sceneName, GUILayout.Height(35)))
                {
                    if (!isCurrentScene)
                    {
                        SceneManager.LoadScene(i);
                    }
                }
                GUI.color = Color.white; // 恢复颜色
                GUILayout.Space(5);
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}