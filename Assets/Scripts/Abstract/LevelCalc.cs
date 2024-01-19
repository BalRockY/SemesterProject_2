using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

abstract class LevelCalc : TileSymbol {

    public enum AreaType { Corridor, Room, Bedroom, Treasury };
    public enum Direction { North, East, South, West };
    protected Random _random;

    protected override void Awake() {
        base.Awake();
    }

    protected int SetSeed(int customSeed) {
        int seedUsed;
        // Custom seed, for random numbers.
        if (customSeed > 0) {
            _random = new Random(customSeed);
            seedUsed = customSeed;
            // Debug.Log("Custom seed used: " + customSeed); // In case I want to reproduce a level.
        }
        // Random seed, for random numbers.
        else {
            DateTime timeNow = DateTime.Now;
            DateTime timeBeginningOfToday = DateTime.Now.Date;
            int milliSecondsToday = (int)((timeNow - timeBeginningOfToday).TotalMilliseconds);
            _random = new Random(milliSecondsToday);
            seedUsed = milliSecondsToday;
            // Debug.Log("Random seed used: " + milliSecondsToday); // In case I want a new level.
        }
        return seedUsed;
    }


    public int RandomInt(int min, int max) {
        int myInt = _random.Next(min, max);
        return myInt;
    }


    public struct Area {
        public int areaID;
        public string name;         // Each area can have a name (for game purposes).
        public AreaType areaType;
        public int originHori;      // The leftmost coordinate for the area.
        public int originVert;      // The lowermost coordinate for the area.
        public int width;           // Width including 2 units for walls.
        public int height;          // Height including 2 units for walls.
        public char[,] areaGrid;    // Each area has a grid with the characters it contains.

        // The completed corridors areplaced in a Dictionary because I want to access by key.
        public Dictionary<Direction, bool> corridors;

        // For missing corridors I use a List, because I want to access by index (random selection, then removal).
        public List<Direction> missingCorridors;

        // Start coordinates for corridors, in case the area is a cellular area.
        public string[] cellAreaCorridorStartCoords;


    
        public Area(int ID, AreaType type, string areaName, int originH, int originV, int w, int h,
                    char[,] aGrid, Dictionary<Direction, bool> corrs, List<Direction> missCoors, string[] areaCorridorStartCoords)
        {
            areaID = ID;
            areaType = type;
            name = areaName;
            originHori = originH;
            originVert = originV;
            width = w;
            height = h;
            areaGrid = aGrid;
            corridors = corrs;
            missingCorridors = missCoors;
            cellAreaCorridorStartCoords = areaCorridorStartCoords;
        }
    
    }

    protected void Cellular(Area area) {
        //Debug.Log("--------------------------------           Area Data          ---------------------------------");
        GenerateNoise(area.areaGrid);
        //Debug.Log("-------------------------------- Extreme space-tile positions ---------------------------------");
        CreateSpace(area.areaGrid, 1.0f, 0.05f);
        CreateSpace(area.areaGrid, 0.05f, 1.0f);
        Iterate(area, 1, 5, 2, 2, 4);
        Iterate(area, 1, 5, 0, 0, 3);
        FindCorrPos2(area);
    }

    void Iterate(Area area, int nbDistA, int nbCountA, int nbDistB, int nbCountB, int iterations) {
        char wall = Symbol(Occupant.Wall);
        char space = Symbol(Occupant.Used);
        int sizeH = area.areaGrid.GetLength(0);
        int sizeV = area.areaGrid.GetLength(1);
        for (int h = 0; h < iterations; h++) {
            for (int i = 1; i < sizeH - 1; i++) {
                for (int j = 1; j < sizeV - 1; j++) {
                    if (nbDistA == 1 && nbCountA > 0 && nbDistB == 0 && nbCountB == 0) {
                        if (NbWallCount(area.areaGrid, i, j, nbDistA) >= nbCountA) area.areaGrid[i, j] = wall;
                        else area.areaGrid[i, j] = space;
                    }
                    else if (nbDistA == 1 && nbCountA > 0 && nbDistB == 2 && nbCountB >= 0) {
                        if (NbWallCount(area.areaGrid, i, j, nbDistA) >= nbCountA || NbWallCount(area.areaGrid, i, j, nbDistB) <= nbCountB) area.areaGrid[i, j] = wall;
                        else area.areaGrid[i, j] = space;
                    }
                }
            }
        }
    }

