using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

class Level : LevelCalc {

    public enum LevelType { TrueFullRandom_Level, SemiFullRandom_Level, TopLevel, HalfTop_HalfDown, Cellular_1, Rectangular_1,
        Cellular_xNumLarge, Rectangular_3Large, Rect_Cell_1Large_3Large,
        Cellular_Medium_50Build, Rectangular_medium_50Build,
        HalfHalf_Medium_50Build, HalfHalf_Medium_100Build
    }

    public enum TileType { Alone, Center,
        UL_Corner, UR_Corner, LL_Corner, LR_Corner,
        Upper_Side, Lower_Side, Left_Side, Right_Side,
        Upper_Single, Lower_Single, Left_Single, Right_Single,
        Horizontal_Platform, Vertical_Platform
    }


    // Variables for level build.
    // ------------------------------------------------------------------------------------------------------
    public LevelType levelType;
    public readonly int GLOBAL_GLOBAL = 7;
    public readonly int levelMaxSize = 300;
    [HideInInspector] public int globalMinAreaSideLength = 10;
    // ratio controls the magnitude of globalMaxAreaWidth & globalMaxAreaHeight, namely :
    // How large a portion of the randomly chosen level sizeH & sizeV is the randomly chosen maxAreaWidth & maxAreaHeight allowed to fill.
    // Randomly variating ratio allows for great variation in area size.
    [HideInInspector] public float ratio = 2f / 3f;     
    [HideInInspector] int globalMaxAreaWidth;
    [HideInInspector] int globalMaxAreaHeight;
    int startFreeTiles; // Total number of tiles to build on.
    int tilesLeftToBuild; // Number of tiles left to build on.
    int maxAreaFittingAttempts = 1000;
    [HideInInspector] public Dictionary<int, int> cellAreas = new Dictionary<int, int>(); // Contains areaIDs of the cellular areas in the level.
    // User controllable variables.
    public int sizeH; // Horizontal size of the level.
    public int sizeV; // Vertical size of the level.
    [HideInInspector] public int buildPct; // How large a percentage of the initial free tiles do we want to build on.
    [HideInInspector] public int minAreaWidth = 0;
    [HideInInspector] public int maxAreaWidth = 0;
    [HideInInspector] public int minAreaHeight = 0;
    [HideInInspector] public int maxAreaHeight = 0;
    public int fixedAreaNumber = 0;
    [HideInInspector] public int pctCellAreas = 0;
    [HideInInspector] public bool digIrregular = true;

    // Tile containers        
    List<string> unUsedTileList = new List<string>(); // List of available tile-coords by index, for easy random selection.
    [HideInInspector] public Dictionary<string, int> unUsedTiles = new Dictionary<string, int>(); // Dictionary of available tile-coords by coordinate-string, for fast access
    Dictionary<string, int> cornerTiles = new Dictionary<string, int>(); // Key: coordinate strings for all area corners, used to prevent corridors from digging into corners. Value holds areaID, but is not used for anything.
    
    // Tile containers regarding removal of dead ends.
    List<List<string>> deadEndCoordsList = new List<List<string>>(); // List containing Lists with coordinates of corridor tiles (for removing dead ends).
    List<List<char>> deadEndCharssList = new List<List<char>>(); // List containing Lists with chars of corridor tiles (for removing dead ends).
    Dictionary<string, int> corridorIntersections = new Dictionary<string, int>(); // Dictionary with keys of coordinates of tiles where corridors have intersected (for removing dead ends).

    // Containers to be used for populating the level.
    // ------------------------------------------------------------------------------------------------------
    public Dictionary<string, int> freeAreaTiles = new Dictionary<string, int>(); // Container of available AREA tiles. For random placement of objects and actors in the Level.
    public Dictionary<string, int> freeCorridorTiles = new Dictionary<string, int>();
    
    // Tile variables
    [HideInInspector] public Sprite[] tileSpritesGreen = new Sprite[16]; // Don't change the line. You will have to assign all the sprites in the inspector again.


    // Variables for the running game (also used during level build)
    // ------------------------------------------------------------------------------------------------------
    public char[,] levelGrid; // Matrix of chars presented to the player.
    public char[,] seenLevelGrid; // Matrix of chars representing what the player has seen.
    public Dictionary<string, int> coordToAreaID = new Dictionary<string, int>(); // Easy access to areaID by coordinate string. Good for knowing what area somthing (e.g. player) is in. (Also used during level build). 
    public List<Area> areas = new List<Area>(); // All the areas in the level.

    // Containers holding positions for different types of objects to be instantiated.
    public Dictionary<string, int> singleSpawnLocs = new Dictionary<string, int>();
    public Dictionary<string, int> centerSpawnLocs = new Dictionary<string, int>();
    public Dictionary<string, int> floorSpawnLocs = new Dictionary<string, int>();
    public Dictionary<string, int> ceilingSpawnLocs = new Dictionary<string, int>();
    public Dictionary<string, int> sideSpawnLocs = new Dictionary<string, int>();


    protected override void Awake() {
        base.Awake();
    }
   
    protected void CreateLevel(LevelType levelType) {
        Initialize();           // Step 1
        LevelSelect(levelType); // Step 2
        CreateEmptyLevel();     // Step 3
        CreateAreas();          // Step 4
        GenerateCellular();     // Step 5, NOTE: Cellular generation has to be done before corridors; the corridors need positions from that.
        HandleCorridors();      // Step 6
        RemoveDeadEnds();       // Step 7
        NeighborSearch();       // Step 8
        AddWalls();             // Step 9
    }

    protected void Initialize() {
        ratio = 2f / 3f; // Adjusting the maximum side length of an area relative to level size.
        // Clear all level creation containers.
        unUsedTileList.Clear();
        unUsedTiles.Clear();
        cornerTiles.Clear();
        deadEndCoordsList.Clear();
        deadEndCharssList.Clear();
        corridorIntersections.Clear();
        freeAreaTiles.Clear();
        freeCorridorTiles.Clear();
        coordToAreaID.Clear();
        areas.Clear();
        cellAreas.Clear();
        // Clear spawn loc containers.
        centerSpawnLocs.Clear();
        floorSpawnLocs.Clear();
        ceilingSpawnLocs.Clear();
        sideSpawnLocs.Clear();
        singleSpawnLocs.Clear();
    }

