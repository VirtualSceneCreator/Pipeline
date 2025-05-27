
using UnityEngine;

namespace Assets.EvaluationScripts
{
    public class InBoundaryScore : ScriptableObject
    {
        public static float CalculateIbs(float width, float height, float depth)
        {
            MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int totalCount = renderers.Length;
            int insideCount = 0;
            Vector3 boundaryCenter = Vector3.zero;

            Vector3 halfExtents = new Vector3(width, height, depth) * 0.5f;
            Vector3 minB = boundaryCenter - halfExtents;
            Vector3 maxB = boundaryCenter + halfExtents;

            foreach (var mr in renderers)
            {
                Bounds b = mr.bounds;
                Vector3 bMin = b.min;
                Vector3 bMax = b.max;

                bool inside =
                    bMin.x >= minB.x && bMax.x <= maxB.x &&
                    bMin.y >= minB.y && bMax.y <= maxB.y &&
                    bMin.z >= minB.z && bMax.z <= maxB.z;

                if (inside)
                    insideCount++;
            }

            float score = totalCount == 0
                ? 0
                : (float)insideCount / totalCount * 100;

            return score;
        }
    }
}