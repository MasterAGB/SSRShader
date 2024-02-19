using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FlipNormalsEditor : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Custom Tools/Flip Normals")]
    static void Flip()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            Debug.LogError("No object selected.");
            return;
        }

        MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("No MeshFilter found on the selected object.");
            return;
        }

        Mesh mesh = null;
        if (Application.isPlaying)
        {
            mesh = meshFilter.mesh;
        }
        else
        {
            mesh = meshFilter.mesh;
        }

        
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }

        mesh.normals = normals;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                int temp = triangles[j];
                triangles[j] = triangles[j + 1];
                triangles[j + 1] = temp;
            }

            mesh.SetTriangles(triangles, i);
        }

        
        
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.RecalculateUVDistributionMetrics();

        
        // Rename the mesh to indicate it's been modified
        mesh.name = selectedObject.name + "_Modified";

        // If you want to see the changes immediately in the Editor (outside of Play mode)
#if UNITY_EDITOR
        EditorUtility.SetDirty(mesh);
#endif
    }



    
    [MenuItem("Custom Tools/Export Mesh to OBJ")]
    public static void ExportSelectedMeshToObj()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject with a Mesh.", "OK");
            return;
        }

        MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("No Mesh", "Selected GameObject does not have a MeshFilter with a mesh.", "OK");
            return;
        }

        // Retrieve the last path used from EditorPrefs
        string lastPath = EditorPrefs.GetString("LastUsedObjPath", "");

        string path = EditorUtility.SaveFilePanel("Save OBJ File", lastPath, selectedObject.name + ".obj", "obj");
        if (string.IsNullOrEmpty(path))
            return;

        // Save the directory used for next time
        string directory = Path.GetDirectoryName(path);
        EditorPrefs.SetString("LastUsedObjPath", directory);

        //Getting mesh, not shared mesh
        Mesh mesh = meshFilter.mesh;
        ExportToObj(path, mesh);

        // Convert absolute path to relative path
        string relativePath = AbsoluteToRelativePath(path);

        // Import and apply the exported OBJ file
        ImportAndApplyObj(relativePath, selectedObject);
    }



    private static string AbsoluteToRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
        {
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }
        else
        {
            Debug.LogError("The path is outside the project: " + absolutePath);
            return "";
        }
    }
    private static void ImportAndApplyObj(string path, GameObject selectedObject)
    {
        // Re-import the asset to update the reference in Unity
        AssetDatabase.ImportAsset(path);
        Mesh newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

      
        if (newMesh != null)
        {
            MeshFilter mf = selectedObject.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.mesh = newMesh;
            }
        }
        else
        {
            Debug.LogError("Error applying new mesh. New mesh is null.");
        }
    }


    private static void ExportToObj(string path, Mesh mesh)
    {
        Debug.Log("Exporting mesh: " + mesh.name);
        string objectName = mesh.name; // or some other meaningful name based on your needs

        
        StringBuilder stringBuilder = new StringBuilder();

        // Object name
        stringBuilder.Append("o ").Append(objectName).Append("\n");

        foreach (var vertex in mesh.vertices)
        {
            //stringBuilder.Append(string.Format("v {0} {1} {2}\n", vertex.x, vertex.y, vertex.z));
            stringBuilder.Append(string.Format("v {0} {1} {2}\n", -vertex.x, vertex.y, vertex.z));

        }

        stringBuilder.Append("\n");

        foreach (var normal in mesh.normals)
        {
            //im doing the MINUS magic all the time, because its getting flippt alwayss..
            stringBuilder.Append(string.Format("vn {0} {1} {2}\n", -normal.x, normal.y, normal.z));
        }

        stringBuilder.Append("\n");

        foreach (var uv in mesh.uv)
        {
            stringBuilder.Append(string.Format("vt {0} {1}\n", uv.x, uv.y));
        }
        
        
        // Group name (often the same as the object name)
        stringBuilder.Append("g ").Append(objectName).Append("\n");

        // Material name (you might need to define this based on your mesh/material setup)
        //stringBuilder.Append("usemtl ").Append(objectName).Append("\n");
        // Map name (optional and often omitted if not using a specific map)
        // stringBuilder.Append("usemap ").Append(objectName).Append("\n");



        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            stringBuilder.Append("\n");

            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                stringBuilder.Append(string.Format("f {2}/{2}/{2} {1}/{1}/{1} {0}/{0}/{0}\n",
                //stringBuilder.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[j] + 1, triangles[j + 1] + 1, triangles[j + 2] + 1));
            }
        }

        File.WriteAllText(path, stringBuilder.ToString());
    }
    
    
    
    [MenuItem("Custom Tools/Copy Selected GameObject Names to Clipboard")]
    private static void CopyNames()
    {
        // Get all selected GameObjects
        var selectedObjects = Selection.gameObjects;
        
        // Use StringBuilder for efficiency with large numbers of objects
        StringBuilder names = new StringBuilder();
        
        // Append each selected GameObject's name to the StringBuilder
        foreach (var obj in selectedObjects)
        {
            names.AppendLine(obj.name);
        }
        
        // Copy the resulting string to the clipboard
        EditorGUIUtility.systemCopyBuffer = names.ToString();
        
        // Optional: Print a confirmation or the copied text to the Unity Console
        Debug.Log("Copied to clipboard:\n" + names.ToString());
    }
#endif
}