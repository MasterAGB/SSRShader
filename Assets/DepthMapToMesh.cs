using UnityEngine;

[ExecuteInEditMode]
public class DepthMapToMeshExtended : MonoBehaviour
{
    public Texture2D depthMap;
    public Texture2D albedoMap;
    public Material material;
    public bool generate = false;
    public float farPlane = 10.0f; // Far plane for depth scaling
    public float fov = 60.0f; // Field of view for realistic scaling

    void Update()
    {
        if (generate)
        {
            generate = false;
            Generate();
        }
    }

    void Generate()
    {
        if (depthMap == null || albedoMap == null || material == null)
        {
            Debug.LogError("Please assign all fields.");
            return;
        }

        GameObject meshObject = new GameObject("MeshFromDepthMapExtended");
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        var colors = new System.Collections.Generic.List<Color>();

        // Adjust quad size based on FOV and aspect ratio
        float aspectRatio = (float)depthMap.width / (float)depthMap.height;
        float fovRad = fov * Mathf.Deg2Rad;
        float quadHeight = 2.0f * Mathf.Tan(fovRad / 2) * farPlane / depthMap.height; // Height of a quad at the far plane
        float quadWidth = quadHeight * aspectRatio; // Width of a quad, maintaining aspect ratio
        
        
        

        Debug.Log(depthMap.width+"x"+depthMap.height);
        
        for (int x = 0; x < depthMap.width-1; x++)
        {
            for (int y = 0; y < depthMap.height-1; y++)
            {
                
                    float depth = depthMap.GetPixel(x, y).grayscale * farPlane;
                    // Adjust position based on depth to simulate perspective
                    float adjustedQuadSizeX = quadWidth * (depth / farPlane);
                    float adjustedQuadSizeY = quadHeight * (depth / farPlane);
                    Vector3 position = new Vector3((x - depthMap.width / 2.0f) * adjustedQuadSizeX,
                        (y - depthMap.height / 2.0f) * adjustedQuadSizeY, -depth);
             

                    // Define vertices
                    Vector3 topLeftPos = position;
                    Vector3 topRightPos = new Vector3(position.x + adjustedQuadSizeX, position.y, position.z);
                    Vector3 bottomRightPos = new Vector3(position.x + adjustedQuadSizeX, position.y - adjustedQuadSizeY,
                        position.z);
                    Vector3 bottomLeftPos = new Vector3(position.x, position.y - adjustedQuadSizeY, position.z);

                    
                    // Get colors for each vertex
                    Color color = albedoMap.GetPixel(x, y);
                    
                    // Add quad with vertex colors
                    //AddQuad(vertices, colors, triangles, topLeftPos, topRightPos, bottomRightPos, bottomLeftPos, color, color, color, color);
                    AddCube(vertices, colors, triangles, topLeftPos, topRightPos, bottomRightPos, bottomLeftPos, color, color, color, color);
                

                    /*

                    // Add connecting quads with gradients if applicable
                    if (x > 0) {
                        // Calculate colors and positions for the left connecting quad
                        float depthLeft = depthMap.GetPixel(x - 1, y).grayscale * farPlane;
                        Color colorLeft = albedoMap.GetPixel(x - 1, y);

                        float adjustedQuadSizeX2 = quadWidth * (depthLeft / farPlane);
                        float adjustedQuadSizeY2 = quadHeight * (depthLeft / farPlane);

                        // Calculate the interpolated position for the left connecting quad
                        Vector3 interpolatedPosition = new Vector3((x - 0.5f - depthMap.width / 2.0f) * adjustedQuadSizeX2, (y - depthMap.height / 2.0f) * adjustedQuadSizeY2, -depthLeft);

                        // Define vertices for the left connecting quad
                        Vector3 leftQuadTopRightCorner = new Vector3(interpolatedPosition.x + adjustedQuadSizeX2 / 2, interpolatedPosition.y, interpolatedPosition.z);
                        Vector3 leftQuadBottomRightCorner = new Vector3(interpolatedPosition.x + adjustedQuadSizeX2 / 2, interpolatedPosition.y - adjustedQuadSizeY2, interpolatedPosition.z);



                        AddQuad(vertices, colors, triangles,
                            leftQuadTopRightCorner,topLeftPos,bottomLeftPos,leftQuadBottomRightCorner,
                            colorLeft, color, color, colorLeft);
                    }

                    if (y > 0) {
                        // Calculate colors and positions for the top connecting quad
                        float depthTop = depthMap.GetPixel(x, y - 1).grayscale * farPlane;
                        Color colorTop = albedoMap.GetPixel(x, y - 1);


                        float adjustedQuadSizeX2 = quadWidth * (depthTop / farPlane);
                        float adjustedQuadSizeY2 = quadHeight * (depthTop / farPlane);


                        // The interpolated position is halfway between the current and the top pixel
                        Vector3 interpolatedPosition = new Vector3((x - depthMap.width / 2.0f) * adjustedQuadSizeX2, (y - 0.5f - depthMap.height / 2.0f) * adjustedQuadSizeY2, -depthTop);

                        // Define vertices for the top connecting quad
                        Vector3 topQuadTopLeftCorner = new Vector3(interpolatedPosition.x, interpolatedPosition.y - adjustedQuadSizeY2 / 2, interpolatedPosition.z);
                        Vector3 topQuadTopRightCorner = new Vector3(interpolatedPosition.x + adjustedQuadSizeX2, interpolatedPosition.y - adjustedQuadSizeY2 / 2, interpolatedPosition.z);




                        AddQuad(vertices, colors, triangles,
                            topQuadTopLeftCorner,topQuadTopRightCorner,topRightPos,topLeftPos,
                            colorTop, colorTop, color, color);






                    }





                    if (x > 0 && y > 0) {
                        // Calculate colors and positions for the diagonal connecting quad
                        float depthTop = depthMap.GetPixel(x, y - 1).grayscale * farPlane;
                        Color colorTop = albedoMap.GetPixel(x, y - 1);

                        float depthLeft = depthMap.GetPixel(x - 1, y).grayscale * farPlane;
                        Color colorLeft = albedoMap.GetPixel(x - 1, y);

                        float depthDiag = depthMap.GetPixel(x - 1, y - 1).grayscale * farPlane;
                        Color colorDiag = albedoMap.GetPixel(x - 1, y - 1);
                        Vector3 diagTop = new Vector3((x - 1) * quadSize - depthMap.width * quadSize * 0.5f, (y - 1) * quadSize - depthMap.height * quadSize * 0.5f, -depthDiag);
                        Vector3 diagBottom = new Vector3(x * quadSize - depthMap.width * quadSize * 0.5f, y * quadSize - depthMap.height * quadSize * 0.5f, -depth);
                        // Create a quad that connects the current quad diagonally to the previous quad
                        AddQuad(vertices, colors, triangles,
                            diagTop,
                            topLeft,
                            bottomLeft,
                            diagBottom,

                            colorDiag,
                            colorTop,
                            color,
                            colorLeft);
                    }
                    */

            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        
        meshObject.transform.SetParent(transform, false);
    }

    void AddQuad(System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.List<Color> colors, System.Collections.Generic.List<int> triangles,
                 Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft,
                 Color colorTL, Color colorTR, Color colorBR, Color colorBL)
    {
        int startIndex = vertices.Count;
        vertices.AddRange(new Vector3[] { topLeft, topRight, bottomRight, bottomLeft });
        colors.AddRange(new Color[] { colorTL, colorTR, colorBR, colorBL });
        triangles.AddRange(new int[] { startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3 });
    }
    
    void AddCube(System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.List<Color> colors, System.Collections.Generic.List<int> triangles,
        Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft,
        Color colorTL, Color colorTR, Color colorBR, Color colorBL)
    {
        // Calculate cube dimensions
        float width = Vector3.Distance(topLeft, topRight)*7;
        Vector3 depthDirection = Vector3.forward * width; // Assuming forward is the depth direction

        // Calculate back face vertices
        Vector3 backTopLeft = topLeft + depthDirection;
        Vector3 backTopRight = topRight + depthDirection;
        Vector3 backBottomRight = bottomRight + depthDirection;
        Vector3 backBottomLeft = bottomLeft + depthDirection;

        // Add front face (same as input quad)
        AddQuad(vertices, colors, triangles, topLeft, topRight, bottomRight, bottomLeft, colorTL, colorTR, colorBR, colorBL);

        // Add back face
        AddQuad(vertices, colors, triangles, backTopRight, backTopLeft, backBottomLeft, backBottomRight, colorTL, colorTR, colorBR, colorBL);

        // Add top face
        AddQuad(vertices, colors, triangles, backTopLeft, backTopRight, topRight, topLeft, colorTL, colorTL, colorTR, colorTR);

        // Add bottom face
        AddQuad(vertices, colors, triangles, bottomLeft, bottomRight, backBottomRight, backBottomLeft, colorBL, colorBR, colorBR, colorBL);

        // Add left face
        AddQuad(vertices, colors, triangles, backTopLeft, topLeft, bottomLeft, backBottomLeft, colorTL, colorTL, colorBL, colorBL);

        // Add right face
        AddQuad(vertices, colors, triangles, topRight, backTopRight, backBottomRight, bottomRight, colorTR, colorTR, colorBR, colorBR);
    }
}
