
using System.Collections.Generic;
using System.Linq;
using Assets.Json_Controller;
using Assets.Json_Files;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets
{
    public class ReverseConverter : ScriptableObject
    {
        private List<JulangGameObject> _gameObjects;

        // Julang Elements
        public string SceneName;
        public JulangEnvironment Environment;
        public List<JulangGameObject> GameObjects => _gameObjects ??= new List<JulangGameObject>();
        public Dictionary<string, string> ObjectGroups;

        public void InitializeJulang(Scene2 scene)
        {
            this.SceneName = scene.SceneName;
            this.Environment = new JulangEnvironment(scene.Environment);
            this.ObjectGroups = new Dictionary<string, string>();
            foreach (KeyValuePair<string, ObjectGroup2> group in scene.ObjectGroups)
            {
                this.ObjectGroups.Add(group.Key, group.Value.Color);
            }
        }

        public GameObject InstantiateObj(GameObject obj, ISceneObject sceneObject)
        {
            JulangGameObject julangGameObject = ScriptableObject.CreateInstance<JulangGameObject>();
            julangGameObject.Instantiate(sceneObject);

            GameObject gameObject = Instantiate(obj);
            julangGameObject.GameObject = gameObject;

            GameObjects.Add(julangGameObject);

            return julangGameObject.GameObject;
        }

        public GameObject InstantiateObj(GameObject obj, ISceneObject sceneObject, Transform transform)
        {
            JulangGameObject julangGameObject = ScriptableObject.CreateInstance<JulangGameObject>();
            julangGameObject.Instantiate(sceneObject);

            GameObject gameObject = Instantiate(obj, transform);
            julangGameObject.GameObject = gameObject;

            GameObjects.Add(julangGameObject);

            return julangGameObject.GameObject;
        }

        public void ScanForNewObjects()
        {
            List<Object> renderers = Object.FindObjectsByType(typeof(MeshRenderer), FindObjectsSortMode.None).ToList();
            List<GameObject> allGameObjects = renderers.Select(r => r.GameObject()).ToList();

            List<GameObject> julanGameObjects = GameObjects.Select(julang => julang.GameObject).ToList();

            foreach (GameObject gameObject in allGameObjects)
            {
                if (!julanGameObjects.Contains(gameObject))
                {
                    JulangGameObject newAddedGameObject = ScriptableObject.CreateInstance<JulangGameObject>();
                    newAddedGameObject.Instantiate(gameObject.name, gameObject.name,
                        gameObject.name, string.Empty, string.Empty);

                    newAddedGameObject.GameObject = gameObject;
                    GameObjects.Add(newAddedGameObject);
                }
            }
        }
    }
}