    void FindCorrPos(Area area) {
        char space = Symbol(Occupant.Used);
        int sizeH = area.areaGrid.GetLength(0);
        int sizeV = area.areaGrid.GetLength(1);
        int horiMin = (int)sizeH / 2;
        int horiMax = (int)sizeH / 2;
        int vertMin = (int)sizeV / 2;
        int vertMax = (int)sizeV / 2;
        int digLRmax = 2;
        for (int i = 1; i < sizeH - 1; i++) {
            for (int j = 1; j < sizeV - 1; j++) {
                if (area.areaGrid[i, j] == space) {
                    if (j > digLRmax && j < sizeV - 1 - digLRmax) {
                        if (i < horiMin) {
                            horiMin = i;
                            area.cellAreaCorridorStartCoords[0] = (area.originHori + horiMin) + "," + (area.originVert + j);
                        }
                        if (i > horiMax) {
                            horiMax = i;
                            area.cellAreaCorridorStartCoords[1] = (area.originHori + horiMax) + "," + (area.originVert + j);
                        }
                    }
                    if (i > digLRmax && i < sizeH - 1 - digLRmax) {
                        if (j < vertMin) {
                            vertMin = j;
                            area.cellAreaCorridorStartCoords[2] = (area.originHori + i) + "," + (area.originVert + vertMin);
                        }
                        else if (j > vertMax) {
                            vertMax = j;
                            area.cellAreaCorridorStartCoords[3] = (area.originHori + i) + "," + (area.originVert + vertMax);
                        }
                    }
                }
            }
        }
        // ##### DEBUG ##### Debug.Log("sizeHV:" + area.width + "," + area.height + "<--->" + area.originHori + ", " + area.originVert + "<---horiMin:" + horiMin + ", horiMax:" + horiMax + ", vertMin:" + vertMin + ", vertMax:" + vertMax); // ##### DEBUG ##### 
    }



    void FindCorrPos2(Area area) {
        char space = Symbol(Occupant.Used);
        int sizeH = area.areaGrid.GetLength(0);
        int sizeV = area.areaGrid.GetLength(1);
        // These 4 variables will be made larger and smaller, respectively, depending on where space is found.
        // The goal is to find space as close as possible to the area border, in all 4 directions.
        // They all start in the middle (X/2) and are then made larger and smaller.
        int horiMin = (int)sizeH / 2;
        int horiMax = (int)sizeH / 2;
        int vertMin = (int)sizeV / 2;
        int vertMax = (int)sizeV / 2;
        
        int digLRmax = 2;
        
        // Apart from digLRmax which prevents digging directly from a corner,
        // I found it neccessary to limit the hori/vert range where it is allowed to dig from.
        // This is because isolated space in cellular areas are often found near corners.
        // So with these 4 variables i limit the range to the middle half of all area sides.
        int horiLimMin = (int)sizeH / 4;
        int horiLimMax = sizeH - (int)sizeH / 4;
        int vertLimMin = (int)sizeV / 4;
        int vertLimMax = sizeV - (int)sizeV / 4;

        for (int i = 1; i < sizeH - 1; i++) {
            for (int j = 1; j < sizeV - 1; j++) {
                if (area.areaGrid[i, j] == space) {
                    if (j > digLRmax && j < sizeV - 1 - digLRmax && j > vertLimMin && j < vertLimMax) {
                        if (i < horiMin) {
                            horiMin = i;
                            area.cellAreaCorridorStartCoords[0] = (area.originHori + horiMin) + "," + (area.originVert + j);
                        }
                        if (i > horiMax) {
                            horiMax = i;
                            area.cellAreaCorridorStartCoords[1] = (area.originHori + horiMax) + "," + (area.originVert + j);
                        }
                    }
                    if (i > digLRmax && i < sizeH - 1 - digLRmax && i > horiLimMin && i < horiLimMax) {
                        if (j < vertMin) {
                            vertMin = j;
                            area.cellAreaCorridorStartCoords[2] = (area.originHori + i) + "," + (area.originVert + vertMin);
                        }
                        else if (j > vertMax) {
                            vertMax = j;
                            area.cellAreaCorridorStartCoords[3] = (area.originHori + i) + "," + (area.originVert + vertMax);
                        }
                    }
                }
            }
        }
        // ##### DEBUG ##### Debug.Log("sizeHV:" + area.width + "," + area.height + "<--->" + area.originHori + ", " + area.originVert + "<---horiMin:" + horiMin + ", horiMax:" + horiMax + ", vertMin:" + vertMin + ", vertMax:" + vertMax); // ##### DEBUG ##### 
    }