    protected void LevelSelect(LevelType levelType) {
        switch (levelType) {
            case LevelType.TrueFullRandom_Level:
                PCG_TrueFullRandom();
                break;
            case LevelType.SemiFullRandom_Level:
                PCG_SemiFullRandom();
                break;
            case LevelType.TopLevel:
                break;
            case LevelType.HalfTop_HalfDown:
                break;
            case LevelType.Cellular_1:
                SetLevelParams(sizeH, sizeV, 100, sizeH, sizeH, sizeV, sizeV, 1, 100, true);     // ##### 1 maximum cellular area #####
                break;
            case LevelType.Rectangular_1:
                SetLevelParams(sizeH, sizeV, 100, sizeH, sizeH, sizeV, sizeV, 1, 0, true);       // ##### 1 maximum rectangular area #####
                break;
            case LevelType.Cellular_xNumLarge:
                SetLevelParams(sizeH, sizeV, 100, 50, 50, 35, 35, fixedAreaNumber, 100, true);       // ##### 3 large cellular areas #####
                break;
            case LevelType.Rectangular_3Large:
                SetLevelParams(sizeH, sizeV, 100, 50, 50, 35, 35, 3, 0, true);         // ##### 3 large rectangular areas #####
                break;
            case LevelType.Rect_Cell_1Large_3Large:
                SetLevelParams(sizeH, sizeV, 100, 50, 50, 35, 35, 4, 99, true);         // ##### 1 large rectangular, and 3 large cellular areas #####
                break;
            case LevelType.Cellular_Medium_50Build:
                SetLevelParams(sizeH, sizeV, 50, 20, 40, 10, 30, 0, 100, true);        // ##### 50% build, 100% cellular, medium size #####  
                break;
            case LevelType.Rectangular_medium_50Build:
                SetLevelParams(sizeH, sizeV, 50, 20, 40, 10, 30, 0, 0, true);          // ##### 50% build, 100% rectangular, medium size #####  
                break;
            case LevelType.HalfHalf_Medium_50Build:
                SetLevelParams(sizeH, sizeV, 50, 20, 40, 10, 30, 0, 50, true);         // ##### 50% build, Half and half, square/cellular, medium size #####
                break;
            case LevelType.HalfHalf_Medium_100Build:
                SetLevelParams(sizeH, sizeV, 100, 20, 40, 10, 30, 0, 50, true);         // ##### 100% build, Half and half, square/cellular, medium size #####
                break;
        }
        this.levelType = levelType;
    }


    void PCG_SemiFullRandom() {
        ratio = 1f * (RandomInt(10, 33) / 100f); // Limiting how large the areas can become, compared to TrueFullRandom.
        sizeH = RandomInt(globalMinAreaSideLength + 2, levelMaxSize + 1); // Plus 2 because the outer wall is 1 + 1.
        sizeV = RandomInt(globalMinAreaSideLength + 2, levelMaxSize + 1); // Plus 2 because the outer wall is 1 + 1.
        buildPct = RandomInt(40, 70);
        minAreaWidth = RandomInt(globalMinAreaSideLength, (sizeH - 2) + 1);
        maxAreaWidth = RandomInt(minAreaWidth, (sizeH - 2) + 1);
        minAreaHeight = RandomInt(globalMinAreaSideLength, (sizeV - 2) + 1);
        maxAreaHeight = RandomInt(minAreaHeight, (sizeV - 2) + 1);
        fixedAreaNumber = RandomInt(1, (sizeH + sizeV) / 10);
        pctCellAreas = RandomInt(80, 101);
        int zeroOrOne = RandomInt(0, 2);
        if (zeroOrOne == 1) digIrregular = true;
        else digIrregular = false;
    }


    void PCG_TrueFullRandom() {
        ratio = 1f * (RandomInt(1, 101) / 100f);
        sizeH = RandomInt(globalMinAreaSideLength + 2, levelMaxSize + 1); // Plus 2 because the outer wall is 1 + 1.
        sizeV = RandomInt(globalMinAreaSideLength + 2, levelMaxSize + 1); // Plus 2 because the outer wall is 1 + 1.
        buildPct = RandomInt(1, 101);
        minAreaWidth = RandomInt(globalMinAreaSideLength, (sizeH - 2) + 1);
        maxAreaWidth = RandomInt(minAreaWidth, (sizeH - 2) + 1);
        minAreaHeight = RandomInt(globalMinAreaSideLength, (sizeV - 2) + 1);
        maxAreaHeight = RandomInt(minAreaHeight, (sizeV - 2) + 1);
        fixedAreaNumber = RandomInt(1, (sizeH + sizeV) / 10);
        pctCellAreas = RandomInt(0, 101);
        int zeroOrOne = RandomInt(0, 2);
        if (zeroOrOne == 1) digIrregular = true;
        else digIrregular = false;
    }


    public void SetLevelParams(int sizeH, int sizeV, int buildPct, int minAreaWidth, int maxAreaWidth, int minAreaHeight, int maxAreaHeight, int fixedAreaNumber, int pctCellAreas, bool digIrregular)
    {
        this.sizeH = sizeH;
        this.sizeV = sizeV;
        this.buildPct = buildPct;
        this.minAreaWidth = minAreaWidth;
        this.maxAreaWidth = maxAreaWidth;
        this.minAreaHeight = minAreaHeight;
        this.maxAreaHeight = maxAreaHeight;
        this.fixedAreaNumber = fixedAreaNumber;
        this.pctCellAreas = pctCellAreas;
        this.digIrregular = digIrregular;
    }


    // Initializing the level.
    void CreateEmptyLevel() {
        // For the special case of a single area we set it to maximum possible size.
        if (fixedAreaNumber == 1) {
            globalMaxAreaWidth = maxAreaWidth;
            globalMaxAreaHeight = maxAreaHeight;
        }
        else {
            int minAreaSize = GLOBAL_GLOBAL + 3;
            globalMaxAreaWidth = minAreaSize + (int)Mathf.Floor(((sizeH - 2) - minAreaSize) * ratio);
            globalMaxAreaHeight = minAreaSize + (int)Mathf.Floor(((sizeV - 2) - minAreaSize) * ratio);
        }

        startFreeTiles = ((sizeH - 2) * (sizeV - 2)); // The number of 'not outer wall' tiles.
        tilesLeftToBuild = (int)Math.Floor(startFreeTiles * (buildPct / 100f));  // The number of tiles we have to build on, after taking the build pct. into account.
        levelGrid = new char[sizeH, sizeV];           // The grid actually holding the developing level, consisting of chars.
        seenLevelGrid = new char[sizeH, sizeV];       // This grid represents what the player has seen, or not seen, of the level.

            // Initializing the level grid.
            // The grid has two symbols after initialization.
            // The 'outer wall' symbol and the 'unused' symbol for all the rest.
            for (int i = 0; i < sizeH; i++) {
            for (int j = 0; j < sizeV; j++) {
                if (i == 0 || i == sizeH - 1 || j == 0 || j == sizeV - 1)
                    levelGrid[i, j] = Symbol(Occupant.OuterWall);
                else {
                    levelGrid[i, j] = Symbol(Occupant.UnUsed);
                    unUsedTileList.Add(i + "," + j);
                    unUsedTiles.Add(i + "," + j, 0);
                }
                // Grid of what the player has seen or not seen is filled.
                seenLevelGrid[i, j] = Symbol(Occupant.UnSeen);
            }
        }
    }


