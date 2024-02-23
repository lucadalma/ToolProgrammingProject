using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class LevelEditor : EditorWindow
{

    [MenuItem("Tools/LevelEditor")]
    public static void OpenWindow() => GetWindow<LevelEditor>();

    SerializedObject so;
    public List<GameObject> spawnPref = null;

    private GameObject selectedObject;
    GameObject[] prefabs;
    private Vector3 prefabSize = new Vector3(1, 1, 1);
    Quaternion rotationMesh = Quaternion.identity;

    private void OnEnable()
    {

        so = new SerializedObject(this);

        SceneView.duringSceneGui += DuringSceneGUI;

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/RoomPrefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void OnGUI()
    {
        so.Update();


        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    int index = 0;

    float scrollDelta;

    private void DuringSceneGUI(SceneView view)
    {

        //GUI

        Event e = Event.current;

        if (e.type == EventType.ScrollWheel && e.shift)
        {
            scrollDelta = Mathf.Sign(e.delta.y);
            if (scrollDelta == -1)
            {
                index = (index - 1 + prefabs.Length) % prefabs.Length;
            }
            else
            {
                index = (index + 1) % prefabs.Length;
            }
            rotationMesh = Quaternion.identity;
        }


        ShowObjectsIcon(prefabs);


        selectedObject = prefabs[index];

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        //controlli del tool
        //Transform camIf = view.camera.transform;

        if (Event.current.type == EventType.MouseMove)
        {
            view.Repaint();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null)
        {
            if (selectedObject == null)
            {
                Debug.LogError("selectedObject shouldn't be null! Check content folder!");
                return;
            }

            DrawRoom(view, hit, e);

            if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
            {
                TrySpawnObject(hit);
            }
        }
    }

    private void TrySpawnObject(RaycastHit hit)
    {
        Vector3 spawnPosition = (hit.point + hit.normal * prefabSize.y / 2);
        Quaternion spawnRotation = rotationMesh;

        GameObject spawnedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(selectedObject);

        spawnedPrefab.transform.position = spawnPosition;
        spawnedPrefab.transform.rotation = spawnRotation;


        Undo.RegisterCreatedObjectUndo(spawnedPrefab, "Item");
    }


    private void ShowObjectsIcon(GameObject[] objects)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            int offset = i * 70;
            Handles.BeginGUI();
            Rect rect = new Rect(8, offset + 8, 64, 64);
            Texture icon = AssetPreview.GetAssetPreview(objects[i]);
            bool isSelected;

            if (objects[i] == selectedObject)
                isSelected = true;
            else
                isSelected = false;

            if (GUI.Toggle(rect, isSelected, new GUIContent(icon)))
            {

            }
            new GUIContent(icon);
            Handles.EndGUI();
        }
    }


    private void DrawRoom(SceneView sceneView, RaycastHit hit, Event e) 
    {

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        Handles.color = Color.red;
        
        Handles.DrawLine(hit.point, hit.point + hit.normal * 1f, 0.1f);


        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;

        if (Event.current.type == EventType.ScrollWheel && holdingAlt)
        {
            float scrollDirection = Mathf.Sign(Event.current.delta.y);

            if (scrollDirection == -1)
            {
                rotationMesh *= Quaternion.Euler(Vector3.up * -90);
            }
            else
            {
                rotationMesh *= Quaternion.Euler(Vector3.up * 90);
            }
            Event.current.Use();
        }

        Vector3 drawPosition = (hit.point + hit.normal * (prefabSize.y / 2));
        Matrix4x4 pointToWorldMtx = Matrix4x4.TRS(drawPosition, rotationMesh, Vector3.one);

        MeshFilter[] objectMeshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter filter in objectMeshFilters)
        {
            Matrix4x4 childToPoint = filter.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMatrix = pointToWorldMtx * childToPoint;

            Mesh mesh = filter.sharedMesh;
            Material material = filter.GetComponent<MeshRenderer>().sharedMaterial;

            material.SetPass(0);

            Graphics.DrawMesh(mesh, childToWorldMatrix, material, 0, sceneView.camera);
        }
    }
}
