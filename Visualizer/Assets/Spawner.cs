using System;
using System.Collections.Generic;
using Assets.Json_Controller;
using UnityEngine;

namespace Assets
{
    public class Spawner
    {
        public Dictionary<string, GameObject> LastSpawnedObjects = new Dictionary<string, GameObject>();

        private readonly Dictionary<int, Type> _jsonVersions = new()
        {
            //{1, typeof(JsonController1)},
            {2, typeof(JsonController2)},
        };

        public void SpawnObjects(object json, int version)
        {
            if (_jsonVersions.TryGetValue(version, out var jsonType))
            {
                if (Activator.CreateInstance(jsonType) is IJsonController controller)
                {
                    LastSpawnedObjects = controller.LoadScene(json);
                }
            }
        }
    }
}