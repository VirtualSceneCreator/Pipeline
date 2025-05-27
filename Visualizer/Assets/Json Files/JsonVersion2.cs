using System;
using System.Collections.Generic;
using Assets.Json_Controller;
using Newtonsoft.Json;

namespace Assets.Json_Files
{
    [Serializable]
    public class SceneData2
    {
        [JsonProperty("scene")]
        public Scene2 Scene { get; set; }
    }

    [Serializable]
    public class Scene2
    {
        [JsonProperty("sceneName")]
        public string SceneName { get; set; }

        [JsonProperty("environment")]
        public Environment2 Environment { get; set; }

        [JsonProperty("objects")]
        public List<TopLevelSceneObject2> Objects { get; set; } = new List<TopLevelSceneObject2>();

        [JsonProperty("objectGroups", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, ObjectGroup2> ObjectGroups { get; set; } = new Dictionary<string, ObjectGroup2>();
    }

    [Serializable]
    public class ObjectGroup2
    {
        [JsonProperty("color")]
        public string Color { get; set; }
    }

    [Serializable]
    public class Environment2
    {
        [JsonProperty("type")]
        public string Type { get; set; } // "indoor", "outdoor" oder "custom"

        [JsonProperty("dimensions")]
        public Dimensions3D2 Dimensions { get; set; }

        [JsonProperty("lighting", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<LightSource2> Lighting { get; set; } = new List<LightSource2>();

        [JsonProperty("background", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Background { get; set; }
    }

    [Serializable]
    public class Dimensions3D2
    {
        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("depth")]
        public float Depth { get; set; }
    }

    [Serializable]
    public class LightSource2
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
    public class XyzCoordinates2
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("z")]
        public float Z { get; set; }
    }

    [Serializable]
    public class RelativePositioning2
    {
        [JsonProperty("referenceObject")]
        public string ReferenceObject { get; set; } // objectId des Referenzobjekts

        [JsonProperty("relation")]
        public string Relation { get; set; } // z. B. "in_front_of_positive_z", "on_top_of", etc.

        [JsonProperty("distance", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float Distance { get; set; } // Optional
    }

    [Serializable]
    public class TopLevelSceneObject2 : ISceneObject
    {
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("assetName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AssetName { get; set; } // Optional

        [JsonProperty("position")]
        public XyzCoordinates2 Position { get; set; }

        [JsonProperty("rotation")]
        public XyzCoordinates2 Rotation { get; set; }

        [JsonProperty("dimensions")]
        public Dimensions3D2 Dimensions { get; set; }

        [JsonProperty("relativePositioning", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RelativePositioning2 RelativePositioning { get; set; }

        [JsonProperty("offset", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public XyzCoordinates2 Offset { get; set; }

        [JsonProperty("children", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ChildSceneObject2> Children { get; set; } = new List<ChildSceneObject2>(); // Default leere Liste

        [JsonProperty("group", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Group { get; set; } // Neu: Zugeordnete Objektgruppe

        [JsonProperty("specification", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Specification { get; set; }
    }

    [Serializable]
    public class ChildSceneObject2 : ISceneObject
    {
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("assetName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AssetName { get; set; } // Optional

        [JsonProperty("relativePositioning")]
        public RelativePositioning2 RelativePositioning { get; set; } // Pflicht

        [JsonProperty("rotation")]
        public XyzCoordinates2 Rotation { get; set; }

        [JsonProperty("dimensions")]
        public Dimensions3D2 Dimensions { get; set; }

        [JsonProperty("offset", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public XyzCoordinates2 Offset { get; set; } // Optional

        [JsonProperty("children", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ChildSceneObject2> Children { get; set; } = new List<ChildSceneObject2>(); // Default leere Liste

        [JsonProperty("group", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Group { get; set; } // Neu: Zugeordnete Objektgruppe

        [JsonProperty("specification", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Specification { get; set; }
    }
}
