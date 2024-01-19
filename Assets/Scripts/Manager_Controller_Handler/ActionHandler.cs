using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ActionHandler : MonoBehaviour {

    public event EventHandler OnLoaded;

    private ButtonActions buttonActions;
    private GameManager gameManager;
    [HideInInspector] public List<Text> textFields = new List<Text>();
    [HideInInspector] public Text headline; // Ref. to the textField where we will write the dialog headline.
    private Scene currentScene;
                     
    // Field to specify filename of the quest we want to get.
    [HideInInspector] public string AH_FileName;
    //[HideInInspector] public string centerDropScene = "_EmptyActionHandler";

    // Fields for JSON file.
    public bool autoActivate;  // Should the quest be auto-activated?
    public bool activate = false; // Toggle to act on the value of 'activateQuest'.
    public string AH_Headline;   // The string written at the top of the dialog panel.
    [TextArea] public string AH_Text0; // What the player needs to do.
    [TextArea] public string AH_Text1; // What the player needs to do.
    [TextArea] public string AH_Text2; // What the player needs to do.
    [TextArea] public string AH_Text3; // What the player needs to do.
    public string actor0Name;
    public string actor1Name;
    public string actor2Name;
    public string actor3Name;
    public string actor4Name;
    public string actorNameCamCheck;    // Actor to include in cam-view when it/she/he is close enough.
    public float actor0SpawnLocH;
    public float actor0SpawnLocV;
    public float actor1SpawnLocH;
    public float actor1SpawnLocV;
    public float actor2SpawnLocH;
    public float actor2SpawnLocV;
    public float actor3SpawnLocH;
    public float actor3SpawnLocV;
    public float actor4SpawnLocH;
    public float actor4SpawnLocV;
    public bool showUpperChoice;
    public bool showMiddleChoice;
    public bool showLowerChoice;
    public bool showYesAndNo;
    public bool showFwdAndBack;
    public bool showDone;
    // Button refs.
    Button upChoice;
    Button midChoice;
    Button lowChoice;
    Button yesChoice;
    Button noChoice;
    Button revChoice;
    Button fwdChoice;
    Button doneChoice;


    private void Awake() {
        //  Ref. to GameManager
        //  -----------------------------------------------------------------------------
        GameObject _instance = GameObject.Find("GameManager");
        if (_instance) {
            gameManager = _instance.GetComponent<GameManager>();
        }
        else Debug.Log(this.name + " : I didn't find GameObject GameManager");
        //  -----------------------------------------------------------------------------

        //  Ref. to ButtonScripts
        //  -----------------------------------------------------------------------------
        _instance = GameObject.Find("ButtonScripts");
        if (_instance)
        {
            buttonActions = _instance.GetComponent<ButtonActions>();
        }
        else Debug.Log(this.name + " : I didn't find ButtonScrips.");
        //  -----------------------------------------------------------------------------

        //  Ref. to text field.
        //  -----------------------------------------------------------------------------
        gameManager.actionHandler_Panel.SetActive(true);
        string lookFor = "Headline_text";
        _instance = GameObject.Find(lookFor);
        if (_instance) {
            headline = _instance.GetComponent<Text>();
        }
        else Debug.Log(this.name + " : I didn't find GameObject " + lookFor);
        //  -----------------------------------------------------------------------------

        //  Refs. to text fields and buttons.
        //  -----------------------------------------------------------------------------
        for (int i = 1; i < 5; i++) {
            _instance = GameObject.Find("ConvText" + (i));
            if (_instance) {
                textFields.Add(_instance.GetComponent<Text>());
            }
            else Debug.Log(this.name + " : I didn't find ConvText" + (i) + " GameObject");
        }
        upChoice = GameObject.Find("A-Button").GetComponent<Button>();
        midChoice = GameObject.Find("B-Button").GetComponent<Button>();
        lowChoice = GameObject.Find("C-Button").GetComponent<Button>();
        yesChoice = GameObject.Find("YesButton").GetComponent<Button>();
        noChoice = GameObject.Find("NoButton").GetComponent<Button>();
        revChoice = GameObject.Find("RevButton").GetComponent<Button>();
        fwdChoice = GameObject.Find("FwdButton").GetComponent<Button>();
        doneChoice = GameObject.Find("DoneButton").GetComponent<Button>();
        buttonActions = GameObject.Find("Text_Interaction").GetComponent<ButtonActions>();

        gameManager.actionHandler_Panel.SetActive(false);
        //  -----------------------------------------------------------------------------

        // When 'Awake' is being run, the ActionHandler sets the default-load-folder and loads the default data.
        // Then, when the user is modifying data, load-folder = save-folder, this is used for temporary data.
        // When satisfactory a temporary file can be moved manually into the folder with default data.
        // There it should be renamed to the filename of the ActionHandler, otherwise deafult data will not be loaded.
        // (First rename or delete corresponding old default file)
        SaveSystem.SetSaveFolder(SaveSystem.folder4);
        SaveSystem.SetLoadFolder(SaveSystem.folder4a);
        AH_FileName = name;

        LoadText_JSON(AH_FileName);
    }

    void Start() {
    }

    void FixedUpdate()  {
        currentScene = SceneManager.GetActiveScene();
        if (activate) {
            activate = false;
            HandleActors();
            if (!autoActivate) {
                HandlePanel();
                //Debug.Log("This is not an AutoActivator:" + name);
            }
            else {
                //Debug.Log("This is an AutoActivator:" + name);
            }
        }
    }
    
    private void Update() {
        if (Application.isEditor) {
            if (Input.GetKeyDown(KeyCode.N)) {
                SaveSystem.SetSaveFolder(SaveSystem.folder4);
                SaveSystem.SetLoadFolder(SaveSystem.folder4);
                SaveSystem.identifier = this.name;
                SaveSystem.dateTime = DateTime.Now.ToString();
                SaveText_JSON();
                Debug.Log(name + ": ActionHandler saved !");
            }
            if (Input.GetKeyDown(KeyCode.M)) {
                SaveSystem.SetSaveFolder(SaveSystem.folder4);
                SaveSystem.SetLoadFolder(SaveSystem.folder4);
                AH_FileName = "";
                LoadText_JSON(AH_FileName);
                Debug.Log(name + ": ActionHandler loaded !");
            }
        }
    }
    

    public void HandleActors() {
        // Init. (clearing text fields).
        headline.text = "";
        for (int i = 0; i < textFields.Count; i++) {
            textFields[i].text = ""; 
        }

        // Actors needed in the scene is added if not present.
        string[] actorNames = { actor0Name, actor1Name, actor2Name, actor3Name, actor4Name };
        float[] actorsSpawnLocsH = { actor0SpawnLocH, actor1SpawnLocH, actor2SpawnLocH, actor3SpawnLocH, actor4SpawnLocH };
        float[] actorsSpawnLocsV = { actor0SpawnLocV, actor1SpawnLocV, actor2SpawnLocV, actor3SpawnLocV, actor4SpawnLocV };
        for (int i = 0; i < 5; i++) {
            if (gameManager.actorRefByBaseTag.ContainsKey(actorNames[i])) {
                if (AH_FileName != "_EmptyActionHandler")
                {
                    gameManager.RelocateAndActivate_ActorInScene(gameManager.actorRefByBaseTag[actorNames[i]], actorsSpawnLocsH[i], actorsSpawnLocsV[i]);
                }
                if (!gameManager.allActorsInScene.Contains(gameManager.actorRefByBaseTag[actorNames[i]]))
                    gameManager.allActorsInScene.Add(gameManager.actorRefByBaseTag[actorNames[i]]);
            }
        }

        // Scenes where actors just drop.
        if (AH_FileName == "_EmptyActionHandler") gameManager.CenterDrop_Of_AllActorsInScene();

        // Adding to cam view if actor0 or actorNameCamCheck is present.
        if (gameManager.actorRefByBaseTag.ContainsKey(actor0Name)) 
            gameManager.actorsInCamView.Add(gameManager.actorRefByBaseTag[actor0Name].gameObject);
        if (gameManager.actorRefByBaseTag.ContainsKey(actorNameCamCheck))
            gameManager.actorsInCamView.Add(gameManager.actorRefByBaseTag[actorNameCamCheck].gameObject);

        // If the Avatar and/or a new actor, who needs to be in camera view, has entered the scene, then we set camera targets.
        // That means that actor1 and actor2 fields can freely be used to place actors in the scene.
        if (gameManager.actorRefByBaseTag.ContainsKey(actor0Name) ||
            gameManager.actorRefByBaseTag.ContainsKey(actorNameCamCheck)
           )
            gameManager.SetCameraTargets();
    }


    public void HandlePanel() {
        // Opening the panel for the player and changing mode.
        // Putting the Avatar in Conversation mode, this will disable grappleHook.
        // This is needed because the grappleHook will fire when the player uses the mouse on the panel buttons.
        gameManager.actionHandler_Panel.SetActive(true);
        gameManager.allActors[0].GetComponent<BaseIdentity>().myMode = BaseIdentity.Mode.Conversation;

        headline.text = AH_Headline;
        if (AH_Text0.Length > 0) textFields[0].text = AH_Text0;
        if (AH_Text1.Length > 0) textFields[1].text = AH_Text1;
        if (AH_Text2.Length > 0) textFields[2].text = AH_Text2;
        if (AH_Text3.Length > 0) textFields[3].text = AH_Text3;

        upChoice.gameObject.SetActive(showUpperChoice);
        midChoice.gameObject.SetActive(showMiddleChoice);
        lowChoice.gameObject.SetActive(showLowerChoice);
        yesChoice.gameObject.SetActive(showYesAndNo);
        noChoice.gameObject.SetActive(showYesAndNo);
        fwdChoice.gameObject.SetActive(showFwdAndBack);
        revChoice.gameObject.SetActive(showFwdAndBack);
        doneChoice.gameObject.SetActive(showDone);
    }


    public class SaveObject
    {
        public CustomObject.SaveObject[] saveObject_Array;
    }

    public void LoadText_JSON(string fileName)
    {
        SaveObject saveObject;
        if (fileName.Length == 0) saveObject = SaveSystem.LoadMostRecentObject<SaveObject>();
        else saveObject = SaveSystem.LoadObject<SaveObject>(fileName);

        foreach (CustomObject.SaveObject itr in saveObject.saveObject_Array)
        {
            this.autoActivate = itr.autoActivate;
            this.AH_Headline = itr.AH_Headline;
            this.AH_Text0 = itr.AH_Text0;
            this.AH_Text1 = itr.AH_Text1;
            this.AH_Text2 = itr.AH_Text2;
            this.AH_Text3 = itr.AH_Text3;
            this.actor0Name = itr.actor0Name;
            this.actor1Name = itr.actor1Name;
            this.actor2Name = itr.actor2Name;
            this.actor3Name = itr.actor3Name;
            this.actor4Name = itr.actor4Name;
            this.actorNameCamCheck = itr.actorNameCamCheck;
            this.actor0SpawnLocH = itr.actor0SpawnLocH;
            this.actor0SpawnLocV = itr.actor0SpawnLocV;
            this.actor1SpawnLocH = itr.actor1SpawnLocH;
            this.actor1SpawnLocV = itr.actor1SpawnLocV;
            this.actor2SpawnLocH = itr.actor2SpawnLocH;
            this.actor2SpawnLocV = itr.actor2SpawnLocV;
            this.actor3SpawnLocH = itr.actor3SpawnLocH;
            this.actor3SpawnLocV = itr.actor3SpawnLocV;
            this.actor4SpawnLocH = itr.actor4SpawnLocH;
            this.actor4SpawnLocV = itr.actor4SpawnLocV;
            this.showUpperChoice = itr.showUpperChoice;
            this.showMiddleChoice = itr.showMiddleChoice;
            this.showLowerChoice = itr.showLowerChoice;
            this.showYesAndNo = itr.showYesAndNo;
            this.showFwdAndBack = itr.showFwdAndBack;
            this.showDone = itr.showDone;
        }
        OnLoaded?.Invoke(this, EventArgs.Empty);
    }

    public void SaveText_JSON()
    {
        List<CustomObject.SaveObject> saveObject_List = new List<CustomObject.SaveObject>();
        CustomObject.SaveObject customObject_SaveObject = new CustomObject.SaveObject
        {
            autoActivate = autoActivate,
            AH_Headline = AH_Headline,
            AH_Text0 = AH_Text0,
            AH_Text1 = AH_Text1,
            AH_Text2 = AH_Text2,
            AH_Text3 = AH_Text3,
            actor0Name = actor0Name,
            actor1Name = actor1Name,
            actor2Name = actor2Name,
            actor3Name = actor3Name,
            actor4Name = actor4Name,
            actorNameCamCheck = actorNameCamCheck,
            actor0SpawnLocH = actor0SpawnLocH,
            actor0SpawnLocV = actor0SpawnLocV,
            actor1SpawnLocH = actor1SpawnLocH,
            actor1SpawnLocV = actor1SpawnLocV,
            actor2SpawnLocH = actor2SpawnLocH,
            actor2SpawnLocV = actor2SpawnLocV,
            actor3SpawnLocH = actor3SpawnLocH,
            actor3SpawnLocV = actor3SpawnLocV,
            actor4SpawnLocH = actor4SpawnLocH,
            actor4SpawnLocV = actor4SpawnLocV,
            showUpperChoice = showUpperChoice,
            showMiddleChoice = showMiddleChoice,
            showLowerChoice = showLowerChoice,
            showYesAndNo = showYesAndNo,
            showFwdAndBack = showFwdAndBack,
            showDone = showDone
        };
        saveObject_List.Add(customObject_SaveObject);
        SaveObject saveObject = new SaveObject { saveObject_Array = saveObject_List.ToArray() };
        SaveSystem.SaveObject(saveObject);
    }


    public class CustomObject
    {
        public bool autoActivate;
        public string AH_Headline;
        public string AH_Text0;
        public string AH_Text1;
        public string AH_Text2;
        public string AH_Text3;
        public string actor0Name;
        public string actor1Name;
        public string actor2Name;
        public string actor3Name;
        public string actor4Name;
        public string actorNameCamCheck;
        public float actor0SpawnLocH;
        public float actor0SpawnLocV;
        public float actor1SpawnLocH;
        public float actor1SpawnLocV;
        public float actor2SpawnLocH;
        public float actor2SpawnLocV;
        public float actor3SpawnLocH;
        public float actor3SpawnLocV;
        public float actor4SpawnLocH;
        public float actor4SpawnLocV;
        public bool showUpperChoice;
        public bool showMiddleChoice;
        public bool showLowerChoice;
        public bool showYesAndNo;
        public bool showFwdAndBack;
        public bool showDone;

        [System.Serializable]
        public class SaveObject
        {
            public bool autoActivate;
            public string AH_Headline;
            public string AH_Text0;
            public string AH_Text1;
            public string AH_Text2;
            public string AH_Text3;
            public string actor0Name;
            public string actor1Name;
            public string actor2Name;
            public string actor3Name;
            public string actor4Name;
            public string actorNameCamCheck;
            public float actor0SpawnLocH;
            public float actor0SpawnLocV;
            public float actor1SpawnLocH;
            public float actor1SpawnLocV;
            public float actor2SpawnLocH;
            public float actor2SpawnLocV;
            public float actor3SpawnLocH;
            public float actor3SpawnLocV;
            public float actor4SpawnLocH;
            public float actor4SpawnLocV;
            public bool showUpperChoice;
            public bool showMiddleChoice;
            public bool showLowerChoice;
            public bool showYesAndNo;
            public bool showFwdAndBack;
            public bool showDone;
        }

    }


}
