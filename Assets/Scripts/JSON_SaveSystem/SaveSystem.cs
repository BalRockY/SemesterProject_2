/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SaveSystem
{
    public const string folder1 = "/JSON_Temporary_Files/_Dialogs_All/";
    public const string folder2 = "/Resources/JSON_Files/_Dialogs_Completed/Resources/";
    public const string folder3 = "/Resources/JSON_Files/_AvailableFolder/";
    public const string folder4 = "/JSON_Temporary_Files/_ActionHandlers_All/";
    public const string folder4a = "/Resources/JSON_Files/_ActionHandlers_Completed/Resources/";
    public const string folder5 = "/JSON_Temporary_Files/_TilemapSaves_All/";
    public const string folder6 = "/Resources/JSON_Files/_TilemapSaves_Selected/Resources/";
    public static string identifier = "";
    public static string dateTime;

    private const string saveExtension = "txt";
    private static string Save_Folder;
    private static string Load_Folder;
    private static bool isInit = false;

    
    public static void SetSaveFolder(string folderName) {
        Save_Folder = Application.dataPath + folderName;
    }
    public static void SetLoadFolder(string folderName) {
        Load_Folder = Application.dataPath + folderName;
    }
    

    public static void Init()
    {
        if (!isInit && Application.isEditor)
        {
            isInit = true;
            // Test if Folders exists
            if (!Directory.Exists(Save_Folder)) {
                // Create Folder
                Directory.CreateDirectory(Save_Folder);
            }
            if (!Directory.Exists(Load_Folder)) {
                // Create Folder
                Directory.CreateDirectory(Load_Folder);
            }
        }
    }

    public static void Save(string fileName, string saveString, bool overwrite)
    {
        Init();
        string saveFileName = fileName;
        if (!overwrite)
        {
            // Make sure the Save Number is unique so it doesnt overwrite a previous save file
            int saveNumber = 1;
            while (File.Exists(Save_Folder + saveFileName + "." + saveExtension))
            {
                saveNumber++;
                saveFileName = fileName + "_" + saveNumber;
            }
            // saveFileName is unique
        }
        File.WriteAllText(Save_Folder + saveFileName + "." + saveExtension, saveString);
    }

    public static string Load(string fileName) {

        if (Application.isEditor) {
            Init();
            if (File.Exists(Load_Folder + fileName + "." + saveExtension)) {
                string saveString = File.ReadAllText(Load_Folder + fileName + "." + saveExtension);
                return saveString;
            }
            else {
                Debug.Log("Editor log entry: Text Asset " + fileName + " not found");
                return null;
            }
        }
        else {
            TextAsset myTextAsset = (TextAsset)Resources.Load(fileName, typeof(TextAsset));
            if (myTextAsset != null) {
                return myTextAsset.text;
            }
            else {
                Debug.Log("Non-Editor log entry: Text Asset " + fileName + " not found");
                return null;
            }
        }


        
        
    }

    public static string LoadMostRecentFile()
    {
        Init();
        DirectoryInfo directoryInfo = new DirectoryInfo(Load_Folder);
        // Get all save files
        FileInfo[] saveFiles = directoryInfo.GetFiles("*." + saveExtension);
        // Cycle through all save files and identify the most recent one
        FileInfo mostRecentFile = null;
        foreach (FileInfo fileInfo in saveFiles)
        {
            if (mostRecentFile == null)
            {
                mostRecentFile = fileInfo;
            }
            else
            {
                if (fileInfo.LastWriteTime > mostRecentFile.LastWriteTime)
                {
                    mostRecentFile = fileInfo;
                }
            }
        }

        // If theres a save file, load it, if not return null
        if (mostRecentFile != null)
        {
            string saveString = File.ReadAllText(mostRecentFile.FullName);
            return saveString;
        }
        else
        {
            return null;
        }
    }

    public static void SaveObject(object saveObject)
    {
        SaveObject("Identifier_" + identifier + "_save", saveObject, false);
    }

    public static void SaveObject(string fileName, object saveObject, bool overwrite)
    {
        Init();
        string json = JsonUtility.ToJson(saveObject);
        Save(fileName, json, overwrite);
    }

    public static TSaveObject LoadMostRecentObject<TSaveObject>()
    {
        Init();
        string saveString = LoadMostRecentFile();
        if (saveString != null)
        {
            TSaveObject saveObject = JsonUtility.FromJson<TSaveObject>(saveString);
            return saveObject;
        }
        else
        {
            return default(TSaveObject);
        }
    }

    public static TSaveObject LoadObject<TSaveObject>(string fileName)
    {
        Init();
        string saveString = Load(fileName);
        if (saveString != null)
        {
            TSaveObject saveObject = JsonUtility.FromJson<TSaveObject>(saveString);
            return saveObject;
        }
        else
        {
            return default(TSaveObject);
        }
    }

}
