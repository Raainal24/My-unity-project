using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FarmhouseBuilder))]
public class FarmhouseBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);

        FarmhouseBuilder builder =
            (FarmhouseBuilder)target;

        if (GUILayout.Button("BUILD FARMHOUSE PLOT", GUILayout.Height(40)))
        {
            builder.BuildPlot();

            EditorUtility.SetDirty(builder);
        }
    }
}