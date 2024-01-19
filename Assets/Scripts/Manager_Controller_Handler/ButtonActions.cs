using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonActions : MonoBehaviour {
    // References.
    private GameManager gameManager;
    private DialogueManager dialogueManager;
    private Text_Interaction text_Interaction;
    private List<Text> textFields = new List<Text>();
    public List<GameObject> ObjectsToMove = new List<GameObject>();
    // More refs. --- (Loading Panel)
    [HideInInspector] public GameObject sceneLoadingPanel;
    [HideInInspector] public Text sceneLoadingText;
    [HideInInspector] public Image loadingPanelImage;
    private Quest[] questList;

    public int pointer = 0; // Pointer which controls which lines are shown on the conversation panel.
    private bool textInteractionFound = false;
    public string activeActionHandler;

    // Data for returning from PCG.
    Stack<string> headOutSceneNames = new Stack<string>(); // Contains LIFO (stack of scene names).
    bool returning = false; // Is the player returning from a PCG scene or not;

    // Scene names as in build settings.
    [HideInInspector] public const string Loading_Scene_Name     = "0_LoadingScene";
    [HideInInspector] public const string Tutorial_Scene_Name    = "1_TutorialScene";
    [HideInInspector] public const string City_Scene_Name        = "2_CityScene";
    [HideInInspector] public const string Oracle_Scene_Name      = "3_OracleScene";
    [HideInInspector] public const string Camp_Scene_Name        = "4_CampScene";
    [HideInInspector] public const string PCG_Scene1_Name        = "5_PCG_SemiFullRandom_Scene";
    [HideInInspector] public const string PCG_Scene2_Name        = "6_PCG_TrueFullRandom_Scene";
    [HideInInspector] public const string Unused_Scene1_Name     = "7_UnusedScene1";
    [HideInInspector] public const string Unused_Scene2_Name     = "8_UnusedScene2";
    [HideInInspector] public const string CityOLD_Scene_Name     = "9_CityScene_OLD";
    [HideInInspector] public const string OracleOLD_Scene_Name   = "10_OracleScene_OLD";
    [HideInInspector] public const string Creation_Scene_Name    = "11_CreationScene";

    Dictionary<string, int> sceneNameToBuildID = new Dictionary<string, int>();
    void MakeBuildIDs() {
        sceneNameToBuildID.Add(Loading_Scene_Name, 0);
        sceneNameToBuildID.Add(Tutorial_Scene_Name, 1);
        sceneNameToBuildID.Add(City_Scene_Name, 2);
        sceneNameToBuildID.Add(Oracle_Scene_Name, 3);
        sceneNameToBuildID.Add(Camp_Scene_Name, 4);
        sceneNameToBuildID.Add(PCG_Scene1_Name, 5);
        sceneNameToBuildID.Add(PCG_Scene2_Name, 6);
        sceneNameToBuildID.Add(Unused_Scene1_Name, 7);
        sceneNameToBuildID.Add(Unused_Scene2_Name, 8);
        sceneNameToBuildID.Add(CityOLD_Scene_Name, 9);
        sceneNameToBuildID.Add(OracleOLD_Scene_Name, 10);
        sceneNameToBuildID.Add(Creation_Scene_Name, 11);
    }


    void Awake() {
        MakeBuildIDs();

        questList = Resources.LoadAll<Quest>("ScriptableObjects/QuestObjects");

        //  Ref. to GameManager
        //  -----------------------------------------------------------------------------
        GameObject _instance = GameObject.Find("GameManager");
        if (_instance) {
            gameManager = _instance.GetComponent<GameManager>();
        }
        else Debug.Log(this.name + " : I didn't find GameManager.");
        //  -----------------------------------------------------------------------------

        //  Ref. to DialogueManager
        //  -----------------------------------------------------------------------------
        _instance = GameObject.Find("DialogueManager");
        if (_instance)
        {
            dialogueManager = _instance.GetComponent<DialogueManager>();
        }
        else Debug.Log(this.name + " : I didn't find DialogueManager.");
        //  -----------------------------------------------------------------------------

        //  Ref. to scene loading panel
        //  -----------------------------------------------------------------------------
        _instance = GameObject.Find("SceneLoading_Panel");
        if (_instance)
        {
            sceneLoadingPanel = _instance;
            loadingPanelImage = sceneLoadingPanel.GetComponent<Image>();
        }
        else Debug.Log(this.name + " : I didn't find SceneLoading_Panel");
        //  -----------------------------------------------------------------------------

        //  Ref. to scene loading Text
        //  -----------------------------------------------------------------------------
        _instance = GameObject.Find("SceneLoadingText");
        if (_instance)
        {
            sceneLoadingText = _instance.GetComponent<Text>();
            sceneLoadingPanel.SetActive(false);
        }
        else Debug.Log(this.name + " : I didn't find SceneLoadingText");
        //  -----------------------------------------------------------------------------

        //  Refs. to text fields.
        //  -----------------------------------------------------------------------------
        gameManager.actionHandler_Panel.SetActive(true);
        for (int i = 1; i < 5; i++) {
            GameObject _TextField = GameObject.Find("ConvText" + (i));
            if (_TextField) {
                textFields.Add(_TextField.GetComponent<Text>());
            }
            else Debug.Log(this.name + " : I didn't find ConvText" + (i) + " GameObject");
        }
        gameManager.actionHandler_Panel.SetActive(false);
        //  -----------------------------------------------------------------------------
    }


    public void Start() {
    }

    public void FixedUpdate() {
        // If the panel is not active then THE PANEL IS RESET.
        // (It means the player has gone in and out of the panel)
        if (!gameManager.actionHandler_Panel.activeInHierarchy && textInteractionFound == true) {
            pointer = 0;
            for (int i = 0; i < textFields.Count - 1; i++) {
                textFields[i].text = "";
            }
            textInteractionFound = false;
        }
    }
    public void NewGame()
    {
        foreach (Quest q in questList)
        {            
            q.isActive = false;
            q.isCompleted = false;
        }
        StartCoroutine(Load(City_Scene_Name, "AH_CityScene_AutoActivator", 0, 0, 0, 0, gameManager.levelType, ""));
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quitting application");
    }

    public void StartGame() {
        StartCoroutine(Load(City_Scene_Name, "AH_CityScene_AutoActivator", 0, 0, 0, 0, gameManager.levelType, ""));
    }

    public void Load_Tutorial_Scene() {
        StartCoroutine(Load(Tutorial_Scene_Name, "AH_TutorialScene_AutoActivator", 0, 0, 0, 0, gameManager.levelType, ""));
    }

    public void Load_Camp_Scene() {
        StartCoroutine(Load(Camp_Scene_Name, "AH_CampScene_AutoActivator", 0, 0, 0, 0, gameManager.levelType, ""));
    }

    public void Load_Oracle_Scene() {
        StartCoroutine(Load(Oracle_Scene_Name, "AH_OracleScene_AutoActivator", 0, 0, 0, 0, gameManager.levelType, ""));
    }

    public void Load_SemiFullRandomPCG_Scene() {
        StartCoroutine(Load(PCG_Scene1_Name, "", 0, 0, 0, -1, Level.LevelType.SemiFullRandom_Level, ""));
    }

    public void Load_TrueFullRandomPCG_Scene() {
        StartCoroutine(Load(PCG_Scene2_Name, "", 0, 0, 0, -1, Level.LevelType.TrueFullRandom_Level, ""));
    }

    // =========================================================================================================

    public void Load_CityScene_OLD() {
        // When the scene loads, the CustomTileParent will attempt to load a tilemap, if one is given,
        // so folders are set before (in the CreationScene, the folders are default folders).
        SaveSystem.SetSaveFolder(SaveSystem.folder5);
        SaveSystem.SetLoadFolder(SaveSystem.folder6);
        StartCoroutine(Load(CityOLD_Scene_Name, "AH_CityScene_OLD_AutoActivator", 150, 80, 1, 56318018, Level.LevelType.Cellular_1, "CitySceneTilemap"));
    }

    public void Load_OracleScene_OLD() {
        StartCoroutine(Load(OracleOLD_Scene_Name, "AH_OracleScene_OLD_AutoActivator", 150, 80, 3, 75915206, Level.LevelType.Cellular_xNumLarge, ""));
    }


    // For Testing and Level Creation.
    // -------------------------------
    public void Load_CreationScene() {
        StartCoroutine(LoadScene(Creation_Scene_Name));
        activeActionHandler = "_EmptyActionHandler";
        gameManager.PrepareLevel(gameManager.customSeedToUse, gameManager.levelType, activeActionHandler);
        gameManager.customTilesFileName = "";
    }

    // =========================================================================================================

    // Scene loading handling method.
    IEnumerator Load(string sceneName, string activeAH, int levelSizeH, int levelSizeV, int fixedAreaNumber,
                 int customSeed, Level.LevelType levelType, string customTilesFileName)
    {
        yield return StartCoroutine(HideView());
        yield return StartCoroutine(LoadScene(sceneName));
        activeActionHandler = activeAH;         // Defining spawn of main actors and possible pop-up.
        gameManager.sizeH = levelSizeH;
        gameManager.sizeV = levelSizeV;
        gameManager.fixedAreaNumber = fixedAreaNumber;
        gameManager.PrepareLevel(customSeed, levelType, activeActionHandler);
        // Finally a custom tilemap is loaded if this string length > 0 
        gameManager.customTilesFileName = customTilesFileName;
        yield return StartCoroutine(ShowView());
    }


    // When pushed lower choice on AH-Panel, (Returning from random PCG).
    public void ReturnFromPCG() {
        returning = true;
        string oneSceneBack;
        if (headOutSceneNames.Count > 0) {
            oneSceneBack = headOutSceneNames.Pop();
            switch (oneSceneBack) {
                case Tutorial_Scene_Name:
                    // Load_Tutorial_Scene();
                    break;
                case City_Scene_Name:
                    // StartGame();
                    break;
                case Oracle_Scene_Name:
                    // Load_OracleScene();
                    break;
                case Camp_Scene_Name:
                    Load_Camp_Scene();
                    break;
                case PCG_Scene1_Name:
                    Load_SemiFullRandomPCG_Scene();
                    break;
                case PCG_Scene2_Name:
                    Load_TrueFullRandomPCG_Scene();
                    break;
                case Unused_Scene1_Name:

                    break;
                case Unused_Scene2_Name:

                    break;
                case CityOLD_Scene_Name:
                    Load_CityScene_OLD();
                    break;
                case OracleOLD_Scene_Name:
                    Load_OracleScene_OLD();
                    break;
                case Creation_Scene_Name:
                    Load_CreationScene();
                    break;
            }
        }
        else {
            Debug.Log("No scene names in stack: headOutSceneNames");
        }
    }


    public void AnswerYes()  {
        gameManager.actionHandler_Panel.SetActive(false);
        string currentSceneName = SceneManager.GetActiveScene().name;

        switch (currentSceneName) {
            case PCG_Scene1_Name:
                switch (activeActionHandler)
                {
                    case "AH_PCG_DoorAction_3":
                        gameManager.actorRefByBaseTag["PCG-DoorAction-3_P"].SetActive(false);
                        Load_Oracle_Scene();
                        break;
                }
                break;
            case PCG_Scene2_Name:
                switch (activeActionHandler)
                {
                    case "AH_PCG_DoorAction_3":
                        gameManager.actorRefByBaseTag["PCG-DoorAction-3_P"].SetActive(false);
                        Load_Oracle_Scene();
                        break;
                }
                break;
            case City_Scene_Name:
                switch (activeActionHandler) {
                    case "AH_CityScene_DoorAction_Exit_2":
                        Load_Tutorial_Scene();
                        break;
                    case "AH_CityScene_DoorAction_Exit_1":
                        break;
                    case "AH_CityScene_DoorAction_Exit_3":
                        break;
                }
                break;
            case Tutorial_Scene_Name:
                switch (activeActionHandler)
                {
                    case "AH_TutorialScene_DoorAction_Exit_1":
                        Load_Camp_Scene();
                        break;
                    case "AH_TutorialScene_DoorAction_Exit_2":
                        break;
                    case "AH_TutorialScene_DoorAction_Exit_3":
                        break;
                }
                break;
            case Oracle_Scene_Name:
                switch (activeActionHandler)
                {
                    case "AH_OracleScene_DoorAction_Exit_1":
                        Load_Camp_Scene();
                        break;
                    case "AH_TutorialScene_DoorAction_Exit_2":
                        break;
                    case "AH_TutorialScene_DoorAction_Exit_3":
                        break;
                }
                break;
            case CityOLD_Scene_Name:
                switch (activeActionHandler) {
                    case "_":
                        break;
                    case "AH_TutorialScene_DoorAction_Exit_1":
                        Load_Tutorial_Scene();
                        break;
                    case "AH_CityScene_DoorAction_Exit_3":
                        Load_SemiFullRandomPCG_Scene();
                        break;
                }
                break;
        }
    }


    public void DeactivatePanel() {
        gameManager.actionHandler_Panel.SetActive(false);
        gameManager.allActors[0].GetComponent<BaseIdentity>().myMode = gameManager.allActors[0].GetComponent<BaseIdentity>().myDefaultMode;
    }


    public void ConvForward() {
        //  Ref. to GameObject (which has Component Text_Interaction)
        //  If this is the first time the player use FORWARD button, a JSON file is loaded.
        //  -------------------------------------------------------------------------------
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.name != Loading_Scene_Name && textInteractionFound == false) {
            // Loading.
            string lookFor = "Text_Interaction";
            GameObject _instance = GameObject.Find(lookFor);
            if (_instance) {
                text_Interaction = _instance.GetComponent<Text_Interaction>();
                SaveSystem.SetLoadFolder(SaveSystem.folder2);
                text_Interaction.LoadText_JSON("OracleVision");
                textInteractionFound = true;
            }
            else Debug.Log(this.name + " : I didn't find GameObject " + lookFor);
        }
        //  --------------------------------------------------------------------------------
        if (pointer < text_Interaction.lines.Count) {
            // When moving forward and the page is still being filled.
            // Then textFields are only allowed to be filled when the pointer reaches their index.
            if (pointer < textFields.Count) {
                textFields[0].text = text_Interaction.lines[0];
                for (int i = 0; i < textFields.Count - 1; i++) {
                    if (pointer > i) textFields[i + 1].text = text_Interaction.lines[i + 1];
                }
            }
            // When the page is full, all text fields will inherit from the next one.
            // Except for the last field, which will need a new text.
            else {
                for (int i = 0; i < textFields.Count - 1; i++) {
                    textFields[i].text = textFields[i + 1].text;
                }
            }
            // When moving forward the last textField-index will always correspond to pointer-value.
            if (pointer > textFields.Count - 2) textFields[textFields.Count - 1].text = text_Interaction.lines[pointer];
            pointer++;
        }
    }


    public void ConvReverse() {
        //  Ref. to GameObject (which has Component Text_Interaction)
        //  If this is the first time the player use REVERSE button, a JSON file is loaded.
        //  -------------------------------------------------------------------------------
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != Loading_Scene_Name && textInteractionFound == false) {
            // Loading.
            string lookFor = "Text_Interaction";
            GameObject _instance = GameObject.Find(lookFor);
            if (_instance) {
                text_Interaction = _instance.GetComponent<Text_Interaction>();
                SaveSystem.SetLoadFolder(SaveSystem.folder2);
                text_Interaction.LoadText_JSON("OracleVision");
                textInteractionFound = true;
            }
            else Debug.Log(this.name + " : I didn't find GameObject " + lookFor);
        }
        //  -----------------------------------------------------------------------------
        // If the page is full, then the reverse button can be used.
        if (pointer > textFields.Count)   {
            // The textfields inherit from the field above
            for (int i = textFields.Count - 1; i > 0; i--) {
                textFields[i].text = textFields[i - 1].text;
            }
            // Except for the first field which will need a new text.
            textFields[0].text = text_Interaction.lines[pointer - (textFields.Count + 1)];
            pointer--;
        }

    }


    // Actual Scene loading.
    IEnumerator LoadScene(string nextSceneName) {
        // Set the current Scene to be able to unload it later
        Scene currentScene = SceneManager.GetActiveScene();
        Scene nextScene = SceneManager.GetSceneByBuildIndex(sceneNameToBuildID[nextSceneName]); // Which scene will we load.

        //Debug.Log("Next Scene:" + nextSceneName + ", next scene build ID:" + sceneNameToBuildID[nextSceneName]);

        // If the Avatar is heading into a random scene it will go back through the same sequence of scenes.
        if (!returning && currentScene.name != Loading_Scene_Name) {
            headOutSceneNames.Push(currentScene.name);
            // Debug.Log("Pushed:" + currentScene.name);
        }
        if (returning) returning = false; // resetting, so the player will not be returning as default in next scene load.


        // If we have a new scene, it's loaded in the background and objects are moved.
        if (nextScene != currentScene) {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
            // Wait until the asynchronous scene fully loads
            while (!asyncLoad.isDone) {
                yield return null;
            }
            // Moving gameobjects (now we can get scene by name because it's loaded).
            for (int i = 0; i < ObjectsToMove.Count; i++) {
                SceneManager.MoveGameObjectToScene(ObjectsToMove[i], SceneManager.GetSceneByName(nextSceneName));
            }
        }
        // Unload the previous Scene, if different from next scene.
        if (nextScene != currentScene) SceneManager.UnloadSceneAsync(currentScene);
    }


    public IEnumerator HideView() {
        // Hiding scene
        //------------------------------
        gameManager.playerStatus_Panel.SetActive(false);
        gameManager.actionHandler_Panel.SetActive(false);

        sceneLoadingPanel.SetActive(true);
        loadingPanelImage.color = new Color(0, 0, 0, 1);
        sceneLoadingText.color = new Color(255, 0, 0, 1);
        sceneLoadingText.text = "Scene Loading";
        //------------------------------
        yield return null;
    }


    public IEnumerator ShowView() {
        yield return new WaitForSeconds(1);
        // Showing scene
        //------------------------------
        gameManager.actionHandler_Panel.SetActive(false);

        if (!gameManager.gameEnded) {
            gameManager.playerStatus_Panel.SetActive(true);
            loadingPanelImage.color = new Color(0, 0, 0, 0);
            sceneLoadingText.color = new Color(255, 0, 0, 0);
            sceneLoadingPanel.SetActive(false);
        }
        //------------------------------
    }




}
