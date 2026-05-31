using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineRandomSpawner))]
public class SplineRandomSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        SplineRandomSpawner spawner =
            (SplineRandomSpawner)target;

        if (GUILayout.Button("生成"))
        {
            spawner.Generate();

            EditorUtility.SetDirty(spawner);
        }

        if (GUILayout.Button("清空"))
        {
            spawner.Clear();

            EditorUtility.SetDirty(spawner);
        }
    }
}