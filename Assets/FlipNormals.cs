using UnityEngine;

public class FlipNormals : MonoBehaviour
{
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter && meshFilter.sharedMesh)
        {
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < normals.Length; i++)
            {
                // Transform the vertex from local to world space
                Vector3 worldVertex = transform.TransformPoint(vertices[i]);
                
                // Transform the normal from local to world direction
                Vector3 worldNormal = transform.TransformDirection(normals[i]);

                // Draw the normal
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(worldVertex, worldVertex + worldNormal * 0.1f); // Adjust the multiplier as needed
            }
        }
    }
#endif
}