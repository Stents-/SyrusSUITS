﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayManager : MonoBehaviour {

    private static OverlayManager _Instance;
    public static OverlayManager Instance {
        get {
            if (_Instance == null) {
                _Instance = FindObjectOfType<OverlayManager>();
            }
            return _Instance;
        }
    }

    string path;                    // Directory where the overlay JSON files are located

    List<string> overlayFiles;      // List of overlay file names
    List<string> overlayNames;      // List of overlay names

    Layout layout = null;           // The currently loaded layout
    
    List<GameObject> objs = new List<GameObject>();  // Module game objects
    List<Prompt> loadedPrompts = new List<Prompt>(); // List of loaded prompts

    Step currentStep;
    public delegate void OverlayCreated();
    public static event OverlayCreated OnOverlayCreated;

    Color modColor;

    void Awake() {
        _Instance = this;
    }

    // Use this for initialization
    void Start() {
        modColor = new Color(1.0f, 1.0f, 1.0f, 1.0f / 4.0f);

        ProcedureManager.OnStepChanged += OnStepChanged;
        path = Application.streamingAssetsPath + "/OverlayLayouts/";

        PreloadOverlays();
    }

    void PreloadOverlays() {
        overlayFiles = new List<string>();
        overlayNames = new List<string>();

        try {
            foreach (string file in System.IO.Directory.GetFiles(path)) {
                string label = file.Replace(path, "");
                if (label.EndsWith(".json")) {
                    string contents = System.IO.File.ReadAllText(path + label);
                    OverlayPreload preload = JsonUtility.FromJson<OverlayPreload>(contents);

                    if (preload.activator) {
                        //CreateCalibrator(label);
                    }

                    overlayNames.Add(preload.name);
                    overlayFiles.Add(label);
                }
            }
        } catch (System.Exception ex) {
            Debug.Log("Error: JSON input. " + ex.Message);
        }
    }

    void CreateCalibrator(string overlayName) {
        // Doesnt work for some reason
        GameObject obj = (GameObject)Instantiate(Resources.Load("OverlayCalib"));
        obj.name = overlayName + "_Calibrator";
		OverlayCalibrator calib = obj.GetComponent<OverlayCalibrator>();
		calib.overlayName = overlayName;
    }

    void BoxMode(GameObject obj) {
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        lr.enabled = true;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        mr.enabled = false;
    }

    void SolidMode(GameObject obj) {
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        lr.enabled = false;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        mr.enabled = true;
    }

    public void LoadOverlay(string fileName, Vector3 pos, Quaternion rot) {
        // Check if this overlay is already loaded
        if (layout != null && layout.fileName == fileName) {
            return;
        }

        try {
            string dir = path + fileName;
            if (System.IO.File.Exists(dir)) { // Check if the file exists
                string contents = System.IO.File.ReadAllText(dir);

                // If there was a previous layout loaded, unload it
                if (layout != null) {
                    foreach (GameObject obj in objs) {
                        Destroy(obj);
                    }
                    objs.Clear();
                }

                layout = JsonUtility.FromJson<Layout>(contents); // Load from JSON
                layout.fileName = fileName; // Set the file name
                CreateTaskboard(pos, rot);

                if (layout.autoLoadProcedure) {
                    ProcedureManager.Instance.LoadProcedure(layout.procedureFileName);
                }

            } else {
                Debug.Log("Error: Unable to read " + fileName + " file, at " + dir);
            }
        }
        catch (System.Exception ex) {
            Debug.Log("Error: LoadOverlay: " + ex.Message);
        }
    }

    //Create the Taskboard
    void CreateTaskboard(Vector3 pos, Quaternion rot) {
        Vector3 corner = new Vector3(-layout.size.x / 2.0f, layout.size.y / 2.0f, layout.size.z / 2.0f);

        transform.localScale = new Vector3(layout.scale, layout.scale, layout.scale);

        transform.position = pos - rot * (corner + new Vector3(layout.activator_pos.x, layout.activator_pos.y, -layout.activator_pos.z)
                             + layout.activator_size.UnityVec() / 2.0f);
        transform.rotation = rot;

        GameObject cube;
        Material mat = Resources.Load("ModuleMat", typeof(Material)) as Material;

        foreach (Modules m in layout.modules) {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = transform;
            cube.name = m.id;
            cube.transform.localScale = new Vector3(m.size.x, m.size.y, m.size.z);
            Vector3 halfScle = new Vector3(m.size.x / 2, m.size.y / 2, -m.size.z / 2);
            cube.transform.localPosition = corner + new Vector3(m.position.x, m.position.y, -m.position.z) + halfScle;
            cube.transform.localRotation = Quaternion.Euler(m.rotation.UnityVec());

            cube.GetComponent<Renderer>().material = mat;

            // Create the line renderer
            LineRenderer lr = cube.AddComponent<LineRenderer>();
            Vector3[] pts = new Vector3[6];
            
            pts[0] = new Vector3(0.25f, 0.0f, 0.5f);
            pts[1] = new Vector3(0.5f, 0.0f, 0.5f);
            pts[2] = new Vector3(0.5f, 0.0f, -0.5f);
            pts[3] = new Vector3(-0.5f, 0.0f, -0.5f);
            pts[4] = new Vector3(-0.5f, 0.0f, 0.5f);
            pts[5] = new Vector3(0.25f, 0.0f, 0.5f);

            lr.positionCount = 6;
            lr.SetPositions(pts);
            lr.widthMultiplier = 0.005f;
            lr.enabled = false;
            lr.useWorldSpace = false;
            lr.numCornerVertices = 3;

            BoxMode(cube);
            cube.SetActive(false);

            objs.Add(cube);
        }
    }

    // Return the layout
    public Layout getLayout() {
        return layout;
    }

    public Vector3 getPanelPosition() {
        if (layout != null) {
            return transform.position + transform.rotation * new Vector3(layout.panel_pos.x, layout.panel_pos.y, layout.panel_pos.z);
        } else {
            return Vector3.zero;
        }
    }

    public Quaternion getPanelRotation() {
        if (layout != null) {
            return Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(layout.panel_rot.x, layout.panel_rot.y, layout.panel_rot.z);
        } else {
            return Quaternion.identity;
        }
    }

    #region old

    //Create the Taskboard
    void CreateTaskboard()
    {
        Vector3 corner = new Vector3(-(float)layout.size.x / 2.0f, (float)layout.size.y / 2.0f, (float)layout.size.z / 2.0f);
        GameObject cube;
        //Material mat = Resources.Load("ModuleMat", typeof(Material)) as Material;

        // whole taskboard
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.parent = transform;
        cube.name = "all";
        cube.transform.localScale = new Vector3((float)layout.size.x, (float)layout.size.y, (float)layout.size.z);
        cube.transform.localPosition = Vector3.zero;

        foreach (Modules m in layout.modules)
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = transform;
            cube.name = m.id;
            cube.transform.localScale = new Vector3((float)m.size.x, (float)m.size.y, (float)m.size.z);
            Vector3 halfScle = new Vector3((float)m.size.x / 2, (float)m.size.y / 2, -(float)m.size.z / 2);
            cube.transform.localPosition = corner + new Vector3((float)m.position.x, (float)m.position.y, -(float)m.position.z) + halfScle;

            cube.transform.localRotation = Quaternion.Euler(m.rotation.UnityVec());

  
            objs.Add(cube);
        }
        transform.localPosition = new Vector3(0, 0, 0.55f);

        //Transform panel = transform.Find("ProcedurePanel");
        //panel.localPosition = new Vector3(0.0f, 0.11f, layout.length / 2.0f + 0.00f);
        //moveTaskboard();
    }

    // Load the taskboard
    void LoadTaskboard(int x)
    {
        try
        {
            string temp = path + overlayFiles[x];
            if (System.IO.File.Exists(temp))
            {
                string contents = System.IO.File.ReadAllText(temp);
                Instance.layout = JsonUtility.FromJson<Layout>(contents);
                CreateTaskboard(Vector3.zero, Quaternion.identity);
                this.GetComponent<PlacingProcedure>().PlacingProcedureOn();
                this.GetComponent<PlacingProcedure>().m_methodToCall = GameObject.Find("ProcedureManager").gameObject.GetComponent<ProcedureManager>().ChooseProcedure;
            }
            else
            {
                Debug.Log("Error: Unable to read " + overlayFiles[x] + " file, at " + temp);
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log("Error: Taskboard JSON input. " + ex.Message);
        }
    }

    //Choose a Taskboard
    public void ChooseTaskboard()
    {

        OptionsMenu opts = OptionsMenu.Instance("Choose A Taskboard", true);
        opts.OnSelection += LoadTaskboard;
        if (overlayNames.Count > 0)
        {
            for (int i = 0; i < overlayNames.Count; i++)
            {
                opts.AddItem(overlayNames[i], i);
            }
            opts.ResizeOptions();
        }
        else
        {
            Debug.Log("Error: No Overlay layouts loaded");
        }
    }

    #endregion

    #region PromptLoading

    private void LoadArrow(Prompt prompt) {
        Transform module1 = transform.Find(prompt.moduleID);
        Transform module2 = transform.Find(prompt.misc);

        if (module1 != null && module2 != null)
        {
            module1.gameObject.SetActive(true);
            module2.gameObject.SetActive(true);
            GameObject arrowObj = new GameObject();
            arrowObj.name = "Arrow " + prompt.moduleID + " to " + prompt.misc;
            arrowObj.transform.SetParent(transform);
            Arrow arrow = arrowObj.AddComponent<Arrow>();
            arrow.beg = module1.position;
            arrow.end = module2.position;
            arrow.transform.position = arrow.transform.position + new Vector3(0.0f, 0.01f, 0.0f);
        }
        else
        {
            //Debug.LogError("Step #" + step.number + ": Couldn't find module by ID");
        }
    }

    private void LoadPush(Prompt prompt) {
        Transform module = transform.Find(prompt.moduleID);

        if (module != null)
        {
            module.gameObject.SetActive(true);
            GameObject arrowObj = new GameObject();
            arrowObj.name = "Push " + prompt.moduleID;
            arrowObj.transform.SetParent(transform);
            arrowObj.transform.position = module.position;
            PushArrow arrow = arrowObj.AddComponent<PushArrow>();
            arrow.end = 0.01f;
            arrow.beg = 0.05f;
            arrow.transform.position = arrow.transform.position + new Vector3(0.0f, 0.01f, 0.0f);
        }
        else
        {
            //Debug.LogError("Step #" + step.number + ": Couldn't find module by ID");
        }
    }

    private void LoadPull(Prompt prompt) {
        Transform module = transform.Find(prompt.moduleID);

        if (module != null)
        {
            module.gameObject.SetActive(true);
            GameObject arrowObj = new GameObject();
            arrowObj.name = "Pull " + prompt.moduleID;
            arrowObj.transform.SetParent(transform);
            arrowObj.transform.position = module.position;
            PushArrow arrow = arrowObj.AddComponent<PushArrow>();
            arrow.end = 0.05f;
            arrow.beg = 0.01f;
            arrow.transform.position = arrow.transform.position + new Vector3(0.0f, 0.01f, 0.0f);
        }
        else
        {
            //Debug.LogError("Step #" + step.number + ": Couldn't find module by ID");
        }
    }

    private void LoadHighlight(Prompt prompt) {
        Transform module = transform.Find(prompt.moduleID);
        if (module != null)
        {
            module.gameObject.SetActive(true);
            Color propCol = Color.green;
            propCol.a = 1.0f / 4.0f;
            module.gameObject.GetComponent<Renderer>().material.color = propCol;
        }
        else
        {
            //Debug.LogError("Step #" + step.number + ": Couldn't find module by ID");
        }
    }

    private void LoadCircle(Prompt prompt) {
        Transform module = transform.Find(prompt.moduleID);

        if (module != null)
        {
            module.gameObject.SetActive(true);
            float dirMod = 1.0f;
            if (prompt.misc == "clockwise") dirMod = 1.0f;
            if (prompt.misc == "counterclockwise") dirMod = -1.0f;

            GameObject circleObj = new GameObject();
            circleObj.name = "Circle " + prompt.moduleID;
            circleObj.transform.SetParent(transform);
            circleObj.AddComponent<Circle>().speed = dirMod * 45.0f;

            Mesh mesh = Resources.Load("Circle", typeof(Mesh)) as Mesh;
            Material mat = Resources.Load("PromptMat", typeof(Material)) as Material;
            circleObj.AddComponent<MeshRenderer>().material = mat;
            circleObj.AddComponent<MeshFilter>().mesh = mesh;


            // Find the smallest dimension of the module
            float smallestDim = module.localScale.x;
            if (module.localScale.z < smallestDim) smallestDim = module.localScale.z;


            circleObj.transform.localScale = new Vector3(dirMod * smallestDim, smallestDim, smallestDim);
            circleObj.transform.position = module.position + new Vector3(0.0f, 0.01f, 0.0f);
        }
        else
        {
            //Debug.LogError("Step #" + step.number + ": Couldn't find module by ID");
        }
    }

    #endregion

    #region PromptClearing

    private void ClearArrow(Prompt prompt) {
        Transform module1 = transform.Find(prompt.moduleID);
        Transform module2 = transform.Find(prompt.misc);

        if (module1 != null && module2 != null)
        {
            module1.gameObject.SetActive(false);
            module2.gameObject.SetActive(false);
        }
        Transform arrow = transform.Find("Arrow " + prompt.moduleID + " to " + prompt.misc);
        if (arrow != null)
        {
            Destroy(arrow.gameObject);
        }
        else
        {
            Debug.LogError("Step #" + currentStep.number + ": Couldn't find arrow for deletion");
        }
    }

    private void ClearPush(Prompt prompt) {
        Transform arrow = transform.Find("Push " + prompt.moduleID);

        Transform module = transform.Find(prompt.moduleID);
        if (module != null)
        {
            module.gameObject.SetActive(false);
        }

        if (arrow != null)
        {
            Destroy(arrow.gameObject);
        }
        else
        {
            Debug.LogError("Step #" + currentStep.number + ": Couldn't find push arrow for deletion");
        }
    }

    private void ClearPull(Prompt prompt) {
        Transform arrow = transform.Find("Pull " + prompt.moduleID);
                                
        Transform module = transform.Find(prompt.moduleID);
        if (module != null)
        {
            module.gameObject.SetActive(false);
        }

        if (arrow != null)
        {
            Destroy(arrow.gameObject);
        }
        else
        {
            Debug.LogError("Step #" + currentStep.number + ": Couldn't find pull arrow for deletion");
        }
    }

    private void ClearHighlight(Prompt prompt) {
        Transform module = transform.Find(prompt.moduleID);
        if (module != null)
        {
            module.gameObject.GetComponent<Renderer>().material.color = modColor;
            module.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Step #" + currentStep.number + ": Couldn't find module by ID " + prompt.moduleID + " for unhighlighting");
        }
    }

    private void ClearCircle(Prompt prompt) {
        Transform circle = transform.Find("Circle " + prompt.moduleID);
        Transform module = transform.Find(prompt.moduleID);

        if (module != null)
        {
            module.gameObject.SetActive(false);
        }

        if (circle != null)
        {
            Destroy(circle.gameObject);
        }
        else
        {
            Debug.LogError("Step #" + currentStep.number + ": Couldn't find circle for deletion");
        }
    }

    #endregion

    #region Prompts

    private void LoadPrompt(Prompt prompt) {
        switch (prompt.type) {
            case "arrow": LoadArrow(prompt); break;
            case "push": LoadPush(prompt); break;
            case "pull": LoadPull(prompt); break;
            case "highlight": LoadHighlight(prompt); break;
            case "circle": LoadCircle(prompt); break;
        }
        loadedPrompts.Add(prompt);
    }

    private void ClearPrompt(Prompt prompt) {
        switch (prompt.type) {
            case "arrow": ClearArrow(prompt); break;
            case "push": ClearPush(prompt); break;
            case "pull": ClearPull(prompt); break;
            case "highlight": ClearHighlight(prompt); break;
            case "circle": ClearCircle(prompt); break;
        }
        //loadedPrompts.Add(prompt);
    }

    private void ClearPrompts() {
        foreach (Prompt prompt in loadedPrompts) {
            ClearPrompt(prompt);
        }
        loadedPrompts.Clear();
    }

    private void OnStepChanged(Step step) {

        // Clear the older prompts
        ClearPrompts();

        // Add each new prompt
        foreach (Prompt prompt in step.prompts) {
            LoadPrompt(prompt);
        }

        currentStep = step;
    }

    #endregion
}

//Overlay Layout
[System.Serializable]
public class Layout {
    public string fileName; // File name of layout, not loaded by JSON

    public string name;

    public bool activator;
    public Vec3 activator_pos;
    public Vec3 activator_size;

    public bool autoLoadProcedure;
    public string procedureFileName;
    
    public Vec3 panel_pos;
    public Vec3 panel_rot;
    
    public Vec3 size;
    public float scale;
    public List<Modules> modules = new List<Modules>();
}

[System.Serializable]
public class Modules {
    public string type;
    public string id;
    public Vec3 size;
    public Vec3 position;
    public Vec3 rotation;
}

[System.Serializable]
public class Vec2 {
    public double x;
    public double y;
}

[System.Serializable]
public class Vec3 {
    public float x;
    public float y;
    public float z;

    public Vector3 UnityVec() {
        return new Vector3(x, y, -z);
    }
}

[System.Serializable]
public class OverlayPreload { // Contains basic information about an overlay for preloading
    public string name;
    public bool activator;
    public Vec3 activator_pos;
    public Vec3 activator_size;
    public int activator_target;

    public string procedure;
}