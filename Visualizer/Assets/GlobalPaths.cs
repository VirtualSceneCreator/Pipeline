
using System;
using System.IO;
using UnityEngine;

namespace Assets
{
    public static class GlobalPaths
    {
        private static string PathEnv => Environment.GetEnvironmentVariable("PATH");

        public static string PythonPath
        {
            get
            {
                if (!string.IsNullOrEmpty(PathEnv))
                {
                    string[] paths = PathEnv.Split(';');
                    foreach (string path in paths)
                    {
                        if (path.Contains("python", StringComparison.CurrentCultureIgnoreCase))
                        {
                            string fullPath = Path.Combine(path, "python.exe");
                            if (File.Exists(fullPath))
                            {
                                return fullPath;
                            }
                        }
                    }
                }

                Debug.LogWarning("GlobalPaths: Python Path nicht gefunden!");
                return string.Empty;
            }
        }

        public static string PipeLinePath
        {
            get
            {
                string pipeLinePath = Environment.GetEnvironmentVariable("PipeLinePath");

                if (!string.IsNullOrEmpty(pipeLinePath))
                {
                    return pipeLinePath;
                }

                Debug.LogWarning("GlobalPaths: PipeLine Path nicht gefunden!");
                return string.Empty;
            }
        }
    }
}