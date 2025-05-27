using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Assets
{
    public class ChatWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _currentText;
        private List<ChatMessage> _messages = new List<ChatMessage>();
        private Process _pipeLineProcess;
        private Texture2D _droppedImage;

        public void ShowWindow()
        {
            GetWindow<ChatWindow>("Chat");
            StartPythonProcess();
            _messages.Add(new ChatMessage("Start the chat (type 'exit' to quit)", "System: "));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // Drag & Drop Bereich
            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop your image here");
            HandleDragAndDrop(dropArea);

            // Anzeige des Bildes
            if (_droppedImage != null)
            {
                GUILayout.Label(_droppedImage, GUILayout.Height(100), GUILayout.Width(100));
            }

            // Chat-Verlauf
            GUILayout.Label("Chat-History:", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            foreach (var message in _messages)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(message.User, GUILayout.Width(60));
                GUILayout.Label(message.Text, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            // Texteingabe und Senden
            GUILayout.BeginHorizontal();
            _currentText = EditorGUILayout.TextField(_currentText);
            if (GUILayout.Button("Send", GUILayout.Width(80)))
            {
                string toSentToPipeline = _currentText;
                _currentText = string.Empty;
                GUIUtility.keyboardControl = 0;
                Repaint();
                SenMessageToPipeLine(toSentToPipeline);
            }
            GUILayout.EndHorizontal();

            // Steuer-Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                _messages.Clear();
                Repaint();
            }
            if (GUILayout.Button("Restart", GUILayout.Width(80)))
            {
                OnApplicationQuit();
                StartPythonProcess();
            }
            if (GUILayout.Button("Exit", GUILayout.Width(80)))
            {
                Exit();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void SenMessageToPipeLine(string toSentToPipeline)
        {
            if (string.IsNullOrEmpty(toSentToPipeline))
            {
                return;
            }
            
            _messages.Add(new ChatMessage(text: toSentToPipeline, user: "User: "));
            
            Repaint();

            GetPipelineAnswer(toSentToPipeline);
            
            _scrollPos.y = float.MaxValue;
            Repaint();
        }

        private void GetPipelineAnswer(string question)
        {
            if (_pipeLineProcess != null && !_pipeLineProcess.HasExited)
            {
                _pipeLineProcess.StandardInput.WriteLine(question);
                _pipeLineProcess.StandardInput.Flush();
            }
        }

        private void StartPythonProcess()
        {
            int maxValidatorIterations = Visualizer.Instance.ValidatorIterations;
            string imagePath = string.Empty;

            if (_droppedImage != null)
            {
                imagePath = AssetDatabase.GetAssetPath(_droppedImage);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = GlobalPaths.PythonPath; // Oder den vollständigen Pfad zu python.exe
            startInfo.WorkingDirectory = GlobalPaths.PipeLinePath;
            startInfo.Arguments = string.IsNullOrEmpty(imagePath) ? 
                $"main.py {maxValidatorIterations}" 
                : $"main.py {maxValidatorIterations} \"{imagePath}\"";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = false;
            startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

            _pipeLineProcess = new Process();
            _pipeLineProcess.StartInfo = startInfo;

            _pipeLineProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.LogWarning("Python: " + e.Data);
                    // Aktualisierung im Hauptthread über EditorApplication.delayCall
                    EditorApplication.delayCall += () =>
                    {
                        _messages.Add(new ChatMessage(e.Data, "Assistant:"));
                        _scrollPos.y = float.MaxValue;
                        _currentText = "";
                        Repaint();
                    };
                }
            };

            _pipeLineProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.LogWarning("Python Error: " + e.Data);
                }
            };

            _pipeLineProcess.Start();
            _pipeLineProcess.BeginOutputReadLine();
            _pipeLineProcess.BeginErrorReadLine();
        }

        private void OnApplicationQuit()
        {
            GetPipelineAnswer("exit");

            if (_pipeLineProcess != null && !_pipeLineProcess.HasExited)
            {
                _pipeLineProcess.WaitForExit(1000);
                if (!_pipeLineProcess.HasExited)
                {
                    _pipeLineProcess.Kill();
                }
            }
            _messages.Clear();
            Repaint();
        }

        private void Exit()
        {
            OnApplicationQuit();
            this.Close();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (DragAndDrop.objectReferences.Any(o => o is Texture2D))
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    else
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is Texture2D tex)
                            {
                                _droppedImage = tex;
                                Repaint();

                                string relativePath = AssetDatabase.GetAssetPath(_droppedImage);
                                string fullPath = Path.GetFullPath(relativePath);
                                Debug.Log($"[ChatWindow] Gedroppter Asset-Pfad: {fullPath}");

                                if (_pipeLineProcess != null && !_pipeLineProcess.HasExited)
                                {
                                    _pipeLineProcess.StandardInput.WriteLine($"Image_flush:{fullPath}");
                                    _pipeLineProcess.StandardInput.Flush();
                                }
                                else
                                {
                                    Debug.LogWarning("[ChatWindow] Python-Prozess nicht verfügbar.");
                                }

                                break;
                            }
                        }
                    }
                    evt.Use();
                    break;
            }
        }

    }
}