    // This method generates random areas in the level.
    void CreateAreas() {
        int cellAreaCount;
        Area tempArea = new Area();
        tempArea.missingCorridors = new List<Direction>() { Direction.North, Direction.East, Direction.South, Direction.West };

        while (tilesLeftToBuild > 0) {
            // Putting some safety restrictions on the user input.
            if (minAreaWidth < globalMinAreaSideLength) minAreaWidth = globalMinAreaSideLength;
            if (maxAreaWidth < minAreaWidth || maxAreaWidth > globalMaxAreaWidth) maxAreaWidth = globalMaxAreaWidth;
            if (minAreaWidth > maxAreaWidth) minAreaWidth = maxAreaWidth;
            // ----------
            if (minAreaHeight < globalMinAreaSideLength) minAreaHeight = globalMinAreaSideLength;
            if (maxAreaHeight < minAreaHeight || maxAreaHeight > globalMaxAreaHeight) maxAreaHeight = globalMaxAreaHeight;
            if (minAreaHeight > maxAreaHeight) minAreaHeight = maxAreaHeight;

            int randomWidth = RandomInt(minAreaWidth, maxAreaWidth + 1);
            int randomHeight = RandomInt(minAreaHeight, maxAreaHeight + 1);

            tempArea.width = randomWidth;
            tempArea.height = randomHeight;

            char[,] areaUsed = FitArea(tempArea); // A call is made to fit the area in the level grid.
            // If null here, it means we cannot fit anymore areas.
            if (areaUsed != null) {
                int tilesUsedforArea = areaUsed.GetLength(0) * areaUsed.GetLength(1);
                tilesLeftToBuild -= tilesUsedforArea;
                // Stopping the build if we have a fixedAreaNumber.
                if (fixedAreaNumber > 0 && areas.Count >= fixedAreaNumber) tilesLeftToBuild = 0;
            }
            else tilesLeftToBuild = 0; // Stopping the build if globalMinAreaSideLength has come down to GLOBAL_GLOBAL.
        }
        // NOTE: By now, the outline of all areas in the level is in place.
        // =================================================================================
        if (pctCellAreas > 100) pctCellAreas = 100;
        cellAreaCount = areas.Count * pctCellAreas / 100;

        // #################### USEFUL LEVEL GENERATION INFO ####################
        // Debug.Log("Level type: " + levelType + ", Build pct." + buildPct);
        // Debug.Log("Fixed area number: " + fixedAreaNumber + ", AreaCount: " + areas.Count + ", cellAreaCount: " + cellAreaCount + ", Pct. cell areas: " + pctCellAreas);
        // Debug.Log("Ratio" + ratio + ", globalMaxAreaWidth: " + globalMaxAreaWidth + ", globalMaxAreaHeight: " + globalMaxAreaHeight);
        // #################### USEFUL LEVEL GENERATION INFO ####################

        // Randomly selecting the areas which will become cellular
        List<Area> tempAreas = new List<Area>(areas);
        int randomArea;
        int randomAreaID;
        for (int i = 0; i < cellAreaCount; i++) {
            randomArea = RandomInt(0, tempAreas.Count);
            randomAreaID = tempAreas[randomArea].areaID;
            cellAreas.Add(randomAreaID, 0);
            tempAreas.RemoveAt(randomArea);
        }
    }


    // This method will place an area in a random location (chosen among unused tiles).
    // The area will become smaller if the space is less.
    // And another location will be tried if there is not enough space for an area of minimum size.
    char[,] FitArea(Area tempArea) {
        int fittingAttempts = 0;
        char[,] tempAreaGrid = new char[tempArea.width, tempArea.height];
        char[,] finalAreaGrid = null;
        bool foundLoc = false;
        int[] tryLoc = new int[2];
        int countGoodH = 0;
        int countGoodV = 0;
        int minCountGoodV = tempArea.height;
        bool breakLoop = false;
        bool finalAreaGridFound = false;

        // Renewing the content (if not up to date) in unusedTilesList, since we need the list now.
        if (unUsedTileList.Count != unUsedTiles.Count) {
            List<string> tmpList = new List<string>();
            foreach (string itr in unUsedTileList) {
                if (unUsedTiles.ContainsKey(itr)) tmpList.Add(itr);
            }
            unUsedTileList = new List<string>(tmpList);
        }


        do {
            do {
                int randomUnUsedTile = RandomInt(0, unUsedTileList.Count);
                tryLoc = unUsedTileList[randomUnUsedTile].Split(',').Select(int.Parse).ToArray();
                tempArea.originHori = tryLoc[0];
                tempArea.originVert = tryLoc[1];
                if (fixedAreaNumber == 1) tempArea.originHori = tempArea.originVert = 1;
                foundLoc = true;
            }
            while (!foundLoc);

            for (int i = 0; i < tempArea.width; i++) {
                for (int j = 0; j < tempArea.height; j++) {
                    // If an area origin is chosen close to outer right/upper wall,
                    // the program will often try to check a coordinate larger(or =) the grid size (out of bounds error),
                    // so I stop that by limiting it to less than grid size.
                    if (i + tempArea.originHori < sizeH && j + tempArea.originVert < sizeV) {

                        // Here we are moving on the edges of the area.
                        if (i == 0 || i == tempArea.width - 1 ||
                            j == 0 || j == tempArea.height - 1)
                        {
                            // 'OuterWall' is not accepted.
                            if (levelGrid[i + tempArea.originHori, j + tempArea.originVert] == Symbol(Occupant.OuterWall)) {
                                breakLoop = true;
                                break;
                            }
                            else {
                                countGoodV++;
                                tempAreaGrid[i, j] = Symbol(Occupant.Wall);
                            }
                        }
                        // 'OuterWall' is not accepted and the loop is broken.
                        else if (levelGrid[i + tempArea.originHori, j + tempArea.originVert] == Symbol(Occupant.OuterWall)) {
                            breakLoop = true;
                            break;
                        }
                        // 'Wall' is accepted (countGoodH is increased) but the loop is broken (so we can continue with the next column).
                        else if (levelGrid[i + tempArea.originHori, j + tempArea.originVert] == Symbol(Occupant.Wall)) {
                            countGoodV++;
                            breakLoop = true;
                            break;
                        }
                        // In all other cases a (normal) 'used' tile is added to the temporary area grid.
                        else {
                            countGoodV++;
                            tempAreaGrid[i, j] = Symbol(Occupant.Used);
                        }
                    }

                }
                if (breakLoop) {
                    if (countGoodV < globalMinAreaSideLength - 1) // In this case there is not enough space for an area of minimumn size, so we have to try in a new location.
                        break;
                    else {
                        // We can still continue with the next column, even though the previous didn't come to full width.
                        countGoodH++;
                        minCountGoodV = Math.Min(countGoodV, minCountGoodV);
                        countGoodV = 0;
                    }
                }
                else {
                    // We continue as normally.
                    countGoodH++;
                    countGoodV = 0;
                }
            }

            // This block decides if the found area is big enough.
            // Then, if yes, transfers it to the level.
            if (countGoodH > globalMinAreaSideLength - 1 && minCountGoodV > globalMinAreaSideLength - 1) {
                // The size of the created area is often less than the random selection,
                // so the needed symbols are taken from the temporary grid into a final area-grid.
                finalAreaGrid = new char[countGoodH, minCountGoodV];

                for (int i = 0; i < countGoodH; i++) {
                    for (int j = 0; j < minCountGoodV; j++) {
                        // Building key-string.
                        string tileStr = (i + tempArea.originHori) + "," + (j + tempArea.originVert);
                        if (i == 0 || i == countGoodH - 1 || j == 0 || j == minCountGoodV - 1) {
                            // If we are on the edges of the area, a wall is added to the area.
                            finalAreaGrid[i, j] = Symbol(Occupant.Wall);
                            // If we are on an area corner, the location is added to a dictionary.
                            // We need this info for later.
                            if ((i == 0 && j == 0) ||
                                 (i == 0 && j == minCountGoodV - 1) ||
                                 (i == countGoodH - 1 && j == minCountGoodV - 1) ||
                                 (i == countGoodH - 1 && j == 0)
                               )
                                if (!cornerTiles.ContainsKey(tileStr)) cornerTiles.Add(tileStr, areas.Count);
                        }
                        else {
                            finalAreaGrid[i, j] = tempAreaGrid[i, j];
                            // Only tiles inside the walls are added.
                            coordToAreaID.Add(tileStr, areas.Count);
                            freeAreaTiles.Add(tileStr,areas.Count); // For use in populating the level.
                        }
                        // Removing the used tile from the dictionary, so the program will not attempt to use it again.
                        unUsedTiles.Remove(tileStr);
                        // Finally adding the tile to the level.
                        levelGrid[i + tempArea.originHori, j + tempArea.originVert] = finalAreaGrid[i, j];
                    }
                }
                finalAreaGridFound = true;
                // Adding the created area to the levels list of areas.
                areas.Add(new Area(areas.Count, AreaType.Room, "DefaultName", tempArea.originHori, tempArea.originVert,
                                   countGoodH, minCountGoodV, finalAreaGrid, new Dictionary<Direction, bool>(),
                                   new List <Direction>(){ Direction.North, Direction.East, Direction.South, Direction.West }, new string[4] )
                         );
                // XXXXX debug XXXXX Debug.Log("AREA DONE =======>  AreaID:" + (areas.Count-1) + ", missDir: " + areas[areas.Count-1].missingCorridors.Count); // XXXXX debug XXXXX
                // XXXXX debug XXXXX AddAreaDebugInfo(areas.Count - 1); // XXXXX debug XXXXX
            }
            else {
                // If we are here, it mean there was not enough space in the chosen location for an area of min size.
                // So we re-initialize and try again.
                foundLoc = false;
                countGoodH = 0;
                countGoodV = 0;
                minCountGoodV = tempArea.height;
                breakLoop = false;
                fittingAttempts++;
                // XXXXX debug XXXXX Debug.Log("Room not possible, trying again. Fitting attempt " + fittingAttempts + "."); // XXXXX debug XXXXX
                if (fittingAttempts >= maxAreaFittingAttempts) {
                    if (globalMinAreaSideLength > GLOBAL_GLOBAL) {
                        Debug.Log("WARNING: It was not possible to fit area #" + areas.Count + ".  " + fittingAttempts + " attempts were made.");
                        Debug.Log("The program is downsizing the 'Global-Minimum-Area-Side-Length' from " + globalMinAreaSideLength + " to " + (globalMinAreaSideLength - 1) + " to try to fit the area. ");
                        globalMinAreaSideLength -= 1;
                        fittingAttempts = 0;
                    }
                    else return null;
                }
            }
        }
        while (!finalAreaGridFound);
        return finalAreaGrid;
    }


