//  Assets/Editor/DevDescriptionImporter.cs
//  v2.4 – Stabile Progress‑Bar + ETA‑Logs
//  17 Apr 2025
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class DevDescriptionImporter : EditorWindow
{
    // ───────── User‑Settings ────────────────────────────────────
    private const string ApiUrl        = "http://127.0.0.1:5000/generate";
    private const string ImageBackend  = "local"; // oder "dalle3"
    private const string NegativeLocal =
        "blurry areas, complex background, strange reflections, odd geometry, distortion, harsh shadows";
    private const string TargetFolder  = "Assets/GeneratedModels";

    // ───────── intern ───────────────────────────────────────────
    private class Group
    {
        public string key;
        public string description;
        public Vector3 size;
        public List<DevDescription> items = new();
        public bool reuse = true;
    }

    private readonly List<Group> _groups = new();
    private Vector2  _scroll;

    // Progress‑Info
    private bool     _isGenerating;
    private bool     _cancelRequested;
    private bool     _showProgress;
    private int      _totalCalls;
    private int      _completed;
    private string   _currentName = "";
    private double   _remainSeconds;
    private DateTime _startTime;

    // Main‑Thread‑Repaint bündeln
    private bool _repaintPending;

    // Caching für Styles
    private static GUIStyle _doneLabelStyle;

    /*───────────────────────────────────────────────────────────
     *  MENU
     *──────────────────────────────────────────────────────────*/
    [MenuItem("GameObject/Dev Tools/Replace Placeholders with GLB...", false, 50)]
    private static void Open()
    {
        var w = GetWindow<DevDescriptionImporter>("GLB Import Options");
        w.minSize = new Vector2(540, 420);
        w.CollectGroups();
    }

    /*───────────────────────────────────────────────────────────
     *  UTILS – Main‑Thread‑Aktionen
     *──────────────────────────────────────────────────────────*/
    private void RequestRepaint()
    {
        if (_repaintPending) return;
        _repaintPending = true;
        EditorApplication.delayCall += () =>
        {
            _repaintPending = false;
            if (this) Repaint();
        };
    }

    private void LogProgressText(string prefix = "[GLB]")
    {
        Debug.Log($"{prefix} {BuildProgressText()}");
    }

    private string BuildProgressText()
    {
        string eta = _completed == 0
            ? "—"
            : TimeSpan.FromSeconds(_remainSeconds).ToString("mm\\:ss");
        return $"{_completed}/{_totalCalls}  –  ETA {eta}  –  {_currentName}";
    }

    /*───────────────────────────────────────────────────────────
     *  GROUP COLLECTION
     *──────────────────────────────────────────────────────────*/
    private void CollectGroups()
    {
        _groups.Clear();
        DevDescription[] all = UnityEngine.Object.FindObjectsOfType<DevDescription>(true);
        if (all.Length == 0)
        {
            ShowNotification(new GUIContent("Keine DevDescription‑Objekte gefunden."));
            return;
        }

        foreach (DevDescription dd in all)
        {
            string desc = string.IsNullOrWhiteSpace(dd.Description) ? "Unnamed object" : dd.Description.Trim();
            Vector3 size = RoundVec(dd.transform.lossyScale, 2);

            string key = $"{desc}__{size.x}_{size.y}_{size.z}";
            Group g = _groups.FirstOrDefault(x => x.key == key);
            if (g == null)
            {
                g = new Group { key = key, description = desc, size = size };
                _groups.Add(g);
            }
            g.items.Add(dd);
        }
    }

    private static Vector3 RoundVec(Vector3 v, int d)
    {
        float f = Mathf.Pow(10, d);
        return new Vector3(Mathf.Round(v.x * f) / f,
                           Mathf.Round(v.y * f) / f,
                           Mathf.Round(v.z * f) / f);
    }

    /*───────────────────────────────────────────────────────────
     *  UI
     *──────────────────────────────────────────────────────────*/
    private void OnGUI()
    {
        if (_groups.Count == 0)
        {
            EditorGUILayout.HelpBox("Keine DevDescription‑Objekte in der Szene.", MessageType.Info);
            if (GUILayout.Button("Neu suchen")) CollectGroups();
            return;
        }

        using (new EditorGUI.DisabledScope(_isGenerating))
        {
            DrawGroupList();
        }

        // Eigene Progress‑Bar
        if (_showProgress)
        {
            DrawProgressBar();
        }

        GUILayout.FlexibleSpace();
        DrawBottomButtons();
    }

    private void DrawGroupList()
    {
        if (_doneLabelStyle == null)
        {
            _doneLabelStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
        }

        EditorGUILayout.LabelField("Duplikat‑Gruppen", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (Group g in _groups)
        {
            bool isDone = IsGroupCompleted(g);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            if (g.items.Count > 1)
                g.reuse = EditorGUILayout.Toggle(g.reuse, GUILayout.Width(20));
            else
                GUILayout.Space(24);

            string label = $"{g.description}  •  {g.size.x:F2}×{g.size.z:F2}×{g.size.y:F2} m  •  {g.items.Count}×";
            EditorGUILayout.LabelField(label, isDone ? _doneLabelStyle : EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            string note = g.items.Count == 1
                ? "(Einzelobjekt – immer individuell)"
                : g.reuse ? "Gemeinsames Modell für alle" : "Individuelle Modelle";
            EditorGUILayout.LabelField(note, isDone ? _doneLabelStyle : EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawProgressBar()
    {
        float progress = _totalCalls == 0 ? 0f : _completed / (float)_totalCalls;
        Rect r = GUILayoutUtility.GetRect(18, 22, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(r, progress, BuildProgressText());
        GUILayout.Space(4);
    }

    private void DrawBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();
        if (!_isGenerating)
        {
            if (GUILayout.Button("Start Generierung", GUILayout.Height(30)))
            {
                _ = StartGeneration();
            }
            if (GUILayout.Button("Schließen", GUILayout.Height(30)))
            {
                Close();
            }
        }
        else
        {
            if (GUILayout.Button("Vorgang abbrechen", GUILayout.Height(30)))
            {
                _cancelRequested = true;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    /*───────────────────────────────────────────────────────────
     *  GENERATION
     *──────────────────────────────────────────────────────────*/
    private async Task StartGeneration()
    {
        _isGenerating    = true;
        _cancelRequested = false;
        _showProgress    = true;
        _completed       = 0;
        _startTime       = DateTime.Now;
        _currentName     = "";

        if (!Directory.Exists(TargetFolder)) Directory.CreateDirectory(TargetFolder);

        _totalCalls = _groups.Sum(g => g.reuse ? 1 : g.items.Count);
        LogProgressText("[GLB] Starte Generierung –");

        RequestRepaint();

        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(20) };

            foreach (Group g in _groups)
            {
                if (_cancelRequested) break;

                int callsInGroup = g.reuse ? 1 : g.items.Count;
                string prefabPath = null;

                for (int i = 0; i < callsInGroup; i++)
                {
                    if (_cancelRequested) break;

                    DevDescription dd = g.items[i];
                    Transform ph     = dd.transform;
                    Vector3   size   = ph.lossyScale;

                    string desc   = string.IsNullOrWhiteSpace(dd.Description) ? "Unnamed object" : dd.Description.Trim();
                    string prompt = $"{desc}. Dimensions {size.x:F2} x {size.z:F2} x {size.y:F2} meters.";

                    var payload = new GeneratePayload
                    {
                        image_backend   = ImageBackend,
                        positive_prompt = prompt,
                        negative_prompt = ImageBackend == "local" ? NegativeLocal : null
                    };
                    string json = JsonUtility.ToJson(payload);

                    // Debug vor dem Call
                    Debug.Log($"[GLB] {_completed + 1}/{_totalCalls} – REST‑Call für '{ph.name}'");

                    HttpResponseMessage resp = await client.PostAsync(ApiUrl,
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    if (!resp.IsSuccessStatusCode)
                    {
                        Debug.LogError($"REST‑Fehler {resp.StatusCode} – {await resp.Content.ReadAsStringAsync()}");
                        continue;
                    }

                    byte[] data = await resp.Content.ReadAsByteArrayAsync();
                    string file = $"{Sanitize(ph.name)}_{Guid.NewGuid():N}.glb";
                    string rel  = $"{TargetFolder}/{file}";
                    string abs  = Path.Combine(Environment.CurrentDirectory, rel);

                    File.WriteAllBytes(abs, data);
                    AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceSynchronousImport);
                    prefabPath ??= rel;
                    if (g.reuse) break;

                    MarkStepFinished(ph.name);
                    if (_cancelRequested) break;
                }

                if (_cancelRequested) break;
                if (prefabPath == null) continue;

                GameObject prefab = LoadPrefab(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"GLB konnte nicht geladen werden: {prefabPath}");
                    continue;
                }

                foreach (DevDescription dd in g.items)
                {
                    Transform ph = dd.transform;
                    if (ph.Find("GLBInstance")) continue;

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ph.parent);
                    inst.name = "GLBInstance";
                    inst.transform.SetPositionAndRotation(ph.position, ph.rotation);

                    Vector3 factor = GetScaleFactor(inst, ph.lossyScale);
                    inst.transform.localScale = factor;

                    ph.gameObject.SetActive(false);
                    AppendDimensionsIfMissing(dd, ph.lossyScale);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            _isGenerating = false;
            _showProgress = false;
            RequestRepaint();
            Debug.Log(_cancelRequested ? "[GLB] Vorgang abgebrochen" : "[GLB] Fertig!");
        }
    }

    private void MarkStepFinished(string objName)
    {
        // Fortschritts‑Berechnung thread‑safe vorbereiten
        int newCompleted = _completed + 1;
        double elapsed   = (DateTime.Now - _startTime).TotalSeconds;
        double remain    = newCompleted > 0 ? elapsed / newCompleted * (_totalCalls - newCompleted) : 0d;

        // Aktion im Main‑Thread einplanen
        EditorApplication.delayCall += () =>
        {
            _completed      = newCompleted;
            _remainSeconds  = remain;
            _currentName    = objName;

            LogProgressText();   // exakte Progress‑Bar‑Zeile ausgeben
            RequestRepaint();    // GUI neu zeichnen
        };
    }

    /*───────────────────────────────────────────────────────────
     *  HELPERS
     *──────────────────────────────────────────────────────────*/
    [Serializable] private class GeneratePayload
    {
        public string image_backend;
        public string positive_prompt;
        public string negative_prompt;
    }

    private static string Sanitize(string n)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
        return n;
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go != null) return go;

        UnityEngine.Object[] reps = AssetDatabase.LoadAllAssetsAtPath(path);
        return reps.OfType<GameObject>().FirstOrDefault();
    }

    private static Vector3 GetScaleFactor(GameObject inst, Vector3 target)
    {
        Bounds b = CalculateBounds(inst);
        return b.size == Vector3.zero ? Vector3.one
            : new Vector3(target.x / b.size.x, target.y / b.size.y, target.z / b.size.z);
    }

    private static Bounds CalculateBounds(GameObject go)
    {
        Renderer[] r = go.GetComponentsInChildren<Renderer>();
        if (r.Length == 0) return new Bounds();
        Bounds b = r[0].bounds;
        for (int i = 1; i < r.Length; i++) b.Encapsulate(r[i].bounds);
        return b;
    }

    private static void AppendDimensionsIfMissing(DevDescription dd, Vector3 s)
    {
        if (dd.Description != null && dd.Description.Contains("[Dim:")) return;
        var so = new SerializedObject(dd);
        so.FindProperty("description").stringValue =
            $"{dd.Description} [Dim: {s.x:F2}×{s.z:F2}×{s.y:F2}]".Trim();
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dd);
    }

    private static bool IsGroupCompleted(Group g)
        => g.items.All(dd => dd.transform.Find("GLBInstance") != null);
}
#endif
