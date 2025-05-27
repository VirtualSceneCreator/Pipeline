
//using Assets.Json_Files;
//using System.Collections.Generic;
//using UnityEngine;

//namespace Assets.Json_Controller
//{
//    public class JsonController1 : IJsonController
//    {
//        public SceneData1 SceneData;
//        public Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
//        //private UnityConnectionSpawner _spawner = new UnityConnectionSpawner();

//        public Dictionary<string, GameObject> LoadScene(object json)
//        {
//            spawnedObjects = new Dictionary<string, GameObject>();

//            if (json == null)
//            {
//                return spawnedObjects;
//            }

//            SceneData = (SceneData1)json;

//            if (SceneData.Scene == null)
//            {
//                return spawnedObjects;
//            }

//            SpawnObjects();

//            return spawnedObjects;
//        }

//        private void SpawnObjects()
//        {
//            if (SceneData == null || SceneData.Scene == null)
//            {
//                return;
//            }

//            foreach (TopLevelSceneObject topLevelSceneObject in SceneData.Scene.Objects)
//            {
//                SpawnObject(topLevelSceneObject);
//            }
//        }

//        private GameObject SpawnObject(TopLevelSceneObject objData)
//        {
//            GameObject obj;

//            // Falls ein Asset (Prefab) angegeben ist, versuchen wir dieses zu laden
//            if (!string.IsNullOrEmpty(objData.AssetName))
//            {
//                GameObject prefab = Resources.Load<GameObject>(objData.AssetName);
//                if (prefab != null)
//                {
//                    //obj = _spawner.InstantiateObj(prefab);
//                }
//                else
//                {
//                    Debug.LogWarning("Prefab " + objData.AssetName + " nicht gefunden. Erstelle leeres GameObject.");
//                    //obj = _spawner.InstantiateObj(Resources.Load<GameObject>("Cube"));
//                }
//            }
//            else
//            {
//                obj = new GameObject(objData.ObjectId);
//            }

//            //obj.name = objData.ObjectId + "_" + objData.ObjectType;

//            // Berechne die Position: Falls relativePositioning definiert ist,
//            // versuchen wir, das referenzierte Objekt zu finden.
//            Vector3 position;
//            if (objData.RelativePositioning != null && !string.IsNullOrEmpty(objData.RelativePositioning.ReferenceObject))
//            {
//                if (spawnedObjects.TryGetValue(objData.RelativePositioning.ReferenceObject, out GameObject refObj))
//                {
//                    position = CalculateRelativePosition(refObj, objData);
//                }
//                else
//                {
//                    Debug.LogWarning("Referenzobjekt " + objData.RelativePositioning.ReferenceObject + " nicht gefunden für Objekt " + objData.ObjectId);
//                    position = ConvertToVector3(objData.Position);
//                }
//            }
//            else
//            {
//                position = ConvertToVector3(objData.Position);
//            }
//            obj.transform.position = position;

//            // Setze Rotation (Euler-Winkel)
//            obj.transform.eulerAngles = ConvertToVector3(objData.Rotation);

//            // Setze die Skalierung anhand der Dimensionen
//            obj.transform.localScale = new Vector3(objData.Dimensions.Width, objData.Dimensions.Height, objData.Dimensions.Depth);

//            // Füge das Objekt zur Dictionary hinzu
//            spawnedObjects[objData.ObjectId] = obj;

//            // Falls Kind-Objekte definiert sind, spawne diese rekursiv
//            if (objData.Children != null)
//            {
//                foreach (var child in objData.Children)
//                {
//                    SpawnChildObject(child, obj);
//                }
//            }

//            return obj;
//        }

//        void SpawnChildObject(ChildSceneObject childData, GameObject parent)
//        {
//            GameObject childObj;

//            if (!string.IsNullOrEmpty(childData.AssetName))
//            {
//                GameObject prefab = Resources.Load<GameObject>(childData.AssetName);
//                if (prefab != null)
//                {
//                    childObj = _spawner.InstantiateObj(prefab, parent.transform);
//                }
//                else
//                {
//                    Debug.LogWarning("Prefab " + childData.AssetName + " nicht gefunden. Erstelle leeres GameObject.");
//                    childObj = _spawner.InstantiateObj(Resources.Load<GameObject>("Cube"), parent.transform);
//                    childObj.transform.parent = parent.transform;
//                }
//            }
//            else
//            {
//                childObj = new GameObject(childData.ObjectId);
//                childObj.transform.parent = parent.transform;
//            }