    void GenerateCellular() {
        for (int i = 0; i < areas.Count; i++) {
            if (cellAreas.ContainsKey(i)) {
                int gridWidth = areas[i].areaGrid.GetLength(0);
                int gridHeight = areas[i].areaGrid.GetLength(1);
                int originH = areas[i].originHori;
                int originV = areas[i].originVert;
                Cellular(areas[i]);
                for (int j = 0; j < gridWidth; j++) {
                    for (int k = 0; k < gridHeight; k++) {
                        levelGrid[originH + j, originV + k] = areas[i].areaGrid[j, k];
                        if (areas[i].areaGrid[j, k] == Symbol(Occupant.Wall))
                            freeAreaTiles.Remove((originH + j) + "," + (originV + k));
                    }
                }
            }
        }
    }


    // This method iterates over all the areas in random order, 4 times (N,S,E,V)
    // In each round a random missing corridor direction is chosen for each area.
    void HandleCorridors() {
        for (int m = 0; m < 4; m++) {
            List<Area> tempAreas = new List<Area>(areas);
            for (int n = 0; n < areas.Count; n++) {
                int rndTempAreaIndex = RandomInt(0, tempAreas.Count);
                Area rndArea = tempAreas[rndTempAreaIndex];

                // Shrinking the tempAreas list by 1
                tempAreas.RemoveAt(rndTempAreaIndex);

                // We only do this, if this area is missing corridors.
                if (areas[rndArea.areaID].missingCorridors.Count > 0) {
                    // Picking a random direction among the ones missing.
                    int rndDirIndex = RandomInt(0, areas[rndArea.areaID].missingCorridors.Count);
                    Direction foundDirection = areas[rndArea.areaID].missingCorridors[rndDirIndex];

                    // #################### FOR DEBUGGING ####################
                    // XXXXX debug XXXXX Debug.Log("\n---------------------------------------------------------------");  // XXXXX debug XXXXX
                    // XXXXX debug XXXXX Debug.Log("n:" + n + ", areaID:" + areas[rndArea.areaID].areaID + ", Origin:" + areas[rndArea.areaID].originHori + "," + areas[rndArea.areaID].originVert + ", Missing corridors count:" + areas[rndArea.areaID].missingCorridors.Count); // XXXXX debug XXXXX
                    // XXXXX debug XXXXX string mdir = null; // XXXXX debug XXXXX
                    // XXXXX debug XXXXX foreach (Direction itr in areas[rndArea.areaID].missingCorridors) mdir += itr + " "; // XXXXX debug XXXXX
                    // XXXXX debug XXXXX Debug.Log("Area-ID:" + areas[rndArea.areaID].areaID + ", Going:" + foundDirection + ", rndDirIndex:" + rndDirIndex + ", missDirs:" + mdir); // XXXXX debug XXXXX
                    // #################### FOR DEBUGGING ####################

                    // Shrinking missing corridors list for the original area by 1
                    areas[rndArea.areaID].missingCorridors.RemoveAt(rndDirIndex);
                    int deltaH = 0;
                    int deltaV = 0;
                    int dimension = 0;
                    int arraySwitchH = 0;
                    int arraySwitchV = 0;

                    int[] startLocCellular = new int[2];
                    switch (foundDirection) {
                        case Direction.North:
                            arraySwitchH = 1;
                            deltaV = rndArea.height - 1;
                            dimension = rndArea.width;
                            if (cellAreas.ContainsKey(rndArea.areaID)){
                                string coordStr = rndArea.cellAreaCorridorStartCoords[3];
                                if (coordStr.Length > 0)
                                    startLocCellular = coordStr.Split(',').Select(int.Parse).ToArray();
                                else Debug.Log("Warning ! Coordinates missing for cellular area corridor.");
                            }
                            break;
                        case Direction.East:
                            arraySwitchV = 1;
                            deltaH = rndArea.width - 1;
                            dimension = rndArea.height;
                            if (cellAreas.ContainsKey(rndArea.areaID)) {
                                string coordStr = rndArea.cellAreaCorridorStartCoords[1];
                                if (coordStr.Length > 0)
                                    startLocCellular = coordStr.Split(',').Select(int.Parse).ToArray();
                                else Debug.Log("Warning ! Coordinates missing for cellular area corridor.");
                            }
                            break;
                        case Direction.South:
                            arraySwitchH = 1;
                            dimension = rndArea.width;
                            if (cellAreas.ContainsKey(rndArea.areaID)) {
                                string coordStr = rndArea.cellAreaCorridorStartCoords[2];
                                if (coordStr.Length > 0)
                                    startLocCellular = coordStr.Split(',').Select(int.Parse).ToArray();
                                else Debug.Log("Warning ! Coordinates missing for cellular area corridor.");
                            }
                            break;
                        case Direction.West:
                            arraySwitchV = 1;
                            dimension = rndArea.height;
                            if (cellAreas.ContainsKey(rndArea.areaID)) {
                                string coordStr = rndArea.cellAreaCorridorStartCoords[0];
                                if (coordStr.Length > 0)
                                    startLocCellular = coordStr.Split(',').Select(int.Parse).ToArray();
                                else Debug.Log("Warning ! Coordinates missing for cellular area corridor.");
                            }
                            break;
                    }
                    // Handling rectangular areas.
                    if (!cellAreas.ContainsKey(rndArea.areaID)) {
                            // Picking a random location on the chosen wall, from where to begin the corridor.
                            int pointIndex = RandomInt(3, dimension - 4);
                            int startH = rndArea.originHori + deltaH + pointIndex * arraySwitchH;
                            int startV = rndArea.originVert + deltaV + pointIndex * arraySwitchV;
                            bool corridorDone = DigCorridor(rndArea, startH, startV, foundDirection, digIrregular);
                    }
                    // Handling cellular areas.
                    else {
                        int startH = startLocCellular[0];
                        int startV = startLocCellular[1];
                        DigCorridor(rndArea, startH, startV, foundDirection, digIrregular);
                    }

                }
            }
        }
    }


