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


    GameObject[] prefabs;
    [SerializeField] bool[] selectedPrefabs;

    private void OnEnable()
    {

        so = new SerializedObject(this);

        SceneView.duringSceneGui += DuringSceneGUI;

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/RoomPrefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        if (selectedPrefabs == null || selectedPrefabs.Length != prefabs.Length)
        {
            selectedPrefabs = new bool[prefabs.Length];
        }

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

    private void DuringSceneGUI(SceneView view)
    {

        //GUI
        DrawGUI();


        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        //controlli del tool
        Transform camIf = view.camera.transform;

        if (Event.current.type == EventType.MouseMove)
        {
            view.Repaint();
        }

        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;

        if (Event.current.type == EventType.ScrollWheel && holdingAlt)
        {
            float scrollDirection = Mathf.Sign(Event.current.delta.y);

            so.Update();
            so.ApplyModifiedProperties();

            Repaint();
            Event.current.Use();
        }

    }

    void DrawGUI()
    {
        Handles.BeginGUI();


        Rect rect = new Rect(10, 10, 150, 50);

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();
            selectedPrefabs[i] = GUI.Toggle(rect, selectedPrefabs[i], new GUIContent(prefab.name, icon));

            if (EditorGUI.EndChangeCheck())
            {
                spawnPref.Clear();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    if (selectedPrefabs[j])
                    {
                        spawnPref.Add(prefabs[j]);
                    }
                }
            }
            rect.y += rect.height + 2;
        }

        Handles.EndGUI();
    }
}
