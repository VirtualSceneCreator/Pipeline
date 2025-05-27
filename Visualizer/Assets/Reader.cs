using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Assets
{
    public class Reader
    {
        private readonly Dictionary<int, Type> _jsonVersions = new()
        {
            {1, typeof(Json_Files.SceneData1)},
            {2, typeof(Json_Files.SceneData2)}
        };


        public object ReadJson(string json, int version)
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Populate
            };

            if (_jsonVersions.TryGetValue(version, out var jsonType))
            {
                object readJson = JsonConvert.DeserializeObject(json, jsonType, settings);
                
                return readJson;
            }

            return null;
        }
    }
}