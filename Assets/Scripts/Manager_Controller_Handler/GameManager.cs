using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.IO;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public delegate void AnnouncePlayerRespawn(); // So the Jukebox knows.

class GameManager : Level
{
    public event AnnouncePlayerRespawn I_Respawned;

    // Seeds
    public int customSeedToUse;
    public int randomSeedUsed;
    [HideInInspector] public int seedUsed;

    // Parent gameobjects / Spawning / Tiles.
    private RuleTile ruleTile;
    private Tilemap thisTileMap;
    [HideInInspector] public List<GameObject> tilesDebugging = new List<GameObject>();
    [HideInInspector] List<string> usedPCG_SpawnLocs = new List<string>();
    private GameObject PCG_TileParent;  // Just an empty object to hold all the tiles and art of the level.
    private GameObject PCG_Parent;  // Just an empty object to hold PCG content of the level.
    private GameObject QuestItem_Parent;

    // Actor containers
    public List<GameObject> allActors = new List<GameObject>();         // All actors.
    [HideInInspector] public List<GameObject> spawnedActors = new List<GameObject>();     // Actors needed for the game.
    public List<GameObject> allActorsInScene = new List<GameObject>();  // Actors needed for the present level/scene.
    public List<GameObject> actorsInCamView = new List<GameObject>();   // Actors wanted in the camera view.

    // Containers for refs.
    [HideInInspector] public List<ActionHandler> AH_Refs = new List<ActionHandler>();
    public Dictionary<string, ActionHandler> actionRefByName = new Dictionary<string, ActionHandler>();
    public Dictionary<string, GameObject> actorRefByBaseTag = new Dictionary<string, GameObject>();
    // Other refs.
    [HideInInspector] public GameObject playerCamera;
    [HideInInspector] public CamControl camControl;
    [HideInInspector] public GameObject actionHandler_Panel;
    public GameObject mapButton;
    // Player status panel refs.
    [HideInInspector] public GameObject playerStatus_Panel;
    Text[] playerStatusTexts = new Text[4];
    [HideInInspector] public ButtonActions buttonActions;

    bool startedCityDialogue = false;
    public Dialogue cityDialogue;
    public Dialogue secretDoorDialogue;

    UIManager _ui;
    [HideInInspector] public DialogueManager _dm;

    // This variable is here for convenience, so you can specify fileName before pressing start.
    // The 'Save_Load' class has it's own identical varable.
    // ----------------------------------------
    public string customTilesFileName = "";
    // ----------------------------------------

    // The game
    [HideInInspector] public bool gameStarted = false;
    [HideInInspector] public bool gameEnded = false;
    public float globalAttackTimer;
    private bool goalReached;
    private float avatarSceneSpawnLocH;
    private float avatarSceneSpawnLocV;
    private GameObject avatarGO;
    public float playerImmortalityTimer;
    [HideInInspector] public string currentSceneName;
    Text sceneLoadingText;


    protected override void Awake()
    {
        base.Awake();

        _ui = GameObject.Find("UIManager").GetComponent<UIManager>();
        _dm = GameObject.Find("DialogueManager").GetComponent<DialogueManager>();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        randomSeedUsed = 0;
        camControl = playerCamera.GetComponent<CamControl>();
        SpawnActors();
        MakeActorRefByTag();
        GetRefs();
        avatarGO = spawnedActors[0];
    }


    void Start()
    {
        StartCoroutine(GameMainFunction());

    }

    void Update()
    {
        //  Interrupts the game and returns to main menu.
        if (Input.GetKey(KeyCode.Escape))
        {
            SceneManager.LoadScene("LoadingScene");
        }
    }

    private void FixedUpdate()
    {
        currentSceneName = SceneManager.GetActiveScene().name;

        playerImmortalityTimer += Time.deltaTime;
        if (playerImmortalityTimer > 10f)
        {
            avatarGO.GetComponent<BaseIdentity>().baseHP = 1;
            avatarGO.GetComponent<BaseIdentity>().HP = 1;
        }
        globalAttackTimer += Time.deltaTime;
        UpdatePlayerStatus();



        if (currentSceneName == "2_CityScene" && startedCityDialogue == false)
        {
            var npc = GameObject.Find("Mentor_P(Clone)");
            if (npc)
            {
                StartCoroutine(startCityDialogue(npc));
            }
        }
    }


