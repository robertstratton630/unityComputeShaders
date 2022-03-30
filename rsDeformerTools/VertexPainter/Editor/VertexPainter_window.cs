using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Deformer
{
    public class Pair<T, U>
    {
        public Pair()
        {
        }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };


    public class VertexPainter_window : EditorWindow
    {
        #region Variables
        GUIStyle boxStyle;
        public bool allowPainting = false;
        public bool brushValueUpdating = false;
        public bool isPainting = false;
        public bool shiftKeyDown = false;

        public Vector2 mousePos = Vector2.zero;
        public RaycastHit rayHit;

        // brush settings
        public float brushSize = 1.0f;
        public float brushStrength = 1.0f;
        public float brushSoftness = 1.0f;



        public List<Pair<string, string>> deformerWeightHash;

        // geo settings
        public GameObject labelObj;
        public GameObject currentObj;
        public MeshDescription currentObjDescription;
        public Weightmap currentWeightmap;
        public string currentWeightmapName;
        public int selectedWeightmap;
        public Mesh currentMesh;

        public MeshTopologyHalfEdge topo;

        public Material origMaterial;

        // colors
        public static Color brushColor;
        public int preEnablePaintLayer;

        bool showVertIDs;

        // toolbar
        int toolbarInt = 0;
        string[] toolBarStrings = { "Add", "Replace", "Scale" };
        int lastWeightmap = -1;

        public bool[] processedColor = new bool[0];
        public float[] oldWeights = new float[0];
        float totalWeight;
        int totalCount;

        public List<GameObject> preEnablePaintObjs = new List<GameObject>();

        public static int _paintLayer = -1;
        #endregion

        #region Main Method
        static public void LaunchVertexPainter()
        {
            var win = EditorWindow.GetWindow<VertexPainter_window>(
            false, "Topology Tools", true);
            win.GenerateStyles();
            brushColor = Color.white;
            win.ResetData();
        }




        void ActivatePainting()
        {
            // we are just entering
            if (allowPainting)
            {

                if (Selection.objects != null)
                {
                    if (Selection.objects.Length > 1)
                    {
                        Debug.Log("Please Select One Mesh");
                        return;
                    }

                    // cache out the pre painting layer
                    currentObj = Selection.gameObjects[0];
                    currentMesh = MeshUtils.GetMesh(currentObj);

                    preEnablePaintLayer = currentObj.layer;

                    // swap the layer
                    currentObj.layer = _paintLayer;

                    // create mesh description
                    currentObjDescription = MeshUtils.CreateMeshDescription(currentObj);


                    // set material to vertex painter
                    origMaterial = currentObj.GetComponent<MeshRenderer>().sharedMaterial;
                    currentObj.GetComponent<MeshRenderer>().sharedMaterial = VPaintUtils.GetVertexPaintMatertial();

                    // set the default weightmap to envelope
                    selectedWeightmap = 0;

                    processedColor = new bool[currentMesh.vertexCount];

                    topo = new MeshTopologyHalfEdge(currentMesh, 1);
                }
            }
            else
            {
                Debug.Log("Deactivating Paint Mode");
                ResetData();
            }
        }



        void ResetData()
        {
            // reset default settings on mesh

            if (currentObjDescription) EditorUtility.SetDirty(currentObjDescription);

            if (currentObj)
            {
                currentObj.GetComponent<Renderer>().sharedMaterial = origMaterial;
                currentObj.layer = preEnablePaintLayer;
            }

            currentObj = null;
            currentObjDescription = null;
            currentWeightmap = null;
            currentWeightmapName = string.Empty;
            selectedWeightmap = -1;
            currentMesh = null;

            if (deformerWeightHash != null) deformerWeightHash.Clear();
        }


        void OnDestory()
        {
            ResetData();
            Debug.Log("Closed Window");
        }

        void OnEnable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            ResetData();

        }
        void OnDisable()
        {
            ResetData();
            SceneView.duringSceneGui -= OnSceneGUI;
        }


        #endregion

        #region Gui Methods
        private void OnSceneGUI(SceneView sceneView)
        {
            DrawVertexIDs();

            //Handles.Label(Vector3.zero, "this is a label");
            if (currentMesh == null) return;
            if (currentWeightmap == null) return;
            currentMesh.colors = currentWeightmap.vertexColor;
            SceneView.RepaintAll();

            ManageActiveTool();
            ManageRaycast();
            ManageBrushSettings();
            ManagePainting();

            // get user inputs
            ProcessInputs();

            // repaint scene view gui
            sceneView.Repaint();
        }


        void DrawVertexIDs()
        {
            if (labelObj != null)
            {
                Vector3[] verts = Utils.GetMesh(labelObj).GetMeshVertices(labelObj.transform);
                for (int i = 0; i < Utils.GetMesh(labelObj).vertexCount; i++)
                {
                    Handles.color = new Color(0f, 0f, 0f, 1.0f);
                    Handles.Label(verts[i], i.ToString());
                }
            }
        }


        void ManageActiveTool()
        {
            if (allowPainting)
            {
                Tools.current = Tool.None;
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
        }

        void ManageBrushSettings()
        {
            if (allowPainting)
            {
                if (rayHit.transform != null)
                {
                    Handles.color = new Color(1f, 0f, 0f, 1.0f);
                    Handles.DrawWireDisc(rayHit.point, rayHit.normal, brushSize, 3f);

                    Handles.color = new Color(0f, 1f, 0f, 1f);
                    Handles.DrawLine(rayHit.point, rayHit.point + rayHit.normal, 3f);
                }
            }
        }

        void ManageRaycast()
        {
            if (allowPainting)
            {
                if (!brushValueUpdating)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
                    Physics.Raycast(ray, out rayHit, float.MaxValue, (1 << _paintLayer));
                }
            }
        }

        void ManagePainting()
        {
            if (isPainting && allowPainting)
                PaintMesh();
        }





        void PaintMesh()
        {
            if (currentObj == null) return;

            Vector3[] verts = currentMesh.vertices;
            totalWeight = 0.0f;
            totalCount = 0;

            for (int i = 0; i < verts.Length; i++)
            {

                // check if in brush radius
                Vector3 vertWS = currentObj.transform.TransformPoint(verts[i]);
                float sq = (vertWS - rayHit.point).magnitude;
                if (sq > brushSize)
                {
                    continue;
                }

                // for smoothing
                if (shiftKeyDown)
                {
                    totalWeight += currentWeightmap.weights[i];
                    totalCount++;
                    continue;
                }

                if (processedColor[i] == true) continue;

                float falloff = (1.0f - (sq / brushSize)) * brushStrength;
                falloff = Mathf.Lerp(brushStrength, falloff, brushSoftness);

                // add
                if (toolbarInt == 0)
                {
                    // add weights to scriptobject
                    VPaintUtils.Brush_Add(i, falloff, currentWeightmap);

                    // use cache weights to figure out if max weight is reached
                    processedColor[i] = currentWeightmap.weights[i] > currentWeightmap.cacheWeights[i] + brushStrength ? true : false;
                }
                // replace
                else if (toolbarInt == 1)
                {
                    VPaintUtils.Brush_Replace(i, brushStrength, currentWeightmap);
                    processedColor[i] = true;
                }
                // scale
                else
                {
                    VPaintUtils.Brush_Scale(i, brushStrength, currentWeightmap);
                    processedColor[i] = true;
                }
            }

            if (shiftKeyDown)
            {
                totalWeight = totalWeight / (float)totalCount;
                for (int i = 0; i < verts.Length; i++)
                {
                    if (processedColor[i]) continue;

                    Vector3 vertWS = currentObj.transform.TransformPoint(verts[i]);
                    float sqMag = (vertWS - rayHit.point).sqrMagnitude;
                    if (sqMag > brushSize)
                    {
                        continue;
                    }

                    int[] ConnectedVerts = topo.GetConnectedVerts(i);

                    float weight = (float)currentWeightmap.weights[i];
                    for (int j = 0; j < ConnectedVerts.Length; j++)
                    {
                        weight += currentWeightmap.weights[ConnectedVerts[j]];
                    }
                    weight = weight / (float)ConnectedVerts.Length;

                    currentWeightmap.weights[i] = weight;
                    currentWeightmap.vertexColor[i] = new Color(weight, weight, weight, 1);
                    processedColor[i] = true;
                }
            }

            currentMesh.colors = currentWeightmap.vertexColor;
        }





        void UpdateBrushSize(float value)
        {
            brushSize = value;
            brushSize = Mathf.Max(0.1f, brushSize);
        }

        void UpdateBrushStrength(float value)
        {
            brushStrength = value;
            brushStrength = Mathf.Clamp01(brushStrength);
        }

        void UpdateBrushSoftness(float value)
        {
            brushSoftness = value;
            brushSoftness = Mathf.Clamp01(brushSoftness);
        }


        void FloodSelectedMeshes()
        {
            Debug.Log("Flushing " + currentWeightmap.name + " to Value : " + brushStrength);
            VPaintUtils.Brush_Flood(brushStrength, currentWeightmap);
            currentWeightmap.CacheWeights();
            currentMesh.colors = currentWeightmap.GetVertexColorMap();
            currentMesh.UploadMeshData(false);
            SceneView.RepaintAll();
        }



        private void SceneViewButtons()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Button("button");
            GUILayout.EndArea();
            Handles.EndGUI();
        }



        private void CacheWeights()
        {
            currentObjDescription.CacheAllWeightmaps();
        }


        private void OnGUI()
        {
            GenerateStyles();
            _paintLayer = LayerMask.NameToLayer("Paint");

            // header
            GUILayout.BeginHorizontal();
            GUILayout.Box("RS Vertex Painter", boxStyle, GUILayout.Height(40), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            bool old = allowPainting;
            bool update = GUILayout.Toggle(allowPainting, "Enable PaintMode", GUI.skin.button, GUILayout.Height(60));
            if (old != update)
            {
                if (update == true && Selection.objects.Length == 0)
                {
                    Debug.Log("Please Select a Mesh to Activate");
                    update = false;
                }

                allowPainting = update;
                ActivatePainting();
            }

            showVertIDs = GUILayout.Toggle(showVertIDs, "ShowVertIDs", GUI.skin.button, GUILayout.Height(60));
            if (showVertIDs && Selection.objects.Length != 0)
            {
                labelObj = (GameObject)Selection.objects[0];
            }
            else
            {
                labelObj = null;
            }


            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(20);
            if (currentObj != null)
            {

                List<string> uiNames = new List<string>();
                if (deformerWeightHash == null) deformerWeightHash = new List<Pair<string, string>>();
                foreach (DeformerData def in currentObjDescription.DeformerList)
                {
                    foreach (Weightmap w in def.Weightmaps)
                    {
                        Pair<string, string> info = new Pair<string, string>(def.name, w.name);
                        deformerWeightHash.Add(info);

                        uiNames.Add(def.name + "_" + w.name);
                    }
                }

                selectedWeightmap = GUILayout.SelectionGrid(selectedWeightmap, uiNames.ToArray(), 1, GUILayout.Height(65));

                if (lastWeightmap != selectedWeightmap)
                {
                    lastWeightmap = selectedWeightmap;
                    currentWeightmap = currentObjDescription.GetWeightmap(deformerWeightHash[selectedWeightmap].First, deformerWeightHash[selectedWeightmap].Second);
                    currentMesh.colors = currentWeightmap.GetVertexColorMap();
                    SceneView.RepaintAll();
                }
            }
            GUILayout.Space(20);
            GUILayout.EndVertical();


            // body
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Brush Mode");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            toolbarInt = GUILayout.Toolbar(toolbarInt, toolBarStrings, GUILayout.Height(40));
            GUILayout.EndHorizontal();



            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Brush Settings");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();



            GUILayout.BeginHorizontal();
            GUILayout.Label("Brush Strength");
            brushStrength = EditorGUILayout.Slider(brushStrength, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Brush Size");
            brushSize = EditorGUILayout.Slider(brushSize, 0.1f, 10.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Brush Softness");
            brushSoftness = EditorGUILayout.Slider(brushSoftness, 0.1f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            if (GUILayout.Button("Flood", GUI.skin.button, GUILayout.Height(40)))
            {
                FloodSelectedMeshes();
            }
            GUILayout.Space(20);
            GUILayout.EndVertical();

            // updates the UI in editor
            Repaint();
        }
        #endregion

        #region Utility Methods
        void GenerateStyles()
        {
            boxStyle = new GUIStyle();
            boxStyle.normal.textColor = Color.white;
            boxStyle.font = (Font)Resources.Load("Fonts/Prototype");
            boxStyle.fontSize = 25;
            boxStyle.margin = new RectOffset(2, 2, 2, 2);
            boxStyle.border = new RectOffset(3, 3, 3, 3);
            boxStyle.alignment = TextAnchor.MiddleCenter;
        }



        bool bKeyDown = false;
        bool vKeyDown = false;
        bool cKeyDown = false;
        void ProcessInputs()
        {
            Event e = Event.current;
            mousePos = e.mousePosition;


            shiftKeyDown = e.shift;

            // key press down
            if (e.type == EventType.KeyDown)
            {
                if (e.isKey)
                {
                    if (e.keyCode == KeyCode.B)
                    {
                        bKeyDown = true;
                        brushValueUpdating = true;
                    }
                    if (e.keyCode == KeyCode.V)
                    {
                        vKeyDown = true;
                        brushValueUpdating = true;
                    }
                    if (e.keyCode == KeyCode.C)
                    {
                        cKeyDown = true;
                        brushValueUpdating = true;
                    }
                    if (e.keyCode == KeyCode.T)
                    {
                        allowPainting = !allowPainting;
                        ActivatePainting();
                    }
                }
            }

            if (e.type == EventType.KeyUp)
            {
                if (e.isKey)
                {
                    if (e.keyCode == KeyCode.B)
                    {
                        bKeyDown = false;
                    }
                    if (e.keyCode == KeyCode.V)
                    {
                        vKeyDown = false;
                    }
                    if (e.keyCode == KeyCode.C)
                    {
                        cKeyDown = false;
                    }

                }
            }

            // cache weights just before painting and dragging
            if (e.type == EventType.MouseDown && !isPainting)
            {
                if (e.button == 0)
                {
                    CacheWeights();
                }
            }

            // brush key combinations
            if (allowPainting)
            {

                if (e.type == EventType.MouseUp || e.alt)
                {
                    brushValueUpdating = false;
                    isPainting = false;

                    for (int i = 0; i < processedColor.Length; i++)
                    {
                        processedColor[i] = false;
                    }
                }
                else
                {
                    if ((bKeyDown && e.button == 0) || (vKeyDown && e.button == 0))
                    {
                        brushValueUpdating = true;
                    }


                    if (e.type == EventType.MouseDrag &&
                                bKeyDown &&
                                e.button == 0)
                    {
                        float v = e.delta.x * 0.01f + brushSize;
                        UpdateBrushSize(v);
                        brushValueUpdating = true;
                    }

                    if (e.type == EventType.MouseDrag &&
                                vKeyDown &&
                                e.button == 0)
                    {
                        float v = e.delta.x * 0.001f + brushStrength;
                        UpdateBrushStrength(v);
                        brushValueUpdating = true;
                    }

                    if (e.type == EventType.MouseDrag &&
                                cKeyDown &&
                                e.button == 0)
                    {
                        brushValueUpdating = true;
                    }

                    if (e.type == EventType.MouseDrag &&
                                !cKeyDown && !vKeyDown && !bKeyDown &&
                                e.button == 0)
                    {
                        isPainting = true;
                    }
                }

            }

            #endregion
        }
    }
}