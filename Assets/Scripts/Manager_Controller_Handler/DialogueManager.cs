using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    #region Fields    
    public Quest quest;

    Scene scene;

    // Reference to GameManager
    GameManager gameManager;
    UIManager ui;
    AudioManager aM;

    // Reference to player
    GameObject playerObject;
    GameObject npcObject; // For testing purposes
    // Reference to the Dialogue class, that allows the construction of a Dialogue object as well as its NextLine class
    public Dialogue dialogue;
    PlayerController playerController;
    RopeSystem ropeSystem;

    // Dialogue specifications
    public int dialogueStartPosition;
    private int nextDialogueLine;
    private int currentDialogueLine;
    public bool isInteracting = true;

    // List of Dialogues and Quests
    Dialogue[] dialogueList;
    Quest[] questList;

    // Last quest bool
    public bool rareCrystQuestComp;

    // Name placeholders
    private string player;  // reference to playername
    private string npc;     // reference to npc name

    // Default strings
    //private string defaultText = "Continue.";
    private string endConversationText = "End conversation.";

    public bool canGrapple;

    public Dialogue cluesComplete;

    #endregion

    #region MonoBehavior Methods
    void Awake()
    {
        scene = SceneManager.GetActiveScene();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        ui = GameObject.Find("UIManager").GetComponent<UIManager>();
        aM = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        if (aM)
        {
            //Debug.Log("Found" + aM.name);
        }
        //dialogueList = Resources.LoadAll<Dialogue>("ScriptableObjects/DialogueObjects");
        questList = Resources.LoadAll<Quest>("ScriptableObjects/QuestObjects");

        // Setting name variables for later use in strings used in dialogues
        
        ui.dialoguePanel.SetActive(false);

    }

    void Start()
    {
        /*
        foreach(Dialogue d in dialogueList)
        {
            //Debug.Log(d.name);
        }               
        */
    }

    void Update()
    {
        // Test
        if (Input.GetKeyDown(KeyCode.P))
        {

            ui.dialoguePanel.SetActive(true);
            StartDialogueNoActor();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            foreach (Quest q in questList)
            {
                Debug.Log("The quest: [" + q.questName + "] has been reset.");
                q.isActive = false;
                q.isCompleted = false;
            }
        }


        // Setting buttons/options true or false based on whether they have a string or not
        foreach (GameObject option in ui.dialogueOptions)
        {
            if (option.GetComponentInChildren<Text>().text != "")
            {
                option.SetActive(true);
            }
            else
            {
                option.SetActive(false);
            }
        }
        
        if (quest != null && quest.questTypes == questType.Collect && scene.name != "0_LoadingScene")
        {
            ui.questUI.SetActive(true);
            playerObject = GameObject.Find("Avatar_P(Clone)");

            var inventory = playerObject.GetComponent<PlayerA>().inventory;

            string questName = quest.questName;
            Item questItem = quest.item;
            int requiredItems = quest.itemAmountRequired;
            
            //int questItemsGathered = playerObject.GetComponent<PlayerA>().inventory.GroupBy(x => x.itemName).Count(x => x.Count() > 0);

            var questItemsGathered = inventory.Where(item => item.itemName == questItem.itemName).Count();


            // Debug.Log("Required item gathered: " + questItemsGathered + " and items required: " + quest.itemAmountRequired);
            
            // Checking if quest is Rare Crystal and completed, starting a dialogue in PCG
             
            // HERE WE CHECK FOR RARE CRYSTAL DIALOGUE
            // Checking if quest is completed
            if (inventory.Contains(questItem) && questItemsGathered >= quest.itemAmountRequired && quest.isCompleted == false)
            {                
                quest.isCompleted = true;
                if (quest.questName == "The Rare Crystal")
                {
                    rareCrystQuestComp = true;
                    quest = null;
                    dialogue = cluesComplete;
                    ui.dialoguePanel.SetActive(true);
                    
                    StartDialogueNoActor();
                }
            }    
            
            // Updating quest UI depending on progress/completion
            ui.questTitle.text = questName;
            ui.questObjective.text = quest.questShortDescription;

            if (!quest.isCompleted)
            {
                ui.questProgress.text = questItemsGathered + " / " + requiredItems + " " + questItem.itemName;
            }
            else if (quest.isCompleted)
            {
                ui.questProgress.fontStyle = FontStyle.Bold;
                ui.questProgress.text = questItemsGathered + " / " + requiredItems + " " + questItem.itemName + " (Quest Completed)";
            }

        }
        else if (quest == null)
        {
            ui.questUI.SetActive(false);
        }
        
    }
    #endregion

    #region Custom Methods
    public void StartDialogue(GameObject npcObject)
    {
        //Debug.Log("Started dialogue");
        playerObject = GameObject.Find("Avatar_P(Clone)");
        var inventory = playerObject.GetComponent<PlayerA>().inventory;
        List<Dialogue> dialogues = null;
        Item questItem = null;
        int requiredItems = -1;
        dialogue = null;

        if (npcObject.tag == "Oracle_P1")
        {
            Debug.Log("Interacting with the Oracle");
            if (npcObject.GetComponent<Oracle>().dialogues[0].quest.item != null)
            {
                questItem = npcObject.GetComponent<Oracle>().dialogues[0].quest.item;
                requiredItems = npcObject.GetComponent<Oracle>().dialogues[0].quest.itemAmountRequired;
            }
            
            dialogues = npcObject.GetComponent<Oracle>().dialogues;
            dialogue = dialogues[0];
        }
        else if (npcObject.tag == "Mentor_P1")
        {
            
            questItem = npcObject.GetComponent<Mentor>().dialogues[0].quest.item;
            requiredItems = npcObject.GetComponent<Mentor>().dialogues[0].quest.itemAmountRequired;
            dialogues = npcObject.GetComponent<Mentor>().dialogues;
            dialogue = dialogues[0];
            
        }
        
        
        // Setting the first dialogue to a specific dialogue start position (dsp), by default the 0th index of the dialogue array

        player = dialogue.playerName;                           // @player
        npc = dialogue.npcName;                                 // @npc        
        
        if (dialogues.Count == 0)
        {
            return;
        }

        if (dialogue.quest != null && dialogue.quest.isCompleted == true)
        {
            quest = null;
            dialogues.Remove(dialogues[0]);
            dialogue = dialogues[0];
            for(int i = 0; i < requiredItems; i++)
                {
                    inventory.Remove(questItem);
                }
        }



        int dsp = dialogueStartPosition;
        currentDialogueLine = dialogue.dialogueObject[dsp].currentDialogueLine;
        nextDialogueLine = dialogue.dialogueObject[dsp].nextLine[dsp].nextDialogueLine;
        ui.dialogueOptions[0].GetComponentInChildren<Text>().text = "1. " + dialogue.dialogueObject[currentDialogueLine].nextLine[dsp].playerResponses;

        // Formatting and inserting dialogue lines
        if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.Narrator)
        {
            ui.characterName.text = "";
            ui.dialogueText.fontStyle = FontStyle.Italic;
            ui.dialogueText.color = ui.narratorFontColor;

            ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
            .Replace("@player", player)
            .Replace("@npc", npc);
        }
        else if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.Player)
        {
            ui.characterName.text = "Max";
            ui.dialogueText.fontStyle = FontStyle.Normal;
            ui.dialogueText.color = ui.playerFontColor;
            ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
            .Replace("@player", player)
            .Replace("@npc", npc);
        }
        else if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.NPC)
        {
            ui.characterName.text = "Jasqar";
            ui.dialogueText.fontStyle = FontStyle.Normal;
            ui.dialogueText.color = ui.npcFontColor;
            ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
            .Replace("@player", player)
            .Replace("@npc", npc);
        }
    }
    public void StartDialogueNoActor()
    {
        Debug.Log("Starting dialogue");

        // Setting the first dialogue to a specific dialogue start position (dsp), by default the 0th index of the dialogue array

        Item questItem = dialogue.quest.item;
        int requiredItems = dialogue.quest.itemAmountRequired;
        var inventory = playerObject.GetComponent<PlayerA>().inventory;

        if (dialogue.quest != null && dialogue.quest.isCompleted == true)
        {
            quest = null;            
            dialogue = null;
            for (int i = 0; i < requiredItems; i++)
            {
                inventory.Remove(questItem);
            }
        }

        int dsp = dialogueStartPosition;
        currentDialogueLine = dialogue.dialogueObject[dsp].currentDialogueLine;
        nextDialogueLine = dialogue.dialogueObject[dsp].nextLine[dsp].nextDialogueLine;
        ui.dialogueOptions[0].GetComponentInChildren<Text>().text = "1. " + dialogue.dialogueObject[currentDialogueLine].nextLine[dsp].playerResponses;

        // Formatting and inserting dialogue lines
        if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.Narrator)
        {
            ui.characterName.text = "";
            ui.dialogueText.fontStyle = FontStyle.Italic;
            ui.dialogueText.color = ui.narratorFontColor;

            ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
            .Replace("@player", player)
            .Replace("@npc", npc);
        }
        else if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.Player)
        {
            ui.characterName.text = "Max";
            ui.dialogueText.fontStyle = FontStyle.Normal;
            ui.dialogueText.color = ui.playerFontColor;
            ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
            .Replace("@player", player)
            .Replace("@npc", npc);
        }
        else if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.NPC)
        {
            ui.characterName.text = "Jasqar";
            ui.dialogueText.fontStyle = FontStyle.Normal;
            ui.dialogueText.color = ui.npcFontColor;
            ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
            .Replace("@player", player)
            .Replace("@npc", npc);
        }
    }
    public void DisplayNextSentence(int lineOption)
    {        
        // Clearing the lines after each button press, in order not to carry previous dialogue options to the next
        ClearLines();

        

        // Keeping track of the next dialogue depending on the current dialogue and the specific button pressed using lineOption
        nextDialogueLine = dialogue.dialogueObject[currentDialogueLine].nextLine[lineOption].nextDialogueLine;
        currentDialogueLine = nextDialogueLine;

        // Keeping track of the number of possible answers/options for each dialogue segment, in order to prevent "out of bounds" errors.
        int playerAnswers = dialogue.dialogueObject[currentDialogueLine].nextLine.Length;

        for (int i = 0; i < playerAnswers; i++)
        {
            ui.dialogueOptions[i].GetComponentInChildren<Text>().text = dialogue.dialogueObject[currentDialogueLine].nextLine[i].playerResponses;
            if (dialogue.dialogueObject[currentDialogueLine].nextLine[i].playerResponses != "")
            {
                ui.dialogueOptions[i].GetComponentInChildren<Text>().text = (i + 1) + ". " + dialogue.dialogueObject[currentDialogueLine].nextLine[i].playerResponses;
            }
        }


        if (dialogue.dialogueObject.Length > nextDialogueLine)
        {
            

            // Formatting and inserting dialogue lines
            if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.Narrator)
            {
                ui.characterName.text = "";
                ui.dialogueText.fontStyle = FontStyle.Italic;
                ui.dialogueText.color = ui.narratorFontColor;
                ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
                .Replace("@player", player)
                .Replace("@npc", npc);
            }
            else if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.Player)
            {
                ui.characterName.text = player;
                ui.dialogueText.fontStyle = FontStyle.Normal;
                ui.dialogueText.color = ui.playerFontColor;
                ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
                .Replace("@player", player)
                .Replace("@npc", npc);
            }
            else if (dialogue.dialogueObject[currentDialogueLine]._dialoguetype == dialogueType.NPC)
            {
                AudioClip audioClip = dialogue.dialogueObject[currentDialogueLine].audio;
                aM.source.clip = audioClip;
                aM.source.Play();
                ui.characterName.text = npc;
                ui.dialogueText.fontStyle = FontStyle.Normal;
                ui.dialogueText.color = ui.npcFontColor;
                ui.dialogueText.text = dialogue.dialogueObject[currentDialogueLine].dialogue
                .Replace("@player", player)
                .Replace("@npc", npc);
            }

            // Checking for Quest Triggers. See the enumerator ont he Dialogue scripted object.
            //  If a dialogue segment has Quest as it's trigger event, it will run the quest associated with the dialogue
            if (dialogue.dialogueObject[currentDialogueLine]._triggerEvent == triggerEvent.Quest)
            {
                quest = dialogue.quest;
                string questName = quest.questName;
                if (dialogue.quest.isActive == false && dialogue.quest.isCompleted == false)
                {
                    quest = dialogue.quest;
                    dialogue.quest.isActive = true;                    

                    // Debug.Log("Recieved quest: " + questName);
                }
                bool hasTriggeredOnce = false;
                if (quest.name == "OracleQuest" && hasTriggeredOnce == false)
                {
                    hasTriggeredOnce = true;
                    Debug.Log("Got Oracle Quest");
                    
                    GameObject.Find("Oracle").SetActive(false);
                    GameObject.Find("Oracle_Head").SetActive(false);
                    var anim = GameObject.Find("Anim_Trigger");
                    anim.SetActive(false);
                    anim.SetActive(true);
                    
                }
            }

            // I wouldve made more triggerEvent types but this late in the process I just used an item to end the conversation.
            if (dialogue.dialogueObject[currentDialogueLine]._triggerEvent == triggerEvent.Item && quest.name == "OracleQuest")
            {
                ui.dialoguePanel.SetActive(false);
                playerObject.GetComponent<PlayerController>().enabled = true;
                playerObject.GetComponent<RopeSystem>().enabled = true;
            }
        }
        // Checking if the conversation has met its end, and if so it stops the conversation.
        if (nextDialogueLine == dialogue.dialogueObject.Length - 1)
        {


            playerObject = GameObject.Find("Avatar_P(Clone)");
            playerController = playerObject.GetComponent<PlayerController>();
            ropeSystem = playerObject.GetComponent<RopeSystem>();
            ui.dialogueOptions[0].GetComponentInChildren<Text>().text = endConversationText;
            isInteracting = false;
            ui.dialoguePanel.SetActive(false);
            playerController.enabled = true;
            if (canGrapple)
            {
                ropeSystem.crosshairSprite.enabled = true;
                ropeSystem.enabled = true;
            }
            else if (!canGrapple)
            {
                ropeSystem.crosshairSprite.enabled = false;
                ropeSystem.enabled = false;
            }

        }
        else
        {
            return;
        }

    }
    // Tiny method to clearing lines after each dialogue segment
    public void ClearLines()
    {
        ui.dialogueOptions[0].GetComponentInChildren<Text>().text = "";
        ui.dialogueOptions[1].GetComponentInChildren<Text>().text = "";
        ui.dialogueOptions[2].GetComponentInChildren<Text>().text = "";
    }
    #endregion
}
