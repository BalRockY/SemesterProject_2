using UnityEngine;

/*  This scriptable object carries an ID, references to player and NPC names, associated quests and a specific Query class.
 *  The Query class holds all the dialogue, responses and associated variables
 *  In order to create a scriptable Dialogue object, right click in the assets folder and click Dialogue Menu.
 */ 

[CreateAssetMenu(fileName = "Dialogue File", menuName = "New Dialogue")]
public class Dialogue : ScriptableObject
{
    public int id;
    public string npcName;
    public string playerName;   // generalize this later   
    public Query[] dialogueObject;    
    public Quest quest;
    public Item item;
    //public GameObject questGiver;
}
public enum dialogueType
{
    Player,
    NPC,
    Narrator
}
public enum triggerEvent
{
    None,
    Quest,
    Item
}
[System.Serializable]
public class Query
{
    [SerializeField]
    public int currentDialogueLine;    

    [SerializeField]
    [TextArea(4, 40)]
    public string dialogue;

    [SerializeField]
    public NextLine[] nextLine;

    public dialogueType _dialoguetype;
    public triggerEvent _triggerEvent;
    public AudioClip audio;
    public Query(NextLine[] nextLine, string dialogue, dialogueType _dialogueType, triggerEvent _triggerEvent, AudioClip audio)
    {        
        this.dialogue = dialogue;
        this.nextLine = nextLine;
        this._dialoguetype = _dialogueType;
        this._triggerEvent = _triggerEvent;
        this.audio = audio;
    }
}
[System.Serializable]
public class NextLine
{
    [SerializeField]
    public string playerResponses;
    [SerializeField]
    public int nextDialogueLine;

}
