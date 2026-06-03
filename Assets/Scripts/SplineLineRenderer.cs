using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[ExecuteInEditMode]
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplineLineRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    public float lineWidth = 0.15f;
    public float yOffset = 0.01f;
    public float textureTiling = 1f;

    private SplineContainer splineContainer;
    private MeshFilter meshFilter;

    private void OnEnable()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();
        Spline.Changed += OnSplineChanged;
        RebuildMesh();
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
    }

    private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
    {
        RebuildMesh();
    }

    private void OnValidate()
    {
        RebuildMesh();
    }

    [ContextMenu("Rebuild Mesh")]
    public void RebuildMesh()
    {
        if (splineContainer == null || splineContainer.Splines == null || splineContainer.Splines.Count == 0)
            return;

        Mesh mesh = new Mesh();
        mesh.name = "SplineLineMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int s = 0; s < splineContainer.Splines.Count; s++)
        {
            var spline = splineContainer.Splines[s];
            if (spline.Count < 2) continue;

            float splineLength = spline.GetLength();
            // 4 vertices per meter to make smooth curves
            int segments = Mathf.Max(8, Mathf.RoundToInt(splineLength * 4f));
            float step = 1f / segments;

            int startVertIndex = vertices.Count;
            float currentDistance = 0f;
            Vector3 prevCenter = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float t = i * step;

                // Evaluate world position, tangent, and up vector from SplineContainer
                float3 position, tangent, upVector;
                splineContainer.Evaluate(s, t, out position, out tangent, out upVector);

                Vector3 worldCenter = (Vector3)position;
                Vector3 worldTangent = (Vector3)tangent;
                Vector3 worldUp = (Vector3)upVector;

                Vector3 worldRight = Vector3.Cross(worldTangent, worldUp).normalized;

                // Position vertices slightly offset on Up axis to prevent Z-fighting
                Vector3 leftPos = worldCenter - worldRight * (lineWidth * 0.5f) + worldUp.normalized * yOffset;
                Vector3 rightPos = worldCenter + worldRight * (lineWidth * 0.5f) + worldUp.normalized * yOffset;

                // Transform to local space of this GameObject
                leftPos = transform.InverseTransformPoint(leftPos);
                rightPos = transform.InverseTransformPoint(rightPos);

                vertices.Add(leftPos);
                vertices.Add(rightPos);

                if (i > 0)
                {
                    currentDistance += Vector3.Distance(worldCenter, prevCenter);
                }
                prevCenter = worldCenter;

                // Tile texture vertically along spline length
                uvs.Add(new Vector2(0f, currentDistance * textureTiling));
                uvs.Add(new Vector2(1f, currentDistance * textureTiling));

                if (i < segments)
                {
                    int vertIndex = startVertIndex + i * 2;

                    // Triangle 1 (Facing Upwards)
                    triangles.Add(vertIndex);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 1);

                    // Triangle 2 (Facing Upwards)
                    triangles.Add(vertIndex + 1);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 3);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }
}
