#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Assets.EvaluationScripts.Evaluation))]
public class EvaluationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Zeichnet alle �blichen Felder (Calculate CFS, Calculate IBS usw.)
        DrawDefaultInspector();
        GUILayout.Space(4);

        // Zus�tzlicher Button
        if (GUILayout.Button("Start", GUILayout.Height(25)))
        {
            // Ziel-Komponente holen und Methode aufrufen
            ((Assets.EvaluationScripts.Evaluation)target).StartEvaluation();
        }
    }
}
#endif