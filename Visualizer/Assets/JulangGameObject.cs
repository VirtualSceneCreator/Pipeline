
using Assets.Json_Controller;
using UnityEngine;

namespace Assets
{
    public class JulangGameObject : ScriptableObject
    {
        public string Specification;
        public string ObjectId;
        public string ObjectType;
        public string AssetName;
        public string Group;
        public GameObject GameObject;

        public void Instantiate(ISceneObject sceneObject)
        {
            this.Specification = sceneObject.Specification;
            this.ObjectId = sceneObject.ObjectId;
            this.ObjectType = sceneObject.ObjectType;
            this.AssetName = sceneObject.AssetName;
            this.Group = sceneObject.Group;
        }

        public void Instantiate(string specification, string objectId, string objectType, string assetName, string group)
        {
            this.Specification = specification;
            this.ObjectId = objectId;
            this.ObjectType = objectType;
            this.AssetName = assetName;
            this.Group = group;
        }
    }
}