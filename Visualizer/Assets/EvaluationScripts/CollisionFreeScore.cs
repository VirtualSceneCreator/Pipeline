using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.EvaluationScripts
{
    public class CollisionFreeScore : ScriptableObject
    {
        public static float CalculateCfs()
        {
            List<MeshRenderer> allSceneElements = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).ToList();

            var penetrated = new HashSet<MeshRenderer>();

            for (int i = 0; i < allSceneElements.Count; i++)
            {
                var ra = allSceneElements[i];
                if (!ra) continue;

                for (int j = i + 1; j < allSceneElements.Count; j++)
                {
                    var rb = allSceneElements[j];
                    if (!rb) continue;

                    if (BoundsPenetrate(ra.bounds, rb.bounds))
                    {
                        ra.sharedMaterial.color = rb.sharedMaterial.color =
                            Visualizer.Instance.CollisionColor;

                        penetrated.Add(ra);
                        penetrated.Add(rb);
                    }
                }
            }

            int collisionFreeCount = allSceneElements.Count - penetrated.Count;

            float cfsPercent = allSceneElements.Count == 0
                ? 100f
                : ((float)collisionFreeCount / (float)allSceneElements.Count) * 100f;

            return cfsPercent;
        }

        public static float CalculateCfsWithoutFloorWalls()
        {
            List<MeshRenderer> allSceneElements = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).ToList().Where(mr =>
                    !mr.name.Contains("floor", StringComparison.OrdinalIgnoreCase) &&
                    !mr.name.Contains("wall", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var penetrated = new HashSet<MeshRenderer>();

            for (int i = 0; i < allSceneElements.Count; i++)
            {
                var ra = allSceneElements[i];
                if (!ra) continue;

                for (int j = i + 1; j < allSceneElements.Count; j++)
                {
                    var rb = allSceneElements[j];
                    if (!rb) continue;

                    if (BoundsPenetrate(ra.bounds, rb.bounds))
                    {
                        ra.sharedMaterial.color = rb.sharedMaterial.color =
                            Visualizer.Instance.CollisionColor;

                        penetrated.Add(ra);
                        penetrated.Add(rb);
                    }
                }
            }

            int collisionFreeCount = allSceneElements.Count - penetrated.Count;

            float cfsPercent = allSceneElements.Count == 0
                ? 100
                : (float)collisionFreeCount / allSceneElements.Count * 100f;

            return cfsPercent;
        }

        public static float CalculateVolumeCollisionImpactScore()
        {
            List<MeshRenderer> allSceneElements = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).ToList();

            // Gesamtvolumen aller Objekte (Bounds-Volumen)
            float totalVolume = 0f;
            foreach (var r in allSceneElements)
            {
                totalVolume += GetVolume(r.bounds);
            }

            // Summe des Kollisions-Überlappungsvolumens
            float collisionVolumeSum = 0f;

            for (int i = 0; i < allSceneElements.Count; i++)
            {
                var ra = allSceneElements[i];
                if (!ra) continue;

                for (int j = i + 1; j < allSceneElements.Count; j++)
                {
                    var rb = allSceneElements[j];
                    if (!rb) continue;

                    // Berechne Überlappung
                    float overlapVol = GetIntersectionVolume(ra.bounds, rb.bounds);
                    if (overlapVol > 0f)
                    {
                        // Färbe die kollidierenden Objekte ein
                        ra.sharedMaterial.color = rb.sharedMaterial.color = Visualizer.Instance.CollisionColor;
                        // Addiere Überlappungsvolumen
                        collisionVolumeSum += overlapVol;
                    }
                }
            }

            // Verbleibendes "freie" Volumen
            float freeVolume = Mathf.Max(0f, totalVolume - collisionVolumeSum);

            // Prozentualer CFS-Wert
            float cfsPercent = totalVolume == 0f
                ? 100
                : freeVolume / totalVolume * 100f;

            return cfsPercent;
        }

        public static float CalculateNormalizedCollisionImpactScore()
        {
            var all = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

            // Für alle Kollisionspaare aufsummierte, normalisierte Überlappung
            float sumNormalizedOverlap = 0f;
            int collisionPairs = 0;

            foreach (var ra in all)
            {
                if (!ra) continue;
                float volA = GetVolume(ra.bounds);

                foreach (var rb in all)
                {
                    if (rb == ra || !rb) continue;
                    float volB = GetVolume(rb.bounds);

                    float volI = GetIntersectionVolume(ra.bounds, rb.bounds);
                    if (volI > 0f)
                    {
                        // Einfärben
                        ra.sharedMaterial.color = rb.sharedMaterial.color = Visualizer.Instance.CollisionColor;

                        // Normierte Überlappung
                        float norm = volI / (volA + volB);
                        sumNormalizedOverlap += norm;
                        collisionPairs++;
                    }
                }
            }

            // Mittelwert aller Paar-Überlappungen (0…1)
            float avgOverlap = collisionPairs > 0
                ? sumNormalizedOverlap / collisionPairs
                : 0f;

            // CFS = (1 - avgOverlap) * 100%
            return (1f - avgOverlap) * 100f;
        }

        private static float GetVolume(Bounds b)
        {
            Vector3 s = b.size;
            return s.x * s.y * s.z;
        }

        private static float GetIntersectionVolume(Bounds a, Bounds b)
        {
            // Überlappung in jeder Achse
            float dx = Mathf.Max(0f, Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x));
            float dy = Mathf.Max(0f, Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y));
            float dz = Mathf.Max(0f, Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));
            return dx * dy * dz;
        }

        private static bool BoundsPenetrate(in Bounds a, in Bounds b, float eps = 1e-5f)
        {
            //  Strict overlap auf allen 3 Achsen:
            return (a.min.x + eps < b.max.x && a.max.x - eps > b.min.x) &&
                   (a.min.y + eps < b.max.y && a.max.y - eps > b.min.y) &&
                   (a.min.z + eps < b.max.z && a.max.z - eps > b.min.z);
        }
    }
}