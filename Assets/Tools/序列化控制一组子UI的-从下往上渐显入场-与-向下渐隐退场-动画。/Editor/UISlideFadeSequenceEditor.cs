#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UISlideFadeSequence))]
public class UISlideFadeSequenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var comp = (UISlideFadeSequence)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("测试", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("播放入场"))
            {
                comp.PlayEnter();
            }
            if (GUILayout.Button("播放退场"))
            {
                comp.PlayExit();
            }
        }
        
        if (GUILayout.Button("立即设为退场状态"))
        {
            comp.SetExitStateImmediate();
        }

        if (GUILayout.Button("从子物体填充 Items"))
        {
            comp.PopulateItemsFromChildren();
            EditorUtility.SetDirty(comp);
        }

        if (GUILayout.Button("手动从容器同步目标位置"))
        {
            comp.SyncTargetsFromContainer();
            EditorUtility.SetDirty(comp);
        }
    }
}

#endif