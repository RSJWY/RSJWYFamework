#if UNITY_EDITOR
using RSJWYFamework.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;

namespace RSJWYFamework.Editor
{
    /// <summary>
    /// 圆角UI
    /// https://blog.csdn.net/q1242416158/article/details/134735046
    /// </summary>
    [CustomEditor(typeof(UIRoundConor_RawImage), true)]
    public class UIRoundConor_RawImageEditor : RawImageEditor
    {
        SerializedProperty isLockCorner;
        SerializedProperty corner4;
        SerializedProperty corner;
        SerializedProperty borderWidth;
        SerializedProperty borderColor;
        SerializedProperty centerColor;
        protected override void OnEnable()
        {
            base.OnEnable();
            isLockCorner= serializedObject.FindProperty("m_IsLockCorner");
            corner4 = serializedObject.FindProperty("m_Corner4");
            corner = serializedObject.FindProperty("m_Corner");
            borderWidth = serializedObject.FindProperty("m_BorderWidth");
            borderColor = serializedObject.FindProperty("m_BorderColor");
            centerColor = serializedObject.FindProperty("m_CenterColor");
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
 
            EditorGUILayout.Space();
            isLockCorner.boolValue= EditorGUILayout.Toggle(new GUIContent("圆角相同大小", "设置所有角的大小是否一样"),isLockCorner.boolValue);
            if (isLockCorner.boolValue)
            {
                corner.floatValue=  EditorGUILayout.FloatField(new GUIContent("圆角大小", "设置所有角的大小"),corner.floatValue);
            }
            else
            {
            
                var cornerValue = corner4.vector4Value;
 
                var cacheLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 70;
                EditorGUILayout.BeginHorizontal();
                cornerValue.x = EditorGUILayout.FloatField("左上角", cornerValue.x);
                EditorGUILayout.Space();
                cornerValue.y = EditorGUILayout.FloatField("右上角", cornerValue.y);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
 
                EditorGUILayout.BeginHorizontal();
                cornerValue.w = EditorGUILayout.FloatField("左下角", cornerValue.w);
                EditorGUILayout.Space();
                cornerValue.z = EditorGUILayout.FloatField("右下角", cornerValue.z);
                EditorGUILayout.EndHorizontal();
 
                corner4.vector4Value = cornerValue;
 
                EditorGUIUtility.labelWidth = cacheLabelWidth;
            }
            centerColor.colorValue= EditorGUILayout.ColorField(new GUIContent("中心颜色"),  centerColor.colorValue);
            borderWidth.floatValue=  EditorGUILayout.FloatField(new GUIContent("描边宽度"),  borderWidth.floatValue);
            borderColor.colorValue= EditorGUILayout.ColorField(new GUIContent("描边颜色"),  borderColor.colorValue);
            serializedObject.ApplyModifiedProperties();
        }
        [MenuItem("GameObject/UI/RoundCornor Raw Image", false, 2002)]//MenuOptionsPriorityOrder.RawImage
        public static void CreateGameObject()
        {
            var parent = GetCanvasGameObject();
            if (parent!=null)
            {
                var go = new GameObject("UIRoundConor_RawImage");
                go.transform.SetParent(parent.transform, false);
                go.AddComponent<RectTransform>();
                go.AddComponent<CanvasRenderer>();
                go.AddComponent<UIRoundConor_RawImage >();
                Selection.activeGameObject = go;
            }
            else
            {
                EditorUtility.DisplayDialog("UIRoundConor_RawImage",
                    "请选择一个画布下的对象", "Ok");
            }
        }
        public static GameObject GetCanvasGameObject()
        {
            GameObject selectedGo = Selection.activeGameObject;
 
            Canvas canvas = (selectedGo != null) ? selectedGo.GetComponentInParent<Canvas>() : null;
            if (IsValidCanvas(canvas))
                return selectedGo.gameObject;
 
            Canvas[] canvasArray = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>();
            for (int i = 0; i < canvasArray.Length; i++)
                if (IsValidCanvas(canvasArray[i]))
                    return canvasArray[i].gameObject;
 
            return null;
        }
        static bool IsValidCanvas(Canvas canvas)
        {
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
                return false;
 
            if (EditorUtility.IsPersistent(canvas) || (canvas.hideFlags & HideFlags.HideInHierarchy) != 0)
                return false;
 
            return StageUtility.GetStageHandle(canvas.gameObject) == StageUtility.GetCurrentStageHandle();
        }
    }
}
#endif