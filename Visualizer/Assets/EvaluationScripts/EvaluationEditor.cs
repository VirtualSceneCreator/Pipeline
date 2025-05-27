#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Assets.EvaluationScripts.Evaluation))]
public class EvaluationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Zeichnet alle üblichen Felder (Calculate CFS, Calculate IBS usw.)
        DrawDefaultInspector();
        GUILayout.Space(4);

        // Zusätzlicher Button
        if (GUILayout.Button("Start", GUILayout.Height(25)))
        {
            // Ziel-Komponente holen und Methode aufrufen
            ((Assets.EvaluationScripts.Evaluation)target).StartEvaluation();
        }
    }
}
#endif