    bool DigCorridor(Area rndArea, int startCoordH, int startCoordV, Direction direction, bool irreg) {
        int stepsBack = 1;
        int explosion = 1;  // Decides the width when creating a small space (to ensure that the avatar can pass).
        int digLRmax = 1;   // This variable sets the MAXIMUM L/R digging in each step (total width becomes: 1 + digLRmax * 2).
        int digLeft;    // (Random number) How many tiles will the digger dig LEFT, in the curret step.
        int digRight;   // (Random number) How many tiles will the digger dig RIGHT, in the curret step.
        int moveLeftRight; // Can be -1, 0 or 1 (the corridor only shifts 1 tile L/R at a time).
        int minH = rndArea.originHori + digLRmax + 1;                   // Overall left horizontal bound for this corridor.
        int maxH = rndArea.originHori + rndArea.width - digLRmax - 1;   // Overall right horizontal bound for this corridor.
        int minV = rndArea.originVert + digLRmax + 1;                   // Overall bottom vertical bound for this corridor.
        int maxV = rndArea.originVert + rndArea.height - digLRmax - 1;  // Overall top vertical bound for this corridor.
        List<char> charsThisStep = new List<char>();        // Encountered chars, in the levelGrid, in the current step.1
        List<string> coordsThisStep = new List<string>();   // Coordinates of the tiles/chars.
        List<char> corridorChars = new List<char>();        // This list accumulates chars until the corridor is done.
        List<string> corridorCoords = new List<string>();   // This list accumulates coordinates until the corridor is done.
        bool corridorDone = false;
        int coordH = startCoordH;
        int coordV = startCoordV;

        // When the digger enters an area, regardless if it's presently touching the used (not wall) tiles.
        // Then it registers the direction opposite to the one it's entering from.
        // That direction will be kept until it enters another area, or reenters the same area.
        List<Direction> oppMissDir = new List<Direction>();
        // This is the last area entered, for starters we set it to where we are coming from.
        int lastAreaID = rndArea.areaID; ;

        switch (direction) {
            case Direction.North:
                for (int j = -stepsBack; j < 1; j++) {
                    for (int i = -explosion; i < explosion + 1; i++) {
                        // levelGrid[coordH + i, coordV + j] = Symbol(Occupant.Used);
                        corridorCoords.Add((coordH + i) + "," + (coordV + j));
                        corridorChars.Add(levelGrid[coordH + i, coordV + j]);
                    }
                }
                break;
            case Direction.East:
                for (int i = -stepsBack; i < 1; i++) {
                    for (int j = -explosion; j < explosion + 1; j++) {
                        // levelGrid[coordH + i, coordV + j] = Symbol(Occupant.Used);
                        corridorCoords.Add((coordH + i) + "," + (coordV + j));
                        corridorChars.Add(levelGrid[coordH + i, coordV + j]);
                    }
                }
                break;
            case Direction.South:
                for (int j = stepsBack; j > -1; j--) {
                    for (int i = explosion; i > -explosion - 1; i--) {
                        // levelGrid[coordH + i, coordV + j] = Symbol(Occupant.Used);
                        corridorCoords.Add((coordH + i) + "," + (coordV + j));
                        corridorChars.Add(levelGrid[coordH + i, coordV + j]);
                    }
                }
                break;
            case Direction.West:
                for (int i = stepsBack; i > -1; i--) {
                    for (int j = explosion; j > -explosion - 1; j--) {
                        // levelGrid[coordH + i, coordV + j] = Symbol(Occupant.Used);
                        corridorCoords.Add((coordH + i) + "," + (coordV + j));
                        corridorChars.Add(levelGrid[coordH + i, coordV + j]);
                    }
                }
                break;
        }


        while (!corridorDone)  {
            digLeft = RandomInt(1, digLRmax + 1);   // How far to dig left from 'axis-tile'.
            digRight = RandomInt(1, digLRmax + 1);  // How far to dig right from 'axis-tile'.
            moveLeftRight = RandomInt(-1, 2);       // Do we stay on course or move L/R.
            // Digging 1 tile forward, perhaps moving the 'axis-tile' sidewards, and also digging to the sides from that.
            switch (direction) {
                case Direction.North:
                    coordV++;
                    if ((coordH > minH || moveLeftRight >= 0) && (coordH < maxH || moveLeftRight <= 0) && irreg)
                        coordH += moveLeftRight;
                    for (int i = -digLeft; i < digRight + 1; i++) {
                        coordsThisStep.Add((coordH + i) + "," + coordV);
                        charsThisStep.Add(levelGrid[coordH + i, coordV]);
                    }
                    break;
                case Direction.East:
                    coordH++;
                    if ((coordV < maxV || moveLeftRight <= 0) && (coordV > minV || moveLeftRight >= 0) && irreg)
                        coordV += moveLeftRight;
                    for (int i = digLeft; i > -digRight - 1; i--) {
                        coordsThisStep.Add(coordH + "," + (coordV + i));
                        charsThisStep.Add(levelGrid[coordH, coordV + i]);
                    }
                    break;
                case Direction.South:
                    coordV--;
                    if ((coordH < maxH || moveLeftRight <= 0) && (coordH > minH || moveLeftRight >= 0) && irreg)
                        coordH += moveLeftRight;
                    for (int i = digLeft; i > -digRight - 1; i--) {
                        coordsThisStep.Add((coordH + i) + "," + coordV);
                        charsThisStep.Add(levelGrid[coordH + i, coordV]);
                    }
                    break;
                case Direction.West:
                    coordH--;
                    if ((coordV > minV || moveLeftRight >= 0) && (coordV < maxV || moveLeftRight <= 0) && irreg)
                        coordV += moveLeftRight;
                    for (int i = -digLeft; i < digRight + 1; i++) {
                        coordsThisStep.Add(coordH + "," + (coordV + i));
                        charsThisStep.Add(levelGrid[coordH, coordV + i]);
                    }
                    break;
            }
            int areaIDthisStep = InOtherArea(coordsThisStep);
            bool thisIsAreaSpace = false;
            if (areaIDthisStep != rndArea.areaID && areaIDthisStep >= 0) {
                if (areaIDthisStep != lastAreaID) {
                    lastAreaID = areaIDthisStep;
                    oppMissDir = new List<Direction>(FindOppMissDir(lastAreaID, coordsThisStep));
                }
                string foundSpace = default;

                // #################### FOR DEBUGGING ####################
                // XXXXX debug XXXXX Debug.Log("A_ID:" + rndArea.areaID + ", corridor count:" + rndArea.corridors.Count + ", oppMissDirCount:" + oppMissDir.Count);  // XXXXX debug XXXXX
                // XXXXX debug XXXXX Debug.Log("In area with ID:" + areaIDthisStep);  // XXXXX debug XXXXX
                // XXXXX debug XXXXX if (rndArea.corridors.ContainsKey(Direction.North)) Debug.Log("A_ID:" + rndArea.areaID + " has " + Direction.North + " = " + rndArea.corridors[Direction.North] );  // XXXXX debug XXXXX
                // XXXXX debug XXXXX if (rndArea.corridors.ContainsKey(Direction.South)) Debug.Log("A_ID:" + rndArea.areaID + " has " + Direction.South + " = " + rndArea.corridors[Direction.South]);  // XXXXX debug XXXXX
                // XXXXX debug XXXXX if (rndArea.corridors.ContainsKey(Direction.East)) Debug.Log("A_ID:" + rndArea.areaID + " has " + Direction.East + " = " + rndArea.corridors[Direction.East]);  // XXXXX debug XXXXX
                // XXXXX debug XXXXX if (rndArea.corridors.ContainsKey(Direction.West)) Debug.Log("A_ID:" + rndArea.areaID + " has " + Direction.West + " = " + rndArea.corridors[Direction.West]);  // XXXXX debug XXXXX
                // #################### FOR DEBUGGING ####################

                if (oppMissDir.Count > 0 || (rndArea.corridors.Count > 2 && !rndArea.corridors.ContainsValue(true))) {
                    // We iterate over the found chars and pick only 1 which meets the conditions.
                    // This is to prevent trying to add  the same corridor more than 1 time.
                    for (int i = 0; i < charsThisStep.Count; i++) {
                        if (charsThisStep[i] == Symbol(Occupant.Used) && coordToAreaID.ContainsKey(coordsThisStep[i])) {
                            thisIsAreaSpace = true;
                            foundSpace = coordsThisStep[i];
                        }
                    }
                    if (thisIsAreaSpace) {
                        // Updating corridors and directions, both for this area and the encountered area.
                        if (oppMissDir.Count > 0) {
                            // #################### FOR DEBUGGING ####################
                            // XXXXX debug XXXXX Debug.Log("foundSpace:" + foundSpace);  // XXXXX debug XXXXX
                            // XXXXX debug XXXXX Debug.Log("OtherA missing corridors count:" + areas[coordToAreaID[foundSpace]].missingCorridors.Count); // XXXXX debug XXXXX
                            // #################### FOR DEBUGGING ####################
                            areas[coordToAreaID[foundSpace]].missingCorridors.Remove(oppMissDir[0]);
                            if (!areas[coordToAreaID[foundSpace]].corridors.ContainsKey(oppMissDir[0]))
                                areas[coordToAreaID[foundSpace]].corridors.Add(oppMissDir[0], true);
                        }
                        List<string> addExplosion = new List<string>(Explosion_3by3(foundSpace));
                        foreach (string itr in addExplosion) {
                            corridorCoords.Add(itr);
                            corridorChars.Add(Symbol(Occupant.Used));
                        }
                        rndArea.corridors.Add(direction, true);
                        corridorDone = true;
                        // #################### FOR DEBUGGING ####################
                        // XXXXX debug XXXXX string mdir = null; // XXXXX debug XXXXX
                        // XXXXX debug XXXXX foreach (Direction itr in areas[coordToAreaID[foundSpace]].missingCorridors) mdir += itr + " "; // XXXXX debug XXXXX
                        // XXXXX debug XXXXX Debug.Log("Other A-ID:" + areas[coordToAreaID[foundSpace]].areaID + ", missDirs:" + mdir); // XXXXX debug XXXXX
                        // XXXXX debug XXXXX Debug.Log("CAN_BUILD__ThisA: " + rndArea.areaID + ", MyDir:" + direction + ", OtherA: " + areas[coordToAreaID[foundSpace]].areaID); // XXXXX debug XXXXX
                        // XXXXX debug XXXXX Debug.Log("ThisA_origin: " + rndArea.originHori + "," + rndArea.originVert); // XXXXX debug XXXXX
                        // #################### FOR DEBUGGING ####################
                    }
                    else {
                        for (int i = 0; i < charsThisStep.Count; i++) {
                            if (charsThisStep[i] == Symbol(Occupant.UnUsed) || charsThisStep[i] == Symbol(Occupant.Wall)) {
                                corridorCoords.Add(coordsThisStep[i]);
                                corridorChars.Add(charsThisStep[i]);
                            }
                            else if (charsThisStep[i] == Symbol(Occupant.OuterWall)) {
                                rndArea.corridors.Add(direction, false);
                                corridorDone = true;
                                break; // new break !!!
                            }
                        }
                    }
                }
                else {
                    rndArea.corridors.Add(direction, false);
                    corridorDone = true;
                    // #################### FOR DEBUGGING ####################
                    /**
                    if (foundSpace != null) {
                        if (oppMissDir.Count > 0)
                            Debug.Log("OppMissDir: " + oppMissDir[0] + ", NO_BUILD__ThisA: " + rndArea.areaID + ", OtherA: " + areas[coordToAreaID[foundSpace]].areaID); // XXXXX debug XXXXX
                        else if (coordToAreaID.ContainsKey(foundSpace))
                            Debug.Log("OppMissDir: " + "(no oppMissDir!)" + ", NO_BUILD, I have ID:" + rndArea.areaID + ", OtherA: " + areas[coordToAreaID[foundSpace]].areaID); // XXXXX debug XXXXX
                    }
                    **/
                    // #################### FOR DEBUGGING ####################
                }
            }
            else if (areaIDthisStep == rndArea.areaID || areaIDthisStep < 0) {
                for (int i = 0; i < charsThisStep.Count; i++) { 
                    if (charsThisStep[i] == Symbol(Occupant.UnUsed) || charsThisStep[i] == Symbol(Occupant.Wall) ) {
                        corridorCoords.Add(coordsThisStep[i]);
                        corridorChars.Add(charsThisStep[i]);
                    }
                    else if (charsThisStep[i] == Symbol(Occupant.OuterWall)) {
                        rndArea.corridors.Add(direction, false);
                        corridorDone = true;
                        break; // new break !!!
                    }
                    // We are not in another area, that means we have encountered another corridor.
                    else if (charsThisStep[i] == Symbol(Occupant.Used)) {
                        rndArea.corridors.Add(direction, true);
                        // Adding some space to make sure the two corridors are fully connected.
                        // A little fifling here.
                        // I need to know if we have more than 2 chars in this step.
                        // If we have, then I will select the 2nd for when making 'explosions'.
                        // (To make sure the corridor is still passable when removing dead ends.)
                        string middleCoords;
                        if (coordsThisStep.Count > 2) middleCoords = coordsThisStep[1];
                        else middleCoords = coordsThisStep[i];
                        // -------------------------------------------------------
                        List<string> addExplosion = new List<string>(Explosion_3by3(middleCoords));
                        foreach (string itr in addExplosion) {
                            if (!freeCorridorTiles.ContainsKey(itr)) {
                                freeCorridorTiles.Add(itr, freeCorridorTiles.Count);
                            }
                            corridorCoords.Add(itr);
                            corridorChars.Add(Symbol(Occupant.Used));
                            // Keeping a 'mark' here, to be used for removing dead ends.
                            if (!corridorIntersections.ContainsKey(itr))
                                corridorIntersections.Add(itr, corridorIntersections.Count);
                        }    
                        corridorDone = true;
                        break; // new break !!!
                    }
                    else Console.WriteLine("Warning! Digging function found unexpected tile with char: ->" + charsThisStep[i] + "<-");
                }
            }
            // Re-initialization
            charsThisStep.Clear();
            coordsThisStep.Clear();
        }
        if (corridorDone) {
            int[] coords = new int[2];
            for (int i = 0; i < corridorCoords.Count; i++) {
                // ##### DEBUG ##### Debug.Log("corridor coords:" + corridorCoords[i]); // ##### DEBUG #####
                coords = corridorCoords[i].Split(',').Select(int.Parse).ToArray();
                levelGrid[coords[0], coords[1]] = Symbol(Occupant.Used);

                // Maintaining tile containers.
                // Only allowed if itr is not in freeAreaTiles XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                if (!freeCorridorTiles.ContainsKey(corridorCoords[i]) && !freeAreaTiles.ContainsKey(corridorCoords[i])) {
                    freeCorridorTiles.Add(corridorCoords[i], freeCorridorTiles.Count);
                }
                unUsedTiles.Remove(corridorCoords[i]);
            }
            if (rndArea.corridors[direction] == false) {
                deadEndCoordsList.Add(corridorCoords);
                deadEndCharssList.Add(corridorChars);
            }

        }
        else Debug.Log("Warning! Corridor not completed.");
        return corridorDone;
    }


    // ###################################################################################################################
    // ######################################## DIGGING SUPPORT FUNCTIONS - BEGIN ########################################

    int InOtherArea(List<string> coordsThisStep) {
        int foundID = -1;
        foreach (string itr in coordsThisStep) {
            if (coordToAreaID.ContainsKey(itr)) foundID = coordToAreaID[itr];
        }
        return foundID;
    }


    List<string> Explosion_3by3(string givenCoords) {
        List<string> explosion = new List<string>();
        int[] coords = new int[2];
        coords = givenCoords.Split(',').Select(int.Parse).ToArray();
        int[] newCoords = new int[2];
        for (int i = -1; i < 2; i++) {
            for (int j = -1; j < 2; j++) {
                newCoords[0] = coords[0] + i;
                newCoords[1] = coords[1] + j;
                explosion.Add(newCoords[0] + "," + newCoords[1]);
            }
        }
        return explosion;
    }


    List<Direction> FindOppMissDir(int inThisArea, List<string> coordsThisStep) {
        int[] coords = new int[2];
        List<Direction> oppMissDir = new List<Direction>();
        Area encounteredArea = areas[inThisArea];
        int areaHmin = encounteredArea.originHori + 1;
        int areaHmax = encounteredArea.originHori + encounteredArea.width - 2;
        int areaVmin = encounteredArea.originVert + 1;
        int areaVmax = encounteredArea.originVert + encounteredArea.height - 2;
        for (int i = 0; i < coordsThisStep.Count; i++) {
            coords = coordsThisStep[i].Split(',').Select(int.Parse).ToArray();
            if (coords[0] == areaHmin && coords[1] >= areaVmin && coords[1] <= areaVmax) {
                if (encounteredArea.missingCorridors.Contains(Direction.West)) {
                    oppMissDir.Add(Direction.West);
                    return oppMissDir;
                }
            }
            if (coords[0] == areaHmax && coords[1] >= areaVmin && coords[1] <= areaVmax) {
                if (encounteredArea.missingCorridors.Contains(Direction.East)) {
                    oppMissDir.Add(Direction.East);
                    return oppMissDir;
                }
            }
            if (coords[0] >= areaHmin && coords[0] <= areaHmax && coords[1] == areaVmin) {
                if (encounteredArea.missingCorridors.Contains(Direction.South)) {
                    oppMissDir.Add(Direction.South);
                    return oppMissDir;
                }
            }
            if (coords[0] >= areaHmin && coords[0] <= areaHmax && coords[1] == areaVmax) {
                if (encounteredArea.missingCorridors.Contains(Direction.North)) {
                    oppMissDir.Add(Direction.North);
                    return oppMissDir;
                }
            }
        }
        return oppMissDir;
    }


    void RemoveDeadEnds() {
        int[] coords = new int[2];
        //for (int i = 0; i < deadEndCoordsList.Count; i++) {
        for (int i = deadEndCoordsList.Count - 1; i > -1; i--) {

            int oneListLength = deadEndCoordsList[i].Count;
            for (int j = oneListLength - 1; j > -1; j--) {

                if (!corridorIntersections.ContainsKey(deadEndCoordsList[i][j])) {
                    if (freeCorridorTiles.ContainsKey(deadEndCoordsList[i][j])) {
                        // Updating corridor tiles.
                        freeCorridorTiles.Remove(deadEndCoordsList[i][j]);
                        // Updating levelGrid.
                        coords = deadEndCoordsList[i][j].Split(',').Select(int.Parse).ToArray();
                        levelGrid[coords[0], coords[1]] = deadEndCharssList[i][j];

                        // I used to: unUsedTiles.Add(deadEndCoordsList[i][j], 0); // Putting coords back in unUsedTileList.
                        // But unused tiles is not needed anymore since level generation is nearly finished.
                    }
                }
                else break;
            }

        }
        int dummyVar = unUsedTiles.Count; // I just keep this line, so I can find where I used to change unUsedTiles (above).
    }


    void NeighborSearch() {
        List<string> tilesToBeChanged = new List<string>();
        List<Direction> allDirections = new List<Direction>() { Direction.North, Direction.East, Direction.South, Direction.West };
        string neighborStr = default;
        int deltaH;
        int deltaV;

        // RegEx expressions to match the possible tiles.
        string usedTile = String.Format(@"{0}", Symbol(Occupant.Used));
        string unUsedTile = String.Format(@"{0}", Symbol(Occupant.UnUsed));

        for (int i = 1; i < levelGrid.GetLength(0) - 1; i++) {
            for (int j = 1; j < levelGrid.GetLength(1) - 1; j++) {
                foreach (Direction itr in allDirections) {
                    deltaH = 0;
                    deltaV = 0;
                    switch (itr) {
                        case Direction.North:
                            deltaV = 1;
                            if (j + deltaV > levelGrid.GetLength(1) - 1) deltaV = 0;
                            neighborStr += levelGrid[i + deltaH, j + deltaV];
                            break;
                        case Direction.East:
                            deltaH = 1;
                            if (i + deltaH > levelGrid.GetLength(0) - 1) deltaH = 0;
                            neighborStr += levelGrid[i + deltaH, j + deltaV];
                            break;
                        case Direction.South:
                            deltaV = -1;
                            if (j + deltaV > levelGrid.GetLength(1) - 1) deltaV = 0;
                            neighborStr += levelGrid[i + deltaH, j + deltaV];
                            break;
                        case Direction.West:
                            deltaH = -1;
                            if (i + deltaH > levelGrid.GetLength(0) - 1) deltaH = 0;
                            neighborStr += levelGrid[i + deltaH, j + deltaV];
                            break;
                    }
                }
                // Finding matches of the relevant tiles in the neighbor string.
                var usedTileMatches = Regex.Matches(neighborStr, usedTile);
                var unUsedTileMatches = Regex.Matches(neighborStr, unUsedTile);
                neighborStr = "";

                // Finding the used tiles which has 3 unused neighbor tiles.
                // This will make the 'outside' of the corridors (before wall-tiles are filled in, instead of unused tiles) look less jaggy.
                // A matter of taste of course.
                if (unUsedTileMatches.Count == 3)  {
                    tilesToBeChanged.Add(i + "," + j);
                }
            }
        }
        foreach (string itr in tilesToBeChanged) {
            int[] coords = itr.Split(',').Select(int.Parse).ToArray();
            levelGrid[coords[0], coords[1]] = Symbol(Occupant.UnUsed);
            if (freeCorridorTiles.ContainsKey(itr)) freeCorridorTiles.Remove(itr);

            // I used to: unUsedTiles.Add(itr, 0);
            // But unused tiles is not needed anymore since level generation is nearly finished.
        }
        int dummyVar = unUsedTiles.Count; // I just keep this line, so I can find where I used to change unUsedTiles (above).
    }


    // When this method is run, the levelgeneration has to be finished.
    // All the tiles which was not used for levelgeneraiton (UnUsed) is changed into Wall.
    // The OuterWall tiles is also changed into Wall since the TileSwitcher will check for neighbors onto the OuterWall.
    // Remaining is only Occupant.Used, which represents space to the player, and Occupant.Wall, which represents walls to the player.
    // -------->  So now the levelGrid, consisting only of chars, is ready to be used to instantiate gameobjects with sprites.
    void AddWalls() {
        for (int i = 0; i < sizeH; i++) {
            for (int j = 0; j < sizeV; j++) {
                if (levelGrid[i, j] == Symbol(Occupant.UnUsed)) {
                    levelGrid[i, j] = Symbol(Occupant.Wall);

                    // I used to: unUsedTiles.Remove(i + "," + j);
                    // But unused tiles is not needed anymore since level generation is nearly finished.
                }
                else if (levelGrid[i, j] == Symbol(Occupant.OuterWall)) levelGrid[i, j] = Symbol(Occupant.Wall);
            }
        }
        int dummyVar = unUsedTiles.Count; // I just keep this line, so I can find where I used to change unUsedTiles (above).
    }

    // ######################################## DIGGING SUPPORT FUNCTIONS - END ########################################
    // #################################################################################################################

    protected GameObject TileSwitcher(int hori, int vert) {
        GameObject tile_gameObject_1 = Resources.Load<GameObject>("Tile_GameObject_1");
        SpriteRenderer rend = tile_gameObject_1.GetComponent<SpriteRenderer>();

        bool up = true;
        bool down = true;
        bool left = true;
        bool right = true;
        // TileType foundTileType;
        Sprite foundSprite = default;
        List<Direction> allDirections = new List<Direction>() { Direction.North, Direction.East, Direction.South, Direction.West };

        foreach (Direction itr in allDirections) {
            switch (itr) {
                case Direction.North:
                    if (levelGrid[hori, vert + 1] == Symbol(Occupant.Wall)) up = false;
                    break;
                case Direction.East:
                    if (levelGrid[hori + 1, vert] == Symbol(Occupant.Wall)) right = false;
                    break;
                case Direction.South:
                    if (levelGrid[hori, vert - 1] == Symbol(Occupant.Wall)) down = false;
                    break;
                case Direction.West:
                    if (levelGrid[hori - 1, vert] == Symbol(Occupant.Wall)) left = false;
                    break;
            }
        }
        // Alone or Center
        if (up && down && left && right) {
            //foundTileType = TileType.Alone;
            foundSprite = tileSpritesGreen[0];
            singleSpawnLocs.Add(hori + "," + vert, 0);
        }
        else if (!up && !down && !left && !right) {
            //foundTileType = TileType.Center;
            foundSprite = tileSpritesGreen[1];
            centerSpawnLocs.Add(hori + "," + vert, 0);
        }
        // Corners
        else if (up && !down && left && !right) {
            //foundTileType = TileType.UL_Corner;
            foundSprite = tileSpritesGreen[2];
            floorSpawnLocs.Add(hori + "," + (vert + 1), 0);
        }
        else if (up && !down && !left && right) {
            //foundTileType = TileType.UR_Corner;
            foundSprite = tileSpritesGreen[3];
            floorSpawnLocs.Add(hori + "," + (vert + 1), 0);
        }
        else if (!up && down && left && !right) {
            //foundTileType = TileType.LL_Corner;
            foundSprite = tileSpritesGreen[4];
            ceilingSpawnLocs.Add(hori + "," + (vert - 1), 0);
        }
        else if (!up && down && !left && right) {
            //foundTileType = TileType.LR_Corner;
            foundSprite = tileSpritesGreen[5];
            ceilingSpawnLocs.Add(hori + "," + (vert - 1), 0);
        }
        // Sides
        else if (up && !down && !left && !right) {
            //foundTileType = TileType.Upper_Side;
            foundSprite = tileSpritesGreen[6];
            floorSpawnLocs.Add(hori + "," + (vert + 1), 0);
        }
        else if (!up && down && !left && !right) {
            //foundTileType = TileType.Lower_Side;
            foundSprite = tileSpritesGreen[7];
            ceilingSpawnLocs.Add(hori + "," + (vert - 1), 0);
        }
        else if (!up && !down && left && !right) {
            //foundTileType = TileType.Left_Side;
            foundSprite = tileSpritesGreen[8];
            if (!sideSpawnLocs.ContainsKey((hori - 1) + "," + vert))
                sideSpawnLocs.Add((hori - 1) + "," + vert, 0);
        }
        else if (!up && !down && !left && right) {
            //foundTileType = TileType.Right_Side;
            foundSprite = tileSpritesGreen[9];
            if (sideSpawnLocs.ContainsKey((hori + 1) + "," + vert))
                sideSpawnLocs.Add((hori + 1) + "," + vert, 0);
        }
        // Singles
        else if (up && !down && left && right) {
            //foundTileType = TileType.Upper_Single;
            foundSprite = tileSpritesGreen[10];
            floorSpawnLocs.Add(hori + "," + (vert + 1), 0);
        }
        else if (!up && down && left && right) {
            //foundTileType = TileType.Lower_Single;
            foundSprite = tileSpritesGreen[11];
            ceilingSpawnLocs.Add(hori + "," + (vert - 1), 0);
        }
        else if (up && down && left && !right) {
            //foundTileType = TileType.Left_Single;
            foundSprite = tileSpritesGreen[12];
            ceilingSpawnLocs.Add(hori + "," + (vert - 1), 0);
        }
        else if (up && down && !left && right) {
            //foundTileType = TileType.Right_Single;
            foundSprite = tileSpritesGreen[13];
            ceilingSpawnLocs.Add(hori + "," + (vert - 1), 0);
        }
        // Platforms
        else if (up && down && !left && !right) {
            //foundTileType = TileType.Horizontal_Platform;
            foundSprite = tileSpritesGreen[14];
            floorSpawnLocs.Add(hori + "," + (vert + 1), 0);
            ceilingSpawnLocs.Add(hori + "," + (vert - 1), 0);
        }
        else if (!up && !down && left && right) {
            //foundTileType = TileType.Vertical_Platform;
            foundSprite = tileSpritesGreen[15];
            sideSpawnLocs.Add((hori - 1) + "," + vert, 0);
            sideSpawnLocs.Add((hori + 1) + "," + vert, 0);
        }
        rend.sprite = foundSprite;
        return tile_gameObject_1;
    }

    

}
