using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Application = UnityEngine.Application;

namespace Assets.EvaluationScripts
{
    public class Evaluation : MonoBehaviour
    {
        [SerializeField] public TextAsset SingleJsonFile;
        [SerializeField] public bool AllFilesInFolder;
        [SerializeField] public bool CalculateCFS;
        [SerializeField] public bool CalculateIBS;

        private readonly string _resultFolder = Path.Combine(Application.dataPath, "EvaluationJsons", "EvaluationResult");

        public void StartEvaluation()
        {
            if (AllFilesInFolder)
            {
                var(jsonFiles, txtFiles) = LoadAllJson();

                foreach (var json in jsonFiles)
                {
                    var baseName = json.name;
                    var matchingTxt = txtFiles.FirstOrDefault(t => t.name == baseName);

                    DoEvaluation(json, matchingTxt);
                }

                Directory.CreateDirectory(_resultFolder);
                string csvPathOld = Path.Combine(_resultFolder, $"evaluation_result_{DateTime.Now:yyyy-MM-dd-HH}.csv");
                string csvPathNew = Path.Combine(_resultFolder, $"evaluation_result_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.csv");

                File.Move(csvPathOld, csvPathNew);
            }
            else
            {
                DoEvaluation(SingleJsonFile, null);
            }
        }

        public void DoEvaluation(TextAsset json, TextAsset txt)
        {
            string fileName = json.name;

            float cfs = -1, cfsWithOutWallsFloor = -1, ibs = -1;
            float volumeCollisionImpactScore = -1;
            float normalizedCollisionImpactScore = -1;
            string error = string.Empty;
            List<MeshRenderer> allSceneElements;
            int totalElementsCount = 0;
            PipeLineInfo pipeLineInfo = null;

            try
            {
                Visualizer.Instance.DoVisualize(json.text);
                allSceneElements = GetAllSceneElements();
                totalElementsCount = allSceneElements.Count;

                if (CalculateCFS)
                {
                    cfs = CollisionFreeScore.CalculateCfs();
                    cfsWithOutWallsFloor = CollisionFreeScore.CalculateCfsWithoutFloorWalls();
                    volumeCollisionImpactScore = CollisionFreeScore.CalculateVolumeCollisionImpactScore();
                    normalizedCollisionImpactScore = CollisionFreeScore.CalculateNormalizedCollisionImpactScore();
                }
                if (CalculateIBS)
                {
                    JulangEnvironment environment = Visualizer.Instance.Converter.Environment;

                    ibs = InBoundaryScore.CalculateIbs(environment.Dimensions.Width, environment.Dimensions.Height, environment.Dimensions.Depth);
                }
            }
            catch (Exception)
            {
                error = "Error";
                Debug.LogWarning($"Couldnt visualize {json.name}");
            }

            try
            {
                pipeLineInfo = LogAnalyzer.AnalyzeLog(txt.text);
            }
            catch (Exception)
            {
                Debug.LogWarning($"PipeLineInfo couldn't be read! {txt.name}");
            }

            string resultFileName = $"evaluation_result_{DateTime.Now:yyyy-MM-dd-HH}.csv";
            Directory.CreateDirectory(_resultFolder);
            string csvPath = Path.Combine(_resultFolder, resultFileName);
            bool headerWritten = !File.Exists(csvPath);

            using (var sw = new StreamWriter(csvPath, append: true))
            {
                if (headerWritten)
                {
                    var headerFields = new List<string>()
                    {
                        "FileName",
                        "IBS",
                        "CFS",
                        "CFS without Floor and Walls",
                        "VCIS",
                        "NCIS",
                        "TotalElementsCount",
                        "Error",
                        "PipelineDurationSeconds"
                    };

                    for (int i = 0; i < pipeLineInfo?.Modules.Count; i++)
                    {
                        headerFields.Add($"Module{i + 1}_Name");
                        headerFields.Add($"Module{i + 1}_Duration_s");
                        headerFields.Add($"Module{i + 1}_PromptTokens");
                        headerFields.Add($"Module{i + 1}_CompletionTokens");
                        headerFields.Add($"Module{i + 1}_TotalTokens");
                    }

                    sw.WriteLine(string.Join(";", headerFields));
                }

                var rowFields = new List<string>()
                {
                    fileName,
                    ibs.ToString(CultureInfo.InvariantCulture),
                    cfs.ToString(CultureInfo.InvariantCulture),
                    cfsWithOutWallsFloor.ToString(CultureInfo.InvariantCulture),
                    volumeCollisionImpactScore.ToString(CultureInfo.InvariantCulture),
                    normalizedCollisionImpactScore.ToString(CultureInfo.InvariantCulture),
                    totalElementsCount.ToString(CultureInfo.InvariantCulture),
                    error,
                    (pipeLineInfo?.TotalDuration ?? 0d).ToString("F3", CultureInfo.InvariantCulture)
                };

                if (pipeLineInfo?.Modules != null)
                    foreach (var mod in pipeLineInfo.Modules)
                    {
                        rowFields.Add(mod.Name);
                        rowFields.Add(mod.Duration.ToString("F3", CultureInfo.InvariantCulture));
                        rowFields.Add(mod.PromptTokens.ToString());
                        rowFields.Add(mod.CompletionTokens.ToString());
                        rowFields.Add(mod.TotalTokens.ToString());
                    }

                sw.WriteLine(string.Join(";", rowFields));
            }

            AssetDatabase.Refresh();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            EditorApplication.QueuePlayerLoopUpdate();
            ClearSceneObjects();
        }

        private static (List<TextAsset> jsonFiles, List<TextAsset> txtFiles) LoadAllJson()
        {
            const string baseFolder = "Assets/EvaluationJsons";
            var jsonFiles = new List<TextAsset>();
            var txtFiles = new List<TextAsset>();

            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { baseFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path)?.Replace("\\", "/") != baseFolder)
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                string ext = Path.GetExtension(path).ToLowerInvariant();

                if (ext == ".json")
                    jsonFiles.Add(asset);
                else if (ext == ".txt")
                    txtFiles.Add(asset);
            }

            return (jsonFiles, txtFiles);
        }

        private List<MeshRenderer> GetAllSceneElements()
        {
            List<MeshRenderer> renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).ToList();

            return renderers;
        }

        private static void ClearSceneObjects()
        {
            var mrs = FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var mr in mrs)
            {
                if (!mr) continue;                                   // schon weg?
                if (mr.gameObject.scene != SceneManager.GetActiveScene()) continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(mr.gameObject);          // Edit-Mode
                else
#endif
                    Destroy(mr.gameObject);                   // Play-Mode
            }

#if UNITY_EDITOR
            // Szene als geändert markieren & Fenster refreshen
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }
}