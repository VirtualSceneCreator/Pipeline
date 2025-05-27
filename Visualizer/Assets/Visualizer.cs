using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Assets
{
    public class Visualizer : MonoBehaviour
    {
        #region Singelton Pattern

        private static Visualizer _instance;

        public static Visualizer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<Visualizer>();

                    if (_instance == null)
                    {
                        GameObject instance = new GameObject("Visualizer");
                        _instance = instance.AddComponent<Visualizer>();

                        return Instance;
                    }
                }
                return _instance;
            }
        }

        public void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        [Tooltip("JSON Datei mit der Szenenbeschreibung")]
        public TextAsset JsonFile;

        [Tooltip("JSON Version")]
        public int JsonVersion;

        [Tooltip("Collision Color")]
        public Color CollisionColor = Color.magenta;

        [Tooltip("Should Check Collision?")]
        public bool ShouldCheckCollision;

        [Tooltip("Instand Load Json")]
        public bool InstantLoadJson;

        [Tooltip("Delete All")]
        public bool DeleteAll;

        [Tooltip("Validator Iterations")]
        public int ValidatorIterations = 1;

        [Tooltip("Scene Name")] 
        public string SceneName;

        [Tooltip("Input Image")] 
        public Texture2D Image;

        private Reader _reader = new Reader();
        private Writer _writer = new Writer();
        private Spawner _spawner = new Spawner();
        private ChatWindow _chatWindow;
        private object _json;
        private List<string> _jsons = new List<string>();
        private ReverseConverter _converter;

        public ReverseConverter Converter => _converter ??= _converter = new ReverseConverter();

        public void Start()
        {
            _chatWindow = new ChatWindow();
        }

        public void Update()
        {
            if (!InstantLoadJson)
            {
                return;
            }

            string folderPath = Path.Combine(Application.dataPath, "Json_Tests/GPT_Tests");

            if (!Directory.Exists(folderPath))
            {
                return;
            }

            List<string> currentJsons = Directory.GetFiles(folderPath, "*.json").ToList();
            List<string> newFiles = currentJsons
                .Where(current => _jsons.All(existing => existing != current))
                .ToList();

            if (!newFiles.Any())
            {
                return;
            }

            _jsons = currentJsons;

            string newestFile = newFiles
                .OrderByDescending(file => file)
                .First();

            DoVisualize(File.ReadAllText(newestFile));
        }

        [MenuItem("GameObject/Visualize %g")]
        public static void Visualize()
        {
            EditorUtility.DisplayDialog("Visualizer", "Starting to load objects...", "OK", "");

            Visualizer.Instance.DoVisualize();
        }

        [MenuItem("GameObject/Save Scene %r")]
        public static void SaveScene()
        {
            Visualizer.Instance.Converter.ScanForNewObjects();
            Visualizer.Instance._writer.WriteScene(Visualizer.Instance.Converter);
        }

        [MenuItem("GameObject/Delete Last Items %e")]
        public static void DeleteLastItems()
        {
            bool delete = EditorUtility.DisplayDialog("Visualizer", "Are u sure u want to delete the last spawned items?", "OK", "NO");

            if (delete)
            {
                Visualizer.Instance.Delete();
            }
        }

        [MenuItem("GameObject/Check Collision")]
        public static void CheckCollision()
        {
            List<MeshRenderer> allSceneElements = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).ToList();

            for (int i = 0; i < allSceneElements.Count; i++)
            {
                var ra = allSceneElements[i];
                if (!ra) continue;

                for (int j = i + 1; j < allSceneElements.Count; j++)
                {
                    var rb = allSceneElements[j];
                    if (!rb) continue;

                    if (BoundsPenetrate(ra.bounds, rb.bounds))
                    {
                        ra.sharedMaterial.color = rb.sharedMaterial.color =
                            Visualizer.Instance.CollisionColor;
                    }
                }
            }
        }

        [MenuItem("GameObject/Show UI")]
        public static void ShowWindow()
        {
            Instance._chatWindow = new ChatWindow();
            Instance._chatWindow.ShowWindow();
        }

        public void DoVisualize()
        {
            DoVisualize(JsonFile.text);
        }

        public void DoVisualize(string fileName)
        {
            ReadJsonFile(fileName);
            SpawnObjects();

            if (ShouldCheckCollision)
            {
                CheckCollision();
            }
        }

        public void Delete()
        {
            Delete(DeleteAll);
        }

        public void Delete(bool deleteAll)
        {
            Visualizer.Instance.Converter.GameObjects.Clear();
            if (DeleteAll)
            {
                MeshRenderer[] meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                EditorApplication.delayCall += () =>
                {
                    foreach (MeshRenderer mr in meshRenderers)
                    {
                        if (mr) DestroyImmediate(mr.gameObject);
                    }
                };

                return;
            }

            foreach (KeyValuePair<string, GameObject> spawnedObject in _spawner.LastSpawnedObjects)
            {
                DestroyImmediate(spawnedObject.Value);
            }
        }

        private void ReadJsonFile(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                if (json.StartsWith("\"") && json.EndsWith("\""))
                {
                    json = json.Substring(1, json.Length - 2);
                }

                string cleanJson = Regex.Unescape(json);

                object readJson = _reader.ReadJson(cleanJson, JsonVersion);

                if (readJson != null)
                {
                    _json = readJson;
                }
            }
            catch (Exception)
            {
                Debug.LogWarning("ERROR/n/nLoading json was not successfully!");
            }
        }

        private void SpawnObjects()
        {
            _spawner.SpawnObjects(_json, JsonVersion);
        }

        private static bool BoundsPenetrate(in Bounds a, in Bounds b, float eps = 1e-5f)
        {
            return (a.min.x + eps < b.max.x && a.max.x - eps > b.min.x) &&
                   (a.min.y + eps < b.max.y && a.max.y - eps > b.min.y) &&
                   (a.min.z + eps < b.max.z && a.max.z - eps > b.min.z);
        }
    }
}
