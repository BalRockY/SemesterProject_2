using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


class PlayerA : BaseBehavior {
    DialogueManager _dm;
    UIManager _ui;

    GameObject crossHair;

    //For the damage blink
    public Renderer[] rend;
    public Color dmgColor = Color.red;
    public Color normalColor = Color.white;
    public int blinkCoolDown = 0;

    public int playerNumber;                      //  Player number for the player to see.
    public Camera playerCamera;                   //  Variable to hold the Camera instance.
    public Canvas playerCanvas;                   //  Variable to hold the Canvas instance.

    //  Purchase variables.
    [HideInInspector] public int cost;
    public float gold;
    public int[] purchase = new int[4];
    public int[] noUnits = new int[4];

    // Sounds
    public AudioClip[] hurtSounds;
    public AudioClip[] jumpSounds;
    public int trackPlaying;
    AudioSource audioSource;

    //  Movement variables.
    public bool isMoving = true;
    private bool doorSpawned = false;


    [HideInInspector] public PlayerController playerController;

    protected override void Awake() {
        base.Awake();
        playerNumber = myID + 1;
        SetStats();

        _dm = GameObject.Find("DialogueManager").GetComponent<DialogueManager>();
        _ui = GameObject.Find("UIManager").GetComponent<UIManager>();
        // Sound and jumping
        
        playerController = GetComponent<PlayerController>(); // Finding my playerController
        playerController.I_Jumped += PlayJumpSound; // Subscribing to event.
        audioSource = GetComponent<AudioSource>();
        crossHair = transform.GetChild(2).gameObject;
        crossHair.SetActive(false);

    }


    protected override void Start() {
        base.Start();
    }


    public void Update() {
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
        if ( !doorSpawned && _dm.rareCrystQuestComp &&
              (
                 (gameManager.currentSceneName == ButtonActions.PCG_Scene1_Name) ||
                 (gameManager.currentSceneName == ButtonActions.PCG_Scene2_Name)
              )
           )
        {
            GameObject door = gameManager.actorRefByBaseTag["PCG-DoorAction-3_P"].gameObject;
            List<string> areaFloorLocList = new List<string>();
            Vector3 playerPos = this.transform.position;
            int xPos = (int)this.transform.position.x;
            int yPos = (int)this.transform.position.y;
            int areaId = -1;
            if (gameManager.coordToAreaID.ContainsKey(xPos + "," + yPos)) {
                areaId = gameManager.coordToAreaID[xPos + "," + yPos];
                doorSpawned = true;
            }

            // We found an area with floorLocations, now we pick the spot.
            if (areaId >= 0) {
                // _dm.rareCrystQuestComp = true;

                // Removing old door from cam view.
                GameObject AH_Actor = gameManager.actorRefByBaseTag["PCG-DoorAction-1_P"];
                AH_Actor.SetActive(false);
                gameManager.camControl.FullViewSwitch();

                SpriteRenderer _rend = door.GetComponent<SpriteRenderer>();
                _rend.color = new Color(1, 0, 0, 1f);

                for (int i = gameManager.areas[areaId].originHori + 1; i < gameManager.areas[areaId].originHori + gameManager.areas[areaId].width - 1; i++)
                {
                    for (int j = gameManager.areas[areaId].originVert + 1; j < gameManager.areas[areaId].originVert + gameManager.areas[areaId].height - 1; j++)
                    {
                        if (gameManager.floorSpawnLocs.ContainsKey(i + "," + j)) areaFloorLocList.Add(i + "," + j);
                    }
                }
                int rndLocIndex = Random.Range(0, areaFloorLocList.Count);
                int[] loc = areaFloorLocList[rndLocIndex].Split(',').Select(int.Parse).ToArray();

                gameManager.RelocateAndActivate_ActorInScene(door, loc[0], loc[1]);
            }
        }

    }


    //  Assigning base stats and HP.
    public void SetStats() {
        aggro = 0f;
        myEntityType = EntityType.Avatar;
        myEntitySubType = EntitySubType.AvatarTypeA;
        myDefaultMode = Mode.Idle;
        myMode = myDefaultMode;
        baseHP = 1f;
        lives = startLives;
        HP = baseHP;
        baseSpeed = 10f;
        speed = baseSpeed;
        baseDefence = 75f;
        defence = baseDefence;
        baseDamage = 50f;
        damage = baseDamage;
        attackSpeed = 1f;
        attackRange = 15f;
        existing = true;
    }


    public void PlayJumpSound() {
        trackPlaying = Random.Range(0, jumpSounds.Length);
        audioSource.clip = jumpSounds[trackPlaying];
        audioSource.pitch = Random.Range(0.85f, 1.1f);
        audioSource.volume = 0.05f;
        audioSource.Play();
    }

    protected override void OnTriggerEnter2D(Collider2D other) {
        base.OnTriggerEnter2D(other);
        if (other.gameObject.tag == "Item")   
        {
            string text = "You have recieved: " + other.GetComponentInChildren<ItemHandler>().item.itemName;
            _ui.UpdateInfoText(text, 3);
            
            //Debug.Log("You have recieved: " + other.GetComponentInChildren<ItemHandler>().item.itemName);
            inventory.Add(other.GetComponentInChildren<ItemHandler>().item);
            Destroy(other.gameObject);
        }
        if(other.gameObject.tag == "Trigger")
        {
            //Debug.Log("Quest trigger name is: " + _dm.quest.trigger.name + " and collider trigger name is: " +other.name);
            if(_dm.quest != null && _dm.quest.questTypes == questType.Information &&_dm.quest.trigger.name == other.name)
            {
                _dm.quest.isCompleted = true;
            }            
        }
        if(other.gameObject.name == "TriggerDoubleJump")
        {
            playerController.canDoubleJump = true;
            string text = "To double jump, press space twice";
            _ui.UpdateInfoText(text, 10);
        }
        if (other.gameObject.name == "TriggerWallClimb")
        {
            playerController.canWallGrab = true;
            string text = "To wall climb, hold shift and press W (up)";
            _ui.UpdateInfoText(text, 10);
        }
        if (other.gameObject.name == "TriggerWallJump")
        {
            playerController.canWallJump = true;
            string text = "To wall jump, press space while near or climbing a wall";
            _ui.UpdateInfoText(text, 10);
        }
        if (other.gameObject.name == "TriggerGrapplingHook")
        {
            playerController.ropeSystem.enabled = true;
            crossHair.SetActive(true);
            playerController.canGrapple = true;
            string text = "To use the grappling hook, press and hold left mouse button. To adjust height, press W or S to hoist up or down, respectively";
            _ui.UpdateInfoText(text, 20);
        }
        if (other.gameObject.name == "TriggerGrapplingHookTwice")
        {            
            string text = "To traverse longer distances with the grappling hook: Swing and grab multiple times while flying through the air";
            _ui.UpdateInfoText(text, 20);
        }
        

    }


    //  Player move
    /*
    protected override void Move(GameObject gameObject, float signedRatio, float maxSpeed) {
        Rigidbody2D playerBody = this.gameObject.GetComponent<Rigidbody2D>();
        bool isJumping;
        float verticalSpeed = playerBody.velocity.y;

        if (verticalSpeed > 0f) isJumping = true;
        else isJumping = false;

        //  Letting the player move and jump, if he/she is allowed.
        if (isMoving) {
            base.Move(this.gameObject, moveAxisInputValue, speed);
            if (isJumping == false) {
                if (interactAxisInputValue == 1f)  {
                    playerBody.AddForce(new Vector2(0, 10), ForceMode2D.Impulse);
                }
            }
        }
    }
    */



}