    int NbWallCount(char[,] cellGrid, int locH, int locV, int nbDist) {
        char wall = Symbol(Occupant.Wall);
        if (nbDist != 1 && nbDist != 2 && nbDist != 0)
            Debug.Log("Warning, function NbWallCount is getting input, nBDist = " + nbDist);
        int wallCount = 0;
        for (int i = locH - nbDist; i < locH + 1 + nbDist; i++) {
            for (int j = locV - nbDist; j < locV + 1 + nbDist; j++) {
                if (i >= 0 && i < cellGrid.GetLength(0) && j >= 0 && j < cellGrid.GetLength(1))
                    if (cellGrid[i, j] == wall) wallCount++;
            }
        }
        return wallCount;
    }

    void GenerateNoise(char[,] grid) {
        int wallChance = 45;
        char wall = Symbol(Occupant.Wall);
        char space = Symbol(Occupant.Used);
        int sizeH = grid.GetLength(0);
        int sizeV = grid.GetLength(1);
        int becomeWall;
        for (int i = 0; i < sizeH; i++) {
            for (int j = 0; j < sizeV; j++) {
                if (i == 0 || i == sizeH - 1 || j == 0 || j == sizeV - 1) grid[i, j] = wall;
                else {
                    becomeWall = RandomInt(0, 100);
                    if (becomeWall < wallChance) grid[i, j] = wall;
                    else grid[i, j] = space;
                }
            }
        }
    }


    bool CreateSpace(char[,] cellGrid, float spaceHsize, float spaceVsize) {
        char space = Symbol(Occupant.Used);
        int minSpace = 2;
        int sizeH = cellGrid.GetLength(0);
        int sizeV = cellGrid.GetLength(1);
        //int spaceH = (int)Math.Floor(0.8 * sizeH);
        //int spaceV = (int)Math.Floor(0.12 * sizeV);
        int spaceH = (int)Math.Floor(spaceHsize * sizeH - 2);
        int spaceV = (int)Math.Floor(spaceVsize * sizeV - 2);
        if (spaceH < minSpace && minSpace < sizeH - 2) spaceH = minSpace;
        if (spaceV < minSpace && minSpace < sizeV - 2) spaceV = minSpace;
        if (spaceH < minSpace || spaceV < minSpace) return false;

        int deltaSpaceHori = (int)Math.Floor(spaceH / 2.0);
        int deltaSpaceVert = (int)Math.Floor(spaceV / 2.0);
        int spaceLeft = (int)Math.Floor(sizeH / 2.0) - deltaSpaceHori;
        int spaceRight = (int)Math.Floor(sizeH / 2.0) + deltaSpaceHori;
        int spaceDown = (int)Math.Floor(sizeV / 2.0) - deltaSpaceVert;
        int spaceUp = (int)Math.Floor(sizeV / 2.0) + deltaSpaceVert;

        for (int i = spaceLeft; i < spaceRight; i++) {
            for (int j = spaceDown; j < spaceUp; j++) {
                cellGrid[i, j] = space;
            }
        }
        // ##### DEBUG ##### Debug.Log("spaceH:" + spaceH + ", spaceV:" + spaceV); // ##### DEBUG #####
        return true;
    }



}
