using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using UnityEditor;


public class SaveLoadTilemapAsJSON : MonoBehaviour {
    
    public event EventHandler OnLoaded;
    private Tilemap thisTileMap;
    private RuleTile ruleTile;
    private string customTilesFileName = "";
    private GameManager gameManager;

    public void Awake() {
        //  Ref. to GameManager
        //  -----------------------------------------------------------------------------
        GameObject GO = GameObject.Find("GameManager");
        if (GO) gameManager = GO.GetComponent<GameManager>();
        else Debug.Log(this.name + " : I didn't find GameManager.");
        //  -----------------------------------------------------------------------------

        //  Ref. to Tilemap
        //  -----------------------------------------------------------------------------
        GO = GameObject.Find("Tilemap");
        if (GO) thisTileMap = GO.GetComponent<Tilemap>();
        else Debug.Log(this.name + " : I didn't find Tilemap.");
        //  -----------------------------------------------------------------------------

        // Loading custom tiles file as per file-name specified in GameManager.
        if (gameManager.customTilesFileName != null && gameManager.customTilesFileName.Length > 0) {
            customTilesFileName = gameManager.customTilesFileName;
            LoadTilemap(thisTileMap, customTilesFileName);
        }

    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.K)) {
            SaveSystem.SetSaveFolder(SaveSystem.folder5);
            SaveSystem.SetLoadFolder(SaveSystem.folder5);
            SaveSystem.dateTime = DateTime.Now.ToString();
            if (gameManager) SaveSystem.identifier = gameManager.seedUsed.ToString();
            else SaveSystem.identifier = "Unknown";
            SaveTilemap(thisTileMap);
            Debug.Log(name + ": Tilemap saved !");
        }

        if (Input.GetKeyDown(KeyCode.L)) {
            SaveSystem.SetSaveFolder(SaveSystem.folder5);
            SaveSystem.SetLoadFolder(SaveSystem.folder5);
            LoadTilemap(thisTileMap, "");
            Debug.Log(name + ": Tilemap loaded !");
        }
    }


    public class SaveObject {
        public TilemapObject.SaveObject[] saveObject_Array;
    }


    public void SaveTilemap(Tilemap tilemap) {
        TileBase tileBase;
        string ruleTileName;
        List<TilemapObject.SaveObject> saveObject_List = new List<TilemapObject.SaveObject>();

        for (int y = tilemap.origin.y; y < (tilemap.origin.y + tilemap.size.y); y++) {
            for (int x = tilemap.origin.x; x < (tilemap.origin.x + tilemap.size.x); x++) {

                tileBase = tilemap.GetTile(new Vector3Int(x, y, 0));
                if (tileBase != null) {
                    ruleTile = (RuleTile)tileBase;
                    ruleTileName = ruleTile.name;

                    TilemapObject.SaveObject tilemapObject_SaveObject = new TilemapObject.SaveObject {
                        xPos = x,
                        yPos = y,
                        ruleTileName = ruleTileName,
                        collType = tilemap.GetColliderType(new Vector3Int(x, y, 0)).ToString(),
                    };
                    saveObject_List.Add(tilemapObject_SaveObject);
                }
            }
        }
        SaveObject saveObject = new SaveObject { saveObject_Array = saveObject_List.ToArray() };
        SaveSystem.SaveObject(saveObject);
    }


    public void LoadTilemap(Tilemap tilemap, string fileName) {
        SaveObject saveObject;
        if (fileName.Length == 0) saveObject = SaveSystem.LoadMostRecentObject<SaveObject>();
        else saveObject = SaveSystem.LoadObject<SaveObject>(fileName);

        foreach (TilemapObject.SaveObject itr in saveObject.saveObject_Array) {
            ruleTile = Resources.Load<RuleTile>("Ruletiles/" + itr.ruleTileName);
            tilemap.SetTile(new Vector3Int(itr.xPos, itr.yPos, 0), ruleTile);
        }
        OnLoaded?.Invoke(this, EventArgs.Empty);
    }


    public class TilemapObject {
        public int xPos;
        public int yPos;
        public string ruleTileName;
        public string collType;

        [System.Serializable]
        public class SaveObject {
            public int xPos;
            public int yPos;
            public string ruleTileName;
            public string collType;
        }
    }



}
