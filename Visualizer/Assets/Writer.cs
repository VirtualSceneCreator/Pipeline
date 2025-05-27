using System;
using System.IO;
using System.Linq;
using Assets.Json_Files;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Assets
{
    public class Writer
    {
        public void WriteScene(ReverseConverter converter)
        {
            Scene2 scene2 = new Scene2
            {
                SceneName = converter.SceneName,
                Environment = new Environment2
                {
                    Type = converter.Environment.Type,
                    Dimensions = new Dimensions3D2
                    {
                        Width = converter.Environment.Dimensions.Width,
                        Height = converter.Environment.Dimensions.Height,
                        Depth = converter.Environment.Dimensions.Depth
                    },
                    Lighting = converter.Environment.Lighting
                        .Select(l => new LightSource2
                        {
                            LightType = l.LightType,
                            Intensity = l.Intensity,
                            Color = l.Color,
                            Range = l.Range,
                            SpotAngle = l.SpotAngle
                        }).ToList(),
                    Background = converter.Environment.Background
                },
                ObjectGroups = converter.ObjectGroups
                    .ToDictionary(
                        kv => kv.Key,
                        kv => new ObjectGroup2 { Color = kv.Value }
                    )
            };

            foreach (JulangGameObject julangGameObject in converter.GameObjects)
            {
                TopLevelSceneObject2 topLevelSceneObject2 = new TopLevelSceneObject2();

                topLevelSceneObject2.ObjectId = julangGameObject.ObjectId;
                topLevelSceneObject2.ObjectType = julangGameObject.ObjectType;
                topLevelSceneObject2.AssetName = julangGameObject.AssetName;

                topLevelSceneObject2.Position = new XyzCoordinates2();
                topLevelSceneObject2.Position.X = julangGameObject.GameObject.transform.position.x;
                topLevelSceneObject2.Position.Y = julangGameObject.GameObject.transform.position.y;
                topLevelSceneObject2.Position.Z = julangGameObject.GameObject.transform.position.z;

                topLevelSceneObject2.Rotation = new XyzCoordinates2();
                Vector3 eulerWorld = julangGameObject.GameObject.transform.eulerAngles;
                topLevelSceneObject2.Rotation.X = eulerWorld.x;
                topLevelSceneObject2.Rotation.Y = eulerWorld.y;
                topLevelSceneObject2.Rotation.Z = eulerWorld.z;

                topLevelSceneObject2.Dimensions = new Dimensions3D2();
                topLevelSceneObject2.Dimensions.Width = julangGameObject.GameObject.transform.localScale.x;
                topLevelSceneObject2.Dimensions.Height = julangGameObject.GameObject.transform.localScale.y;
                topLevelSceneObject2.Dimensions.Depth = julangGameObject.GameObject.transform.localScale.z;

                topLevelSceneObject2.Group = julangGameObject.Group;
                topLevelSceneObject2.Specification = julangGameObject.Specification;

                scene2.Objects.Add(topLevelSceneObject2);
            }

            SceneData2 sceneData2 = new SceneData2 {Scene = scene2};

            string json = JsonConvert.SerializeObject(sceneData2, Formatting.Indented);

            string folderPath = Path.Combine(Application.dataPath, "Json_Tests", "GPT_Tests");
            string fileName = $"output_reverse_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json";
            string filePath = Path.Combine(folderPath, fileName + ".json");

            File.WriteAllText(filePath, json);

            Debug.Log($"JSON gespeichert unter: {filePath}");

            #if UNITY_EDITOR
            AssetDatabase.Refresh();
            #endif
        }
    }
}