//            childObj.name = childData.ObjectId + "_" + childData.ObjectType;

//            // Für Child-Objekte erfolgt die Positionierung über relativePositioning.
//            // Hier nehmen wir an, dass das referenzierte Objekt in der Regel der Parent ist.
//            Vector3 position = parent.transform.position;
//            if (childData.RelativePositioning != null && !string.IsNullOrEmpty(childData.RelativePositioning.ReferenceObject))
//            {
//                position = CalculateRelativePosition(parent, childData);
//            }
//            childObj.transform.position = position;

//            // Setze Rotation
//            childObj.transform.eulerAngles = ConvertToVector3(childData.Rotation);

//            // Setze Skalierung
//            childObj.transform.localScale = new Vector3(childData.Dimensions.Width, childData.Dimensions.Height, childData.Dimensions.Depth);

//            // Spawne ggf. weitere verschachtelte Kind-Objekte
//            if (childData.Children != null)
//            {
//                foreach (var nested in childData.Children)
//                {
//                    SpawnChildObject(nested, childObj);
//                }
//            }
//        }

//        Vector3 ConvertToVector3(XyzCoordinates coords)
//        {
//            return new Vector3(coords.X, coords.Y, coords.Z);
//        }

//        Vector3 CalculateRelativePosition(GameObject referenceObj, TopLevelSceneObject objData)
//        {
//            // Wir nutzen die Transform.scale als Annäherung an die Dimensionen
//            Dimensions3D refDim = new Dimensions3D
//            {
//                Width = referenceObj.transform.localScale.x,
//                Height = referenceObj.transform.localScale.y,
//                Depth = referenceObj.transform.localScale.z
//            };

//            Dimensions3D childDim = objData.Dimensions;

//            return CalculatePosition(referenceObj.transform.position, objData.RelativePositioning, refDim, childDim, objData.Offset);
//        }

//        Vector3 CalculateRelativePosition(GameObject referenceObj, ChildSceneObject childData)
//        {
//            Dimensions3D refDim = new Dimensions3D
//            {
//                Width = referenceObj.transform.localScale.x,
//                Height = referenceObj.transform.localScale.y,
//                Depth = referenceObj.transform.localScale.z
//            };

//            Dimensions3D childDim = childData.Dimensions;

//            return CalculatePosition(referenceObj.transform.position, childData.RelativePositioning, refDim, childDim, childData.Offset);
//        }

//        Vector3 CalculatePosition(Vector3 referencePosition, RelativePositioning rel, Dimensions3D refDim, Dimensions3D childDim, XyzCoordinates offset)
//        {
//            Vector3 offsetVector = Vector3.zero;
//            switch (rel.Relation)
//            {
//                case "in_front_of_positive_z":
//                    offsetVector = new Vector3(0, 0, rel.Distance);
//                    break;
//                case "behind_negative_z":
//                    offsetVector = new Vector3(0, 0, -rel.Distance);
//                    break;
//                case "left_of_negative_x":
//                    offsetVector = new Vector3(-rel.Distance, 0, 0);
//                    break;
//                case "right_of_positive_x":
//                    offsetVector = new Vector3(rel.Distance, 0, 0);
//                    break;
//                case "on_top_of":
//                    // Hier gehen wir davon aus, dass das Child exakt oben auf dem Referenzobjekt sitzt.
//                    offsetVector = new Vector3(0, refDim.Height / 2 + childDim.Height / 2, 0);
//                    break;
//                case "below":
//                    if (rel.Distance != 0)
//                        offsetVector = new Vector3(0, -rel.Distance, 0);
//                    else
//                        offsetVector = new Vector3(0, -(refDim.Height / 2 + childDim.Height / 2), 0);
//                    break;
//                case "next_to":
//                    // Hier wird kein Abstand (distance) verwendet, stattdessen muss ein offset angegeben sein.
//                    if (offset != null)
//                        offsetVector = ConvertToVector3(offset);
//                    break;
//                case "over":
//                    offsetVector = new Vector3(0, rel.Distance, 0);
//                    break;
//                default:
//                    break;
//            }
//            return referencePosition + offsetVector;
//        }
//    }
//}