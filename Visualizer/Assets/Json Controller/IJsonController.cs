
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Json_Controller
{
    public interface IJsonController
    {
        public Dictionary<string, GameObject> LoadScene(object json);
    }
}