    //  Main jobs started from here.
    //  ==================================================
    private IEnumerator GameMainFunction()
    {
        yield return StartCoroutine(InitGame()); // Setting the scene with instantiated objects.
        yield return StartCoroutine(Game_Running());    // Running the game.
        //yield return StartCoroutine(EndGame());       // Game end message
    }
    //  ==================================================


    // New Game initialization
    private IEnumerator InitGame()
    {
        // sceneLoadingText.text = "Scene Loading";
        gameStarted = false;
        gameEnded = false;
        goalReached = false;
        globalAttackTimer = 0;
        allActors[0].GetComponent<PlayerA>().SetStats();
        yield return null;
    }

    //  Main loop of the game.
    private IEnumerator Game_Running()
    {
        while (LastQuestNotCompleted())
        {
            yield return null;
        }
    }

    private bool LastQuestNotCompleted()
    {
        //if (!goalReached)
        if (currentSceneName != ButtonActions.CityOLD_Scene_Name)
        {
            return true;
        }
        else
        {
            //gameEnded = true;
            //return false;
            gameEnded = false;
            return true;
        }
    }

    //  End game actions
    private IEnumerator EndGame()
    {
        buttonActions.sceneLoadingPanel.SetActive(true);
        buttonActions.loadingPanelImage.color = new Color(255, 128, 0, 1);
        buttonActions.sceneLoadingText.text = "Game over";
        yield return new WaitForSeconds(1);
    }


    public void PrepareLevel(int customSeed, LevelType levelType, string AH_Name)
    {
        GameObject parent1 = GameObject.Find("PCG_Parent");
        GameObject parent2 = GameObject.Find("QuestItem_Parent");
        // Destroying old PCG content.
        Destroy(parent1);
        Destroy(parent2);
        // Destroying old level-map.
        Destroy(PCG_TileParent);

        seedUsed = SetSeed(customSeed);
        if (customSeed != 0)
        {
            // Preparing next level data (PCG level with custom or random seed).
            CreateLevel(levelType);
            // Spawning new tiles.
            SpawnTiles();

            //  Ref. to Tilemap
            //  -----------------------------------------------------------------------------
            //GameObject GO = GameObject.Find("Tilemap");
            //if (GO) thisTileMap = GO.GetComponent<Tilemap>();
            //else Debug.Log(this.name + " : I didn't find Tilemap.");
            //  -----------------------------------------------------------------------------

            // Showing random seed.
            if (customSeed < 0) randomSeedUsed = seedUsed;
            else randomSeedUsed = 0;
        }
        // Deactivating elements of old Scene/Level
        actionHandler_Panel.SetActive(false);
        DeactivateAllActorsInScene();
        allActorsInScene.Clear();
        actorsInCamView.Clear();

        // Setting the scene with actors/etc. Calling ActionHandler to do that.
        // ----------------------------------------------------------
        if (AH_Name.Length > 0)
        {
            ActionHandler action = actionRefByName[AH_Name].GetComponent<ActionHandler>();
            if (action.autoActivate == true)
            {
                action.activate = true;
            }
        }
        if (AH_Name != "_EmptyActionHandler")
        {
            // Spawn PCG content, but not Avatar (we have a custom seed).
            if (customSeed > 0) Get_PCG_SpawnLocs_AndSpawn(false);
            // Spawn PCG content, also Avatar (we have a random seed).
            if (customSeed < 0) Get_PCG_SpawnLocs_AndSpawn(true);
            // ---------------------------------------------------------
        }
    }


    void SpawnActors()
    {
        int i = 0;
        foreach (GameObject itr in allActors)
        {
            spawnedActors.Add(Instantiate(itr, new Vector2(), new Quaternion()));
            spawnedActors[i].transform.parent = transform.Find("ActorParent").transform;
            spawnedActors[i].SetActive(false);
            i++;
        }
    }

    // Used when avatar is down to 0 lives.
    public void AvatarBackToSceneSpawn()
    {
        playerImmortalityTimer = 0;
        avatarGO.GetComponent<BaseIdentity>().baseHP = 1000;
        I_Respawned();
        avatarGO.GetComponent<PlayerController>().Avatar_GrappleOff();
        avatarGO.GetComponent<BaseIdentity>().lives = avatarGO.GetComponent<BaseIdentity>().startLives;
        avatarGO.GetComponent<BaseIdentity>().HP = avatarGO.GetComponent<BaseIdentity>().baseHP;
        RelocateAndActivate_ActorInScene(avatarGO, avatarSceneSpawnLocH, avatarSceneSpawnLocV);
    }


    public void RelocateAndActivate_ActorInScene(GameObject actor, float spawnLocH, float spawnLocV)
    {
        // Keeping Avatar spawn location for future reference.
        if (actor.tag == spawnedActors[0].tag)
        {
            avatarSceneSpawnLocH = spawnLocH;
            avatarSceneSpawnLocV = spawnLocV;
        }
        string sceneName = SceneManager.GetActiveScene().name;
        //Debug.Log("Relocating:" + actor.name + ", in Scene:" + sceneName);
        actor.SetActive(true);
        actor.transform.position = new Vector2(spawnLocH, spawnLocV);
        actor.transform.rotation = new Quaternion();
    }


    public void CenterDrop_Of_AllActorsInScene()
    {
        int rndArea;
        int centerAreaH;
        int centerAreaV;
        int spawnLocH = 0;
        int spawnLocV = 0;
        foreach (GameObject itr in allActorsInScene)
        {
            if (areas.Count > 0)
            {
                rndArea = RandomInt(0, areas.Count);
                centerAreaH = areas[rndArea].width / 2;
                centerAreaV = areas[rndArea].height / 2;
                spawnLocH = areas[rndArea].originHori + centerAreaH;
                spawnLocV = areas[rndArea].originVert + centerAreaV;
            }
            itr.SetActive(true);
            if (itr.tag == spawnedActors[0].tag)
            {
                avatarSceneSpawnLocH = spawnLocH;
                avatarSceneSpawnLocV = spawnLocV;
            }
            itr.transform.position = new Vector2(spawnLocH, spawnLocV);
            itr.transform.rotation = new Quaternion();
        }
    }




    void DeactivateAllActorsInScene()
    {
        foreach (GameObject itr in allActorsInScene)
        {
            itr.SetActive(false);
        }
    }

    public void SetCameraTargets()
    {
        camControl.targets = new List<Transform>();
        for (int i = 0; i < actorsInCamView.Count; i++)
        {
            camControl.targets.Add(actorsInCamView[i].transform);
        }
    }



    // #################################################################################################################
    // ############################################## LEVEL INSTANTIATION ##############################################


    // Tiles used for the level.
    void SpawnTiles()
    {
        // Parent to hold all the tiles.
        PCG_TileParent = new GameObject();
        PCG_TileParent.name = "PCG_TileParent";
        PCG_TileParent.transform.parent = transform;
        // Accessing the universal tile prefab to handle outerWall.
        GameObject tile_gameObject_2 = Resources.Load<GameObject>("Tile_GameObject_2");
        SpriteRenderer rend = tile_gameObject_2.GetComponent<SpriteRenderer>();
        rend.sprite = tileSpritesGreen[1];
        GameObject outerTileObject;
        GameObject innerTileObject;
        rend.sprite = tileSpritesGreen[1];

        //-------------- TileMapTest
        ruleTile = Resources.Load<RuleTile>("Ruletiles/GreenDirtPaletteRules");
        //--------------

        for (int x = 0; x < levelGrid.GetLength(0); x++)
        {
            for (int y = 0; y < levelGrid.GetLength(1); y++)
            {
                // Setting the gameObjects/sprites of the outerWall manually because TileSwitcher() will look for its neighbors.
                // (And there will be none outside the wall).
                if (x == 0 || x == levelGrid.GetLength(0) - 1 || y == 0 || y == levelGrid.GetLength(1) - 1)
                {
                    if (levelGrid[x, y] == Symbol(Occupant.Wall))
                    {
                        outerTileObject = Instantiate(tile_gameObject_2, new Vector2(x, y), new Quaternion());
                        outerTileObject.transform.parent = PCG_TileParent.transform;
                    }
                }
                // Setting all the 'inner' gameObects/tile-sprites.
                else if (levelGrid[x, y] == Symbol(Occupant.Wall))
                {

                    // -------------- TileMapTest
                    // thisTileMap.SetTile(new Vector3Int(x, y, 0), ruleTile);
                    // --------------

                    innerTileObject = TileSwitcher(x, y);
                    GameObject innerTileInstance = Instantiate(innerTileObject, new Vector2(x, y), new Quaternion());
                    innerTileInstance.transform.parent = PCG_TileParent.transform;
                }
            }
        }

    }


    void Get_PCG_SpawnLocs_AndSpawn(bool isRndPCG)
    {
        GameObject commonCrystal = Resources.Load<GameObject>("Common Crystal");
        int maxCommonCrystalCount = 0;
        GameObject clue = Resources.Load<GameObject>("Clue");
        int maxClueCount = 0;

        GameObject walking = Resources.Load<GameObject>("UnivWalkingPCG-NPC_P");
        GameObject flying = Resources.Load<GameObject>("UnivFlyingPCG-NPC_P");
        GameObject Z1_Art = Resources.Load<GameObject>("Z1_ArtPCG_Object");     // For foreground (z-layer 1)
        GameObject Z0_Art = Resources.Load<GameObject>("Z0_ArtPCG_Object");     // For midground (z-layer 0)
        GameObject Z_1_Art = Resources.Load<GameObject>("Z_1_ArtPCG_Object");   // For background (z-layer -1)
        //
        PCG_Parent = new GameObject();
        PCG_Parent.name = "PCG_Parent";
        PCG_Parent.transform.parent = transform;
        //
        QuestItem_Parent = new GameObject();
        QuestItem_Parent.name = "QuestItem_Parent";
        QuestItem_Parent.transform.parent = transform;

        // Loading sprites as objects.
        UnityEngine.Object[] ceilingDecObjects = Resources.LoadAll("PCG_Sprites/CeilingDecoration", typeof(Sprite));
        UnityEngine.Object[] largeDoorObjects = Resources.LoadAll("PCG_Sprites/Doors_Large", typeof(Sprite));
        UnityEngine.Object[] tileDoorObjects = Resources.LoadAll("PCG_Sprites/Doors_TileSize", typeof(Sprite));
        UnityEngine.Object[] floorDecObjects = Resources.LoadAll("PCG_Sprites/FloorDecoration", typeof(Sprite));
        UnityEngine.Object[] flyingObjects = Resources.LoadAll("PCG_Sprites/Flying", typeof(Sprite));
        UnityEngine.Object[] largeObjects = Resources.LoadAll("PCG_Sprites/LargeSprites", typeof(Sprite));
        UnityEngine.Object[] pillarObjects = Resources.LoadAll("PCG_Sprites/Pillars", typeof(Sprite));
        UnityEngine.Object[] tileBackgroundObjects = Resources.LoadAll("PCG_Sprites/TileSizeBackground", typeof(Sprite));
        UnityEngine.Object[] walkObjects = Resources.LoadAll("PCG_Sprites/Walking", typeof(Sprite));
        UnityEngine.Object[] wallDecObjects = Resources.LoadAll("PCG_Sprites/WallDecoration", typeof(Sprite));
        UnityEngine.Object[] centerDecObjects = Resources.LoadAll("PCG_Sprites/CenterDecoration", typeof(Sprite));

        List<string> corridorSpaceLocList = new List<string>();
        List<string> nonAreaLocList = new List<string>();


        foreach (Area itr1 in areas)
        {
            // Containers for wall-relative positions.
            //
            List<string> areaCeilingLocList = new List<string>();
            List<string> areaFloorLocList = new List<string>();
            List<string> areaSideLocList = new List<string>();
            List<string> areaCenterLocList = new List<string>();
            List<string> areaSingleLocList = new List<string>();
            // Container for space-relative positions.
            //
            List<string> areaSpaceLocList = new List<string>();

            int areaSize = (itr1.width - 2) * (itr1.height - 2);
            int areaWidth = (itr1.width - 2);
            int areaHeight = (itr1.height - 2);

            // Finding different types of spawnlocations available in this area.
            for (int i = itr1.originHori + 1; i < itr1.originHori + itr1.width - 1; i++)
            {
                for (int j = itr1.originVert + 1; j < itr1.originVert + itr1.height - 1; j++)
                {
                    // area-wall-relative
                    if (singleSpawnLocs.ContainsKey(i + "," + j)) areaSingleLocList.Add(i + "," + j);
                    if (centerSpawnLocs.ContainsKey(i + "," + j)) areaCenterLocList.Add(i + "," + j);
                    if (floorSpawnLocs.ContainsKey(i + "," + j)) areaFloorLocList.Add(i + "," + j);
                    if (ceilingSpawnLocs.ContainsKey(i + "," + j)) areaCeilingLocList.Add(i + "," + j);
                    if (sideSpawnLocs.ContainsKey(i + "," + j)) areaSideLocList.Add(i + "," + j);
                    // area-space-relative
                    if (freeAreaTiles.ContainsKey(i + "," + j)) areaSpaceLocList.Add(i + "," + j);
                }
            }

            // ===========================================================================
            // ============================== Spawn begin ================================
            // ===========================================================================
            // Walking
            if (!cellAreas.ContainsKey(itr1.areaID))
            {

                SpawnObjects(walking, Random.Range(0, 2), areaFloorLocList, walkObjects);
                SpawnObjects(flying, Random.Range(0, 2), areaSpaceLocList, flyingObjects);
            }

            // Flying
            if (cellAreas.ContainsKey(itr1.areaID))
            {
                SpawnObjects(flying, Random.Range(0, 3), areaSpaceLocList, flyingObjects);
            }


            // Floor deco
            SpawnObjects(Z_1_Art, Random.Range(0, (int)Math.Floor(Mathf.Sqrt(areaWidth))), areaFloorLocList, floorDecObjects);

            // Common crystals
            int rndNumber = Random.Range(0, 2);
            if (rndNumber > 0 && maxCommonCrystalCount < 4)
            {
                maxCommonCrystalCount++;
                SpawnSpecialObjects(commonCrystal, 1, areaFloorLocList);
            }


            // Clues
            if (_dm.quest != null && _dm.quest.questName == "The Rare Crystal")
            {
                rndNumber = Random.Range(0, 2);
                if (rndNumber > 0 && maxClueCount < 4)
                {
                    maxClueCount++;
                    SpawnSpecialObjects(clue, 1, areaFloorLocList);
                }
            }


            // Area-relative 'On top of wall' deco.
            if (cellAreas.ContainsKey(itr1.areaID)) SpawnObjects(Z1_Art, Random.Range(0, areaHeight + areaWidth), areaCenterLocList, centerDecObjects);


            // stalagtites
            // --------------------------------------------------
            int stalagtiteCount;
            float areaAspect = (float)areaWidth / (float)areaHeight;
            UnityEngine.Object stalagtite = largeObjects[2];
            Sprite tempStalagtite = (Sprite)stalagtite;

            float sprite1Width = tempStalagtite.texture.width;
            float sprite1Height = tempStalagtite.texture.height;
            int areaPixWidth = areaWidth * 32;
            int areaPixHeight = areaHeight * 32;

            stalagtiteCount = Random.Range(0, (int)Math.Floor(areaPixWidth / sprite1Width) + 1);
            stalagtiteCount = Random.Range(-5, 6);

            if (stalagtiteCount > 0)
            {
                for (int i = 0; i < stalagtiteCount; i++)
                {
                    int spawnAreaLocH = Random.Range(2, areaWidth - 1);
                    int spawnLocH = itr1.originHori + spawnAreaLocH;
                    int spawnLocV = itr1.originVert + itr1.height - 1;
                    string spawnLocString = spawnLocH + "," + spawnLocV;
                    float overallScale = 0.8f * areaPixHeight / sprite1Height;
                    float scaleRatio = Random.Range(0.25f, 1f);
                    overallScale *= scaleRatio;
                    float scaleHori = Random.Range(-0.8f, -0.5f);

                    Spawn1Sprite(Z_1_Art, spawnLocString, tempStalagtite, overallScale, scaleHori);
                }
            }
            // --------------------------------------------------


            // Pillars
            // --------------------------------------------------
            int pillarCount;
            UnityEngine.Object pillarDown = largeObjects[0];
            UnityEngine.Object pillarUp = largeObjects[1];
            Sprite tempPillarDown = (Sprite)pillarDown;
            Sprite tempPillarUp = (Sprite)pillarUp;
            float sprite2Width = tempPillarDown.texture.width;
            float sprite2Height = tempPillarDown.texture.height;

            //Debug.Log("spWidth: " + spriteWidth);
            //Debug.Log("spHeight: " + spriteHeight);
            //Debug.Log("areaPixWidth: " + areaPixWidth);
            //Debug.Log("areaPixHeight: " + areaPixHeight);
            //Debug.Log("Area ID:" + itr1.areaID + ", Area aspect:" + areaAspect);

            if (areaAspect >= 1)
            {
                pillarCount = Random.Range(0, (int)Math.Floor(areaPixWidth / sprite2Width) + 1);
                pillarCount = Random.Range(-5, 6);
                if (pillarCount > 0)
                {
                    for (int i = 0; i < pillarCount; i++)
                    {
                        int spawnAreaLocH = Random.Range(2, areaWidth - 1);
                        int spawnLocH = itr1.originHori + spawnAreaLocH;
                        int spawnLocV = itr1.originVert;
                        string spawnLocString = spawnLocH + "," + spawnLocV;
                        float overallScale = areaPixHeight / sprite2Height;
                        float scaleRatio = Random.Range(0.25f, 1f);
                        overallScale *= scaleRatio;
                        float scaleHori = Random.Range(-0.5f, 0.5f);

                        Spawn1Sprite(Z_1_Art, spawnLocString, tempPillarUp, overallScale, scaleHori);
                    }
                }
            }
            // --------------------------------------------------


            // =======================================================
            // ================ Spawning actors last =================
            // =======================================================
            // Putting the avatar in the first area, it's as good as any,
            // because it's random, it could be anywhere in the level,
            // and I guess it's more likely to be a large one,
            // since it's harder to fit large areas later in the level generation process.
            // A door will be spawned in the same area.
            // It makes sense, it's the door he/she entered from.

            //if (itr1.areaID == 0 && isRndPCG)
            if (areaFloorLocList.Count > 2 && itr1.areaID < 5 && isRndPCG &&
                !allActorsInScene.Contains(actorRefByBaseTag["Avatar_P"]) &&
                !allActorsInScene.Contains(actorRefByBaseTag["PCG-DoorAction-1_P"])
                )
            {
                ActivateActorInPCG("Avatar_P");
                SetCameraTargets(); // Finally with actor in place we update camera targets.
                ActivateActorInPCG("PCG-DoorAction-1_P");
            }

            void ActivateActorInPCG(string actorTag)
            {
                // Finding free spawn loc.
                int index = Random.Range(0, areaFloorLocList.Count);
                if (areaFloorLocList.Count == 0)
                {
                    Debug.Log("Warning, no floor locs for pcg spawn.");
                }
                int[] loc = new int[2];
                loc = areaFloorLocList[index].Split(',').Select(int.Parse).ToArray();
                //Debug.Log("Actor:" + actorTag + ", Loc found:" + areaFloorLocList[index]);

                // Updating locs.
                usedPCG_SpawnLocs.Add(areaFloorLocList[index]);
                areaFloorLocList.RemoveAt(index);

                // Adding new actor.
                if (!allActorsInScene.Contains(actorRefByBaseTag[actorTag]))
                {
                    allActorsInScene.Add(actorRefByBaseTag[actorTag]);
                }

                if (actorRefByBaseTag.ContainsKey(actorTag))
                {
                    actorsInCamView.Add(actorRefByBaseTag[actorTag].gameObject);
                }


                RelocateAndActivate_ActorInScene(actorRefByBaseTag[actorTag], loc[0], loc[1]); // Moving actor to new location.
            }



        }

        // Corridor background deco, and on top of wall deco.
        // --------------------------------------------------
        for (int i = 0; i < levelGrid.GetLength(0); i++)
        {
            for (int j = 0; j < levelGrid.GetLength(1); j++)
            {
                // space-relative
                if (freeCorridorTiles.ContainsKey(i + "," + j)) corridorSpaceLocList.Add(i + "," + j);
                if (unUsedTiles.ContainsKey(i + "," + j)) nonAreaLocList.Add(i + "," + j);
            }
        }
        SpawnObjects(Z0_Art, freeCorridorTiles.Count, corridorSpaceLocList, tileBackgroundObjects);
        SpawnObjects(Z1_Art, Random.Range((int)Math.Sqrt(unUsedTiles.Count / 2), (int)Math.Sqrt(unUsedTiles.Count)), nonAreaLocList, centerDecObjects);


        void SpawnObjects(GameObject spawnThis, int maxSpawnCount, List<string> spawnLocList, UnityEngine.Object[] objectArray)
        {
            for (int i = 0; i < maxSpawnCount; i++)
            {

                if (spawnLocList.Count > 0)
                {
                    // Instantiating.
                    int rndLoc = Random.Range(0, spawnLocList.Count);
                    int[] loc = spawnLocList[rndLoc].Split(',').Select(int.Parse).ToArray();
                    spawnLocList.RemoveAt(rndLoc);
                    GameObject _instance = Instantiate(spawnThis, new Vector2(loc[0], loc[1]), new Quaternion());
                    _instance.transform.parent = PCG_Parent.transform;

                    // Setting sprite.
                    Sprite mySprite = (Sprite)objectArray[Random.Range(0, objectArray.Length)];
                    //_instance.AddComponent<SpriteRenderer>();
                    SpriteRenderer _rend = _instance.GetComponent<SpriteRenderer>();
                    _rend.sprite = mySprite;
                    if (objectArray == tileBackgroundObjects)
                    {
                        float rndgrey = Random.Range(0.05f, 0.35f);
                        _rend.color = new Color(rndgrey, rndgrey, rndgrey, 1f);
                    }
                }
            }
        }

        // Spawning Common Crystals and Clues (quest items).
        void SpawnSpecialObjects(GameObject spawnThis, int maxSpawnCount, List<string> spawnLocList)
        {
            for (int i = 0; i < maxSpawnCount; i++)
            {
                if (spawnLocList.Count > 0)
                {
                    // Instantiating.
                    int rndLoc = Random.Range(0, spawnLocList.Count);
                    int[] loc = spawnLocList[rndLoc].Split(',').Select(int.Parse).ToArray();
                    spawnLocList.RemoveAt(rndLoc);
                    GameObject _instance = Instantiate(spawnThis, new Vector2(loc[0], loc[1]), new Quaternion());
                    _instance.transform.parent = QuestItem_Parent.transform;

                    SpriteRenderer _rend = spawnThis.GetComponentInChildren<SpriteRenderer>();
                    if (spawnThis.name == "Common Crystal")
                    {
                        float rndRed = Random.Range(0f, 1f);
                        float rndGreen = Random.Range(0f, 1f);
                        float rndBlue = Random.Range(0f, 1f);
                        _rend.color = new Color(rndRed, rndGreen, rndBlue, 1f);
                    }

                }
            }
        }


        void Spawn1Sprite(GameObject spawnThis, string locString, Sprite mySprite, float overallScale, float horiScale)
        {
            // Instantiating.
            int[] loc = locString.Split(',').Select(int.Parse).ToArray();
            GameObject _instance = Instantiate(spawnThis, new Vector2(loc[0], loc[1]), new Quaternion());
            _instance.transform.parent = PCG_Parent.transform;
            // Setting size.
            _instance.transform.localScale *= overallScale;
            Vector3 newWidth = new Vector3(_instance.transform.localScale.x * horiScale, 0, 0);
            _instance.transform.localScale += newWidth;
            // Setting sprite colour.
            SpriteRenderer _rend = _instance.GetComponent<SpriteRenderer>();
            _rend.sprite = mySprite;
            float red = Random.Range(0.3f, 0.7f);
            float green = Random.Range(0.3f, 0.7f);
            float blue = Random.Range(0.3f, 0.7f);
            _rend.color = new Color(red, green, blue, 1f);
        }
    }


    // Tiles for debugging.
    void SpawnTiles_Debugging()
    {
        // Parent to hold all the tiles.
        PCG_TileParent = new GameObject();
        PCG_TileParent.name = "PCG_TileParent";
        PCG_TileParent.transform.parent = transform;

        GameObject tile = default;

        for (int x = 0; x < levelGrid.GetLength(0); x++)
        {
            for (int y = 0; y < levelGrid.GetLength(1); y++)
            {
                char occupant = levelGrid[x, y];
                if (occupant == Symbol(Occupant.OuterWall))
                {
                    tile = Instantiate(tilesDebugging[0], new Vector2(x, y), new Quaternion());
                }
                else if (occupant == Symbol(Occupant.Wall))
                {
                    tile = Instantiate(tilesDebugging[1], new Vector2(x, y), new Quaternion());
                }
                else if (occupant == Symbol(Occupant.Used))
                {
                    tile = Instantiate(tilesDebugging[2], new Vector2(x, y), new Quaternion());
                }
                else if (occupant == Symbol(Occupant.UnUsed))
                {
                    tile = Instantiate(tilesDebugging[3], new Vector2(x, y), new Quaternion());
                }
                tile.transform.parent = PCG_TileParent.transform;
            }
        }
    }

    // ############################################## LEVEL INSTANTIATION ##############################################
    // #################################################################################################################

    void UpdatePlayerStatus()
    {
        int lives = actorRefByBaseTag["Avatar_P"].GetComponent<BaseIdentity>().lives;
        if (lives == 3)
        {
            _ui.life1.SetActive(true);
            _ui.life2.SetActive(true);
            _ui.life3.SetActive(true);
        }
        else if (lives == 2)
        {
            _ui.life1.SetActive(true);
            _ui.life2.SetActive(true);
            _ui.life3.SetActive(false);
        }
        else if (lives == 1)
        {
            _ui.life1.SetActive(true);
            _ui.life2.SetActive(false);
            _ui.life3.SetActive(false);
        }
        else if (lives < 1)
        {
            _ui.life1.SetActive(false);
            _ui.life2.SetActive(false);
            _ui.life3.SetActive(false);
        }
        //playerStatusTexts[1].text = lives.ToString();
        //int HP = (int)actorRefByTag["Avatar_P"].GetComponent<BaseIdentity>().HP;
        //playerStatusTexts[3].text = HP.ToString();

    }


    void MakeActorRefByTag()
    {
        foreach (GameObject itr in spawnedActors)
        {
            actorRefByBaseTag.Add(itr.tag.Split('_')[0] + "_P", itr);
            itr.SetActive(false);
        }
        camControl.door1 = actorRefByBaseTag["PCG-DoorAction-1_P"].transform;
        camControl.door3 = actorRefByBaseTag["PCG-DoorAction-3_P"].transform;
        camControl.doorTransform = actorRefByBaseTag["PCG-DoorAction-1_P"].transform;
    }

    void GetRefs()
    {
        //  Ref. to actionHandlers.
        //  -----------------------------------------------------------------------------
        string lookFor = "AH_Parent";
        GameObject _instance = GameObject.Find(lookFor);
        if (_instance)
        {
            ActionHandler[] allActionHandlers = _instance.GetComponentsInChildren<ActionHandler>();
            foreach (ActionHandler itr in allActionHandlers)
            {
                AH_Refs.Add(itr);
                actionRefByName.Add(itr.name, itr);
            }
        }
        else Debug.Log(this.name + " : I didn't find GameObject " + lookFor);
        //  -----------------------------------------------------------------------------

        //  Ref. to MapButton.
        //  -----------------------------------------------------------------------------
        playerStatus_Panel.SetActive(true);
        lookFor = "MapButton";
        mapButton = GameObject.Find(lookFor);
        if (!mapButton) Debug.Log(this.name + " : I didn't find GameObject" + lookFor);
        playerStatus_Panel.SetActive(false);
        //  -----------------------------------------------------------------------------

        //  Ref. to ButtonScripts.
        //  -----------------------------------------------------------------------------
        lookFor = "ButtonScripts";
        _instance = GameObject.Find(lookFor);
        if (_instance)
        {
            buttonActions = _instance.GetComponent<ButtonActions>();
        }
        else Debug.Log(this.name + " : I didn't find GameObject" + lookFor);
        //  -----------------------------------------------------------------------------

    }

    IEnumerator startCityDialogue(GameObject npc)
    {
        yield return new WaitForSeconds(1f);
        PlayerController playerController = GameObject.Find("Avatar_P(Clone)").GetComponent<PlayerController>();
        RopeSystem ropeSystem = GameObject.Find("Avatar_P(Clone)").GetComponent<RopeSystem>();
        playerController.enabled = false;
        ropeSystem.crosshairSprite.enabled = false;
        ropeSystem.GetComponent<RopeSystem>().enabled = false;

        _ui.dialoguePanel.SetActive(true);
        _dm.dialogue = cityDialogue;
        _dm.StartDialogue(npc);
        startedCityDialogue = true;
    }

    public void LoadSpecificScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }
    IEnumerator LoadSceneRoutine(string sceneName)
    {
        GameObject npc;
        Scene scene = SceneManager.GetActiveScene();
        
        yield return new WaitForSeconds(0.5f);
        
        if(scene.name != "3_OracleScene")
        {
            npc = GameObject.Find("Mentor_P(Clone)");

            var allDialogues = new List<Dialogue>(npc.GetComponent<Mentor>().dialogues);
            npc.GetComponent<Mentor>().dialogues.Clear();



            if (sceneName == "1_TutorialScene")
            {
                npc.GetComponent<Mentor>().dialogues.Add(allDialogues[1]);
                npc.GetComponent<Mentor>().dialogues.Add(allDialogues[2]);
                npc.GetComponent<Mentor>().dialogues.Add(allDialogues[3]);
                npc.GetComponent<Mentor>().dialogues.Add(allDialogues[4]);

            }
            else if (sceneName == "4_CampScene")
            {
                npc.GetComponent<Mentor>().dialogues.Add(allDialogues[3]);
                npc.GetComponent<Mentor>().dialogues.Add(allDialogues[4]);
            }

        }
        else
        {
            if (sceneName == "3_OracleScene")
            {
                _dm.dialogue = secretDoorDialogue;
                _dm.quest = secretDoorDialogue.quest;

            }
        }
        
        
    }
}
