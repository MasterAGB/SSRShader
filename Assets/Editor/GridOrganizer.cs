using UnityEngine;
using UnityEditor;

public class GridOrganizer : EditorWindow
{
    private GameObject parentObject;
    private float gridSpacing = 1.0f;
    private int maxColumns = 5;
    private Vector3 startOffset = Vector3.zero;
    private enum Layout { Horizontal, Vertical, Grid }
    private Layout layoutType = Layout.Grid;

    [MenuItem("Custom Tools/Grid Organizer")]
    public static void ShowWindow()
    {
        GetWindow<GridOrganizer>("Grid Organizer");
    }

    void OnGUI()
    {
        GUILayout.Label("Grid Organizer Settings", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        layoutType = (Layout)EditorGUILayout.EnumPopup("Layout Type", layoutType);
        gridSpacing = EditorGUILayout.FloatField("Grid Spacing", gridSpacing);

        if (layoutType == Layout.Grid)
        {
            maxColumns = EditorGUILayout.IntField("Max Columns", maxColumns);
        }

        startOffset = EditorGUILayout.Vector3Field("Start Offset", startOffset);

        if (GUILayout.Button("Organize"))
        {
            if (parentObject != null)
            {
                OrganizeChildren();
            }
            else
            {
                EditorUtility.DisplayDialog("Grid Organizer", "No Parent Object selected. Please select a Parent Object.", "OK");
            }
        }
    }

    private void OrganizeChildren()
    {
        int rowCount = 0;
        int colCount = 0;

        foreach (Transform child in parentObject.transform)
        {
            float x = layoutType == Layout.Vertical ? 0 : colCount * gridSpacing;
            float y = layoutType == Layout.Horizontal ? 0 : rowCount * gridSpacing;
            float z = 0;
            child.localPosition = startOffset + new Vector3(x, y, z);

            if (layoutType == Layout.Grid)
            {
                colCount++;
                if (colCount >= maxColumns)
                {
                    colCount = 0;
                    rowCount++;
                }
            }
            else if (layoutType == Layout.Horizontal)
            {
                colCount++;
            }
            else if (layoutType == Layout.Vertical)
            {
                rowCount++;
            }
        }
    }
}