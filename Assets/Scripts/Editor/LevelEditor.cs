using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.SceneManagement;


/*
 Per il funzionamento del tool, premendo alt e rotellina si può ruotare la stanza di 90 gradi in base alla direzzione della rotazione della rotellina
 Per cambiare stanza da istasnziare, con Shift e la rotellina posso scegliere tra i vari prefab
 Per istanziare la stanza, premere Space
 */
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
    Vector3 drawPosition;
    Matrix4x4 pointToWorldMtx;

    private void OnEnable()
    {

        so = new SerializedObject(this);

        SceneView.duringSceneGui += DuringSceneGUI;

        //Mi prendo i prefab dalla cartella
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

        //Bottone per undo
        GUILayout.Label("Undo Object Positioning", EditorStyles.boldLabel);

        if (GUILayout.Button("Undo"))
        {
            Undo.PerformUndo(); 
        }

    }


    int index = 0;

    float scrollDelta;

    private void DuringSceneGUI(SceneView view)
    {

        //GUI

        Event e = Event.current;


        //Selezione del prefab che si vuole istanziare
        if (e.type == EventType.ScrollWheel && e.shift)
        {
            scrollDelta = Mathf.Sign(e.delta.y);
            //in base alla direzione dello scroll del mouse posso navigare tra le varie stanze
            if (scrollDelta == -1)
            {
                index = (index - 1 + prefabs.Length) % prefabs.Length;
            }
            else
            {
                index = (index + 1) % prefabs.Length;
            }
            //rotazione base
            rotationMesh = Quaternion.identity;
        }

        //Funzione per mostrare in scena i vari prefab
        ShowObjectsIcon(prefabs);

        //Mi prendo il prefab attualmente selezionato
        selectedObject = prefabs[index];


        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        if (Event.current.type == EventType.MouseMove)
        {
            view.Repaint();
        }

        //Casto il ray dome ho la posizione del mouse 
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null)
        {
            //controllo eventuali errori
            if (selectedObject == null)
            {
                Debug.LogError("Non ci sono prefab!");
                return;
            }

            //Funzione per disegnare la mesh della stanza
            DrawRoom(view, hit, e);


            //se premo space, provo ad instanziare la stanza
            if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
            {
                TrySpawnObject(hit);
            }
        }
    }


    private void TrySpawnObject(RaycastHit hit)
    {
        //mi ottengo la posizione e la rotazione i spawn
        Vector3 spawnPosition = drawPosition ;
        Quaternion spawnRotation = rotationMesh;


        //istanzio il prefab
        GameObject spawnedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(selectedObject);

        spawnedPrefab.transform.position = spawnPosition;
        spawnedPrefab.transform.rotation = spawnRotation;

        Undo.RegisterCreatedObjectUndo(spawnedPrefab, "Item");
    }

    //funzione per mostrare le icone delle stanza
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

    //funzione che mi snappa la stanza alla posizione della porta
    public Vector3 SnapRoom(Vector3 doorPosition, Vector3 roomPosition, GameObject doorDestination)
    {

        Vector3 offset = roomPosition - doorPosition;

        Vector3 snappedPosition = doorDestination.transform.position + offset;

        return snappedPosition;
    }

    //Funzione per disegnare la stanza
    private void DrawRoom(SceneView sceneView, RaycastHit hit, Event e) 
    {

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        Handles.color = Color.red;
        
        Handles.DrawLine(hit.point, hit.point + hit.normal * 1f, 0.1f);


        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;

        //Ruoto la stanza
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
        
        drawPosition = (hit.point + hit.normal * (prefabSize.y / 2));
        pointToWorldMtx = Matrix4x4.TRS(drawPosition, rotationMesh, Vector3.one);

        MeshFilter[] objectMeshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
        Door[] doors = selectedObject.GetComponentsInChildren<Door>();

        //mi ottengo le porte e le loro corispettive posizioni
        foreach (Door door in doors)
        {
            Matrix4x4 childToPoint = door.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMatrix = pointToWorldMtx * childToPoint;
            Vector3 position = childToWorldMatrix.GetColumn(3);
            position = new Vector3(position.x, position.y, position.z);

            //faccio un overlap sphere
            Collider[] hitColliders = Physics.OverlapSphere(position, 3);
            foreach (var hitCollider in hitColliders)
            {
                GameObject objHit = hitCollider.gameObject;

                if (objHit.GetComponent<Door>())
                {
                    Vector3 directionAB = position - objHit.transform.position;
                    Vector3 directionBA = objHit.transform.position - position;
                    float dotProduct = Vector3.Dot(directionAB.normalized, directionBA.normalized);
                    Debug.Log(dotProduct);
                    if (dotProduct < 0)
                    {
                        //Debug.Log("Posso snappare");
                        drawPosition = SnapRoom(position, (hit.point + hit.normal * prefabSize.y / 2) , objHit);
                        pointToWorldMtx = Matrix4x4.TRS(drawPosition, rotationMesh, Vector3.one);
                    }

                }

            }

        }
        //disegno la stanza
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
