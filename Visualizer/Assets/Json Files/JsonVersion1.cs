using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.Json_Files
{
    [Serializable]
    public class SceneData1
    {
        [JsonProperty("scene")]
        public Scene Scene { get; set; }
    }

    [Serializable]
    public class Scene
    {
        [JsonProperty("sceneName")]
        public string SceneName { get; set; }

        [JsonProperty("environment")]
        public Environment Environment { get; set; }

        [JsonProperty("objects")]
        public List<TopLevelSceneObject> Objects { get; set; } = new List<TopLevelSceneObject>();
    }

    [Serializable]
    public class Environment
    {
        [JsonProperty("type")]
        public string Type { get; set; } // "indoor", "outdoor" oder "custom"

        [JsonProperty("dimensions")]
        public Dimensions3D Dimensions { get; set; }

        [JsonProperty("lighting", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<LightSource> Lighting { get; set; } = new List<LightSource>();

        [JsonProperty("background", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Background { get; set; }
    }

    [Serializable]
    public class Dimensions3D
    {
        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("depth")]
        public float Depth { get; set; }
    }

    [Serializable]
    public class LightSource
    {
        [JsonProperty("lightType")]
        public string LightType { get; set; } // "ambient", "directional", "point", "spot"

        [JsonProperty("intensity", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float? Intensity { get; set; }

        [JsonProperty("color", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Color { get; set; } // z. B. "#FFFFFF"

        [JsonProperty("range", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float? Range { get; set; }

        [JsonProperty("spotAngle", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float? SpotAngle { get; set; }
    }

    [Serializable]
    public class XyzCoordinates
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("z")]
        public float Z { get; set; }
    }

    [Serializable]
    public class RelativePositioning
    {
        [JsonProperty("referenceObject")]
        public string ReferenceObject { get; set; } // objectId des Referenzobjekts

        [JsonProperty("relation")]
        public string Relation { get; set; } // z. B. "in_front_of_positive_z", "on_top_of", etc.

        [JsonProperty("distance", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float Distance { get; set; } // Optional
    }

    [Serializable]
    public class TopLevelSceneObject
    {
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("assetName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AssetName { get; set; } // Optional

        [JsonProperty("position")]
        public XyzCoordinates Position { get; set; }

        [JsonProperty("rotation")]
        public XyzCoordinates Rotation { get; set; }

        [JsonProperty("dimensions")]
        public Dimensions3D Dimensions { get; set; }

        [JsonProperty("relativePositioning", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RelativePositioning RelativePositioning { get; set; }

        [JsonProperty("offset", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public XyzCoordinates Offset { get; set; }

        [JsonProperty("children", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ChildSceneObject> Children { get; set; } = new List<ChildSceneObject>(); // Default leere Liste
    }

    [Serializable]
    public class ChildSceneObject
    {
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("assetName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AssetName { get; set; } // Optional

        [JsonProperty("relativePositioning")]
        public RelativePositioning RelativePositioning { get; set; } // Pflicht

        [JsonProperty("rotation")]
        public XyzCoordinates Rotation { get; set; }

        [JsonProperty("dimensions")]
        public Dimensions3D Dimensions { get; set; }

        [JsonProperty("offset", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public XyzCoordinates Offset { get; set; } // Optional

        [JsonProperty("children", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ChildSceneObject> Children { get; set; } = new List<ChildSceneObject>(); // Default leere Liste
    }
}
