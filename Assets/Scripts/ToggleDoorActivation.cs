using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleDoorActivation : MonoBehaviour
{
    DialogueManager _dm;
    Door door;
    public bool hasActivated;


    private void Awake()
    {
        _dm = GameObject.Find("DialogueManager").GetComponent<DialogueManager>();
        door = GetComponent<Door>();
    }
    // Start is called before the first frame update
    void Start()
    {
        door.enabled = false;        
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasActivated && _dm.quest != null)
        {
            
            if(_dm.quest.isCompleted == true)
            {
                door.enabled = true;
                hasActivated = true;
            } 
            
        }
    }
}
