using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Text_Interaction : MonoBehaviour {

    public event EventHandler OnLoaded;
    public List<string> lines = new List<string>();
    public SaveObject saveObject;


    private void Awake() {
    }

    void Start() {
    }

    void Update() {
    }


    public class SaveObject {
        public CustomObject.SaveObject[] saveObject_Array;
    }


    public void LoadText_JSON(string fileName) {
        lines.Clear();
        saveObject = SaveSystem.LoadObject<SaveObject>(fileName);

        foreach (CustomObject.SaveObject itr in saveObject.saveObject_Array) {
            lines.Add(itr.text);
        }
        OnLoaded?.Invoke(this, EventArgs.Empty);
    }


    public class CustomObject {
        public string colour;
        public string text;

        [System.Serializable]
        public class SaveObject {
            public string colour;
            public string text;
        }

    }


}
