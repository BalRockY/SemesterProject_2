using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

abstract class BaseBehavior : BaseIdentity {
    //  Reference to GameManager.
    public GameManager gameManager;
    DialogueManager _dm;
    UIManager _ui;
    

    // Random for player events.


    //  Movement variables
    //protected string moveAxisName;                //  Left/Right movement (seen from the screen).
    //public float moveAxisInputValue;            //  Movement value ranging [-1,1].
    //protected string interactAxisName;            //  In reserve for future interactions (with buildings etc.)
    //public float interactAxisInputValue;

    //  Combat & Move
    public float attackTimer;
    public float PCG_NPC_NewDirLimit = 0;
    Vector2 moveVector = default;

    // Collision
    public List<Collider2D> myOpponents = new List<Collider2D>();
    public List<Collider2D> interactNowList = new List<Collider2D>();
    private List<Collider2D> removeList = new List<Collider2D>();

    private ButtonActions buttonActions;

    protected override void Awake() {
        base.Awake();
    }

    protected virtual void Start() {
        //  Ref. to GameManager
        //  -----------------------------------------------------------------------------
        GameObject _instance = GameObject.Find("GameManager");
        if (_instance) {
            gameManager = _instance.GetComponent<GameManager>();
        }
        else Debug.Log(this.name + " : I didn't find GameManager.");
        //  -----------------------------------------------------------------------------

        //  -----------------------------------------------------------------------------
        string lookFor = "DialogueManager";
        _instance = GameObject.Find(lookFor);

        if (_instance) {
            _dm = _instance.GetComponent<DialogueManager>();
        }
        else Debug.Log(this.name + " : I didn't find " + lookFor);
        //  -----------------------------------------------------------------------------

        //  -----------------------------------------------------------------------------
        lookFor = "UIManager";
        _instance = GameObject.Find(lookFor);

        if (_instance) {
            _ui = _instance.GetComponent<UIManager>();
        }
        else Debug.Log(this.name + " : I didn't find " + lookFor);
        //  -----------------------------------------------------------------------------

        buttonActions = GameObject.Find("ButtonScripts").GetComponent<ButtonActions>();
    }

    //  Controlling actor mode.
    protected virtual void FixedUpdate() {
        attackTimer += Time.deltaTime;

        if (myEntityType == EntityType.NPC) {
            switch (myMode) {
                case Mode.Auto:
                    TestForInteract();
                    if (!tag.Contains("Guard")) {
                        Move(this.gameObject, 1, speed);
                    }
                    break;
                case Mode.GetInRange:
                    TestForInteract();
                    //  The NPC is only allowed to move if there is none to attack right now.
                    if (interactNowList.Count == 0) Move(this.gameObject, 1, speed);
                    break;
                case Mode.Combat:
                    TestForInteract();
                    break;
            }
        }
        if (myEntityType == EntityType.Avatar) {
            switch (myMode) {
                case Mode.Idle:
                    TestForInteract();
                    GetComponent<PlayerController>().Avatar_GrappleOn();
                    break;
                case Mode.Move:
                    TestForInteract();
                    GetComponent<PlayerController>().Avatar_GrappleOn();
                    break;
                case Mode.Conversation:
                    TestForInteract();
                    GetComponent<PlayerController>().Avatar_GrappleOff();
                    break;
                case Mode.GetInRange:
                    TestForInteract();
                    GetComponent<PlayerController>().Avatar_GrappleOn();
                    break;
                case Mode.Combat:
                    TestForInteract();
                    GetComponent<PlayerController>().Avatar_GrappleOn();
                    break;
            }

        }
        if (myEntityType == EntityType.NonMovingEntity) {
            switch (myMode) {
                case Mode.Auto:
                    TestForInteract();
                    break;
                case Mode.Idle:
                    TestForInteract();
                    break;
                case Mode.Conversation:
                    TestForInteract();
                    break;
            }

        }


    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && myMode == Mode.Conversation && dialogues.Count > 0)
        {
            Debug.Log("Conversation time!");
            Conversation();
        }

    }





    // Movement function.
    protected virtual void Move(GameObject gameObject, float signedRatio, float maxSpeed) {
        bool moving = true;
        if (attackTimer > attackSpeed + PCG_NPC_NewDirLimit) {
            float hDir = Random.Range(-1f, 1f);
            float vDir = Random.Range(-1f, 1f);
            if (myEntitySubType == EntitySubType.UnivWalkingPCG_NPC) vDir = 0;
            moveVector = new Vector2(hDir, vDir);
            if (hDir < 0) {
                gameObject.transform.localRotation = Quaternion.RotateTowards(gameObject.transform.rotation, Quaternion.Euler(0,0,0), 180);
            }
            else if (hDir > 0) {
                gameObject.transform.localRotation = Quaternion.RotateTowards(gameObject.transform.rotation, Quaternion.Euler(0, 180, 0), 180);
            }
            attackTimer = attackSpeed;
            PCG_NPC_NewDirLimit = Random.Range(attackSpeed + 1f, 5f);
        }
        // Moving the actor.
        if (signedRatio != 0 && moving) {
            gameObject.transform.Translate(0.5f * moveVector * maxSpeed * Time.deltaTime, Space.World);
        }
    }
    


    protected void ChangeAggro(float aggroValue) {
        BoxCollider2D myBoxCollider = this.gameObject.GetComponent<BoxCollider2D>();
        aggro = aggroValue;
    }


    //  Just checking if players or NPCs are allowed to attack.
    protected void TestForInteract() {
        if (myEntityType == BaseIdentity.EntityType.Avatar)
            if (attackTimer >= attackSpeed) Interact();
        if (myEntityType == BaseIdentity.EntityType.NPC) {
            if (attackTimer >= (attackSpeed + Random.Range(0f, 100f)) ) {
                Interact();
            }
        }
        if (myEntityType == BaseIdentity.EntityType.NonMovingEntity)
            if (attackTimer >= attackSpeed) Interact();
    }

    //  The purpose of this method is to keep score of what opponents I have.
    //  And which of those are in range.
    protected void Interact() {
        List<Collider2D> removeList = new List<Collider2D>();
        float opponentDist;
        //  Checking in any opponents in my list have been killed.
        foreach (Collider2D itr in myOpponents) {
            if (!itr.gameObject.activeInHierarchy) removeList.Add(itr);
        }
        //  If they are, they are removed from both lists.
        foreach (Collider2D itr in removeList) {
            myOpponents.Remove(itr);
            interactNowList.Remove(itr);
        }
        removeList.Clear();
        //  Checking the distances to remaining opponents.
        //  Adding the ones within attack range to a list.
        foreach (Collider2D itr in myOpponents)
        {
            Vector2 myPos = new Vector2(this.gameObject.transform.position.x, this.gameObject.transform.position.y);
            Vector2 otherPos = new Vector2(itr.gameObject.transform.position.x, itr.gameObject.transform.position.y);
            opponentDist = Vector2.Distance(myPos, otherPos);
            if (attackRange >= opponentDist && !interactNowList.Contains(itr)) interactNowList.Add(itr);
            else if (attackRange < opponentDist && interactNowList.Contains(itr)) {
                removeList.Add(itr);
            }
        }
        //  Removing opponents which are no longer in range.
        foreach (Collider2D itr in removeList) {
            myOpponents.Remove(itr);
            interactNowList.Remove(itr);
        }
        //  If I have a viable opponent I interact / attack.
        if (interactNowList.Count > 0) {
            switch (myEntityType) {
                case EntityType.Avatar:
                    if (!gameManager.actionHandler_Panel.activeInHierarchy) {
                        if (Input.GetButtonDown("Fire3")) {
                            myMode = Mode.Combat;
                            Attack(interactNowList.First());
                        }
                    }
                    break;
                case EntityType.NPC:
                    if (have_AH &&
                        aggro < aggroLimit &&
                        myMode != Mode.Conversation &&
                        !gameManager.actionHandler_Panel.activeInHierarchy &&
                        interactNowList.First().GetComponent<BaseIdentity>().myEntityType == EntityType.Avatar
                        )
                    {
                        myMode = Mode.Conversation;
                        Conversation();
                    }
                    else if (aggro < aggroLimit) {
                        myMode = myDefaultMode;
                    }
                    else if (dialogues.Count > 0 &&
                        aggro < aggroLimit &&
                        myMode != Mode.Conversation &&
                        !gameManager.actionHandler_Panel.activeInHierarchy &&
                        interactNowList.First().GetComponent<BaseIdentity>().myEntityType == EntityType.Avatar
                        )
                    {
                        myMode = Mode.Conversation;
                    }
                    else {
                        myMode = Mode.Combat;
                        Attack(interactNowList.First());
                    }
                    break;
                case EntityType.NonMovingEntity:
                    if (have_AH &&
                        aggro < aggroLimit &&
                        myMode != Mode.Conversation &&
                        !gameManager.actionHandler_Panel.activeInHierarchy &&
                        interactNowList.First().GetComponent<BaseIdentity>().myEntityType == EntityType.Avatar
                       )
                    {
                        myMode = Mode.Conversation;
                        Conversation();
                    }
                    else if (dialogues.Count > 0 &&
                        aggro < aggroLimit &&
                        myMode != Mode.Conversation &&
                        !gameManager.actionHandler_Panel.activeInHierarchy &&
                        interactNowList.First().GetComponent<BaseIdentity>().myEntityType == EntityType.Avatar
                        )
                    {
                        myMode = Mode.Conversation;
                    }
                    /*
                    else  {
                        myMode = myDefaultMode;
                    }
                    */
                    break;
            }
        }
        else if (myOpponents.Count > 0) {
            if (myEntityType == EntityType.NPC) myMode = Mode.GetInRange;
            else if (myEntityType == EntityType.Avatar) myMode = Mode.GetInRange;
        }
        //  Finally if none of the colliders which triggered my collider are alive,
        //  or has exited my collider, myMode is set to myDefaultMode.
        else if (myOpponents.Count == 0 && myMode != myDefaultMode) {
            if (myEntityType != EntityType.Avatar) myMode = myDefaultMode;
            else if (!gameManager.actionHandler_Panel.activeInHierarchy) {
                myMode = myDefaultMode;
            }
            //Debug.Log(name + ", Setting myDefaultMode->" + myDefaultMode + "<- in TestForInteract");
            interactNowList.Clear();
        }
        else if (myOpponents.Count == 0 && myEntitySubType == EntitySubType.Door) {
            Animator myAnimator = this.GetComponent<Animator>();
            myAnimator.SetBool("DoorTrigger", false);
        }
    }

    protected void Conversation() {
        if (interactNowList.First().GetComponent<BaseIdentity>().myEntitySubType == EntitySubType.AvatarTypeA) {
            interactNowList.First().GetComponent<BaseIdentity>().myMode = Mode.Conversation;
        }
        if (dialogues.Count > 0) {


            if (interactNowList.First().GetComponent<RopeSystem>().enabled)
            {
                _dm.canGrapple = true;
            }
            else if (!interactNowList.First().GetComponent<RopeSystem>().enabled)
            {
                _dm.canGrapple = false;
            }

            Debug.Log("Interacting with " + dialogues[0].npcName);

                interactNowList.First().GetComponent<PlayerController>().enabled = false;
                interactNowList.First().GetComponent<RopeSystem>().crosshairSprite.enabled = false;
                interactNowList.First().GetComponent<RopeSystem>().enabled = false;

                _ui.dialoguePanel.SetActive(true);
                _dm.StartDialogue(this.gameObject);
            
        }
        else {
            // Activating the first ActionHandler in the Entities list.
            buttonActions.activeActionHandler = AH_NameList[0];
            // Debug.Log("Active actionhandler->" + buttonActions.activeActionHandler);
            ActionHandler myActionHandler = GameObject.Find(AH_NameList[0]).GetComponent<ActionHandler>();
            myActionHandler.activate = true;
            if (myEntitySubType == EntitySubType.Door)
            {
                Animator myAnimator = this.GetComponent<Animator>();
                myAnimator.SetBool("DoorTrigger", true);
            }
        }

    }

    protected virtual void Attack(Collider2D other) {
        //  Notifying gameManager of attacks.
        //  This is used to update music state.
        BaseIdentity.EntityType entityType = interactNowList.First().GetComponent<BaseIdentity>().myEntityType;
        if (entityType == EntityType.Avatar || entityType == EntityType.NPC) gameManager.globalAttackTimer = 0;
        //  Doing damage
        float oppDef = interactNowList.First().GetComponent<BaseIdentity>().defence;
        float myDamage = damage * ((100f - oppDef) / 100f);
        interactNowList.First().GetComponent<BaseIdentity>().HP -= myDamage;

        // Player take-damage animation. ---------------------------------
        /*
        if (interactNowList.First().tag == "Avatar_P") {
            Animation animation = interactNowList.First().GetComponent<Animation>();
        }
        */
        // Player take-damage animation. ---------------------------------

        //  Death
        if (interactNowList.First().GetComponent<BaseIdentity>().HP <= 0) {
            interactNowList.First().GetComponent<BaseIdentity>().HP = 0;
            Die(this.gameObject, interactNowList.First());
            if (interactNowList.Count > 0) {
                myOpponents.Remove(interactNowList.First());
                interactNowList.Remove(interactNowList.First());
            }
        }
        attackTimer = 0;
    }


    //  Handling death.
    protected virtual void Die(GameObject attacker, Collider2D other) {
        //  Player
        if (other.GetComponent<BaseIdentity>().myEntitySubType == EntitySubType.AvatarTypeA) {
            other.GetComponent<BaseIdentity>().lives -= 1;
            // Sound
            //StartCoroutine(DyingFeedback(other));
            // Resetting
            if (other.GetComponent<BaseIdentity>().lives <= 0) {
                other.GetComponent<BaseIdentity>().myMode = myDefaultMode;
                other.GetComponent<BaseBehavior>().myOpponents.Clear();
                other.GetComponent<BaseBehavior>().removeList.Clear();
                other.GetComponent<BaseIdentity>().existing = false;
                other.gameObject.SetActive(false);
                gameManager.AvatarBackToSceneSpawn();
            }
            else other.GetComponent<BaseIdentity>().HP = other.GetComponent<BaseIdentity>().baseHP;
        }
        /*
        else if (other.GetComponent<BaseIdentity>().myEntitySubType == EntitySubType.WallTower)
        {
            //  Sound
            StartCoroutine(DyingFeedback(other));
            //  Shake
            StartCoroutine(ShakeHandler(null, other));
            other.GetComponents<Collider2D>()[0].enabled = false;
            other.GetComponents<Collider2D>()[1].enabled = false;
            // other.gameObject.SetActive(false);
        }
        else
        {
            other.gameObject.SetActive(false);
            other.GetComponent<BaseIdentity>().existing = false;
        }
        */
    }


    //  Opponents removed if they exit trigger collider.
    protected void OnTriggerExit2D(Collider2D other) {
        myOpponents.Remove(other);
        interactNowList.Remove(other);
        if (myOpponents.Count == 0) {
            // Debug.Log("My name in trig exit:" + name + ", other name:" + other.name);
            if (have_AH && other.isTrigger && other.tag == "Avatar_P0") gameManager.actionHandler_Panel.SetActive(false);
            myMode = myDefaultMode;

            //Debug.Log(name + ", Setting myDefaultMode->" + myDefaultMode + "<- in TriggerExit2D");
        }
    }


    //  Opponents are registered here.
    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (other.isTrigger && !myOpponents.Contains(other) &&
              (myEntityType == BaseIdentity.EntityType.Avatar ||
               myEntityType == BaseIdentity.EntityType.NPC ||
               myEntityType == BaseIdentity.EntityType.NonMovingEntity
              ) &&
              other.tag.Contains("_P") && !(other.GetComponent<BaseIdentity>().myEntitySubType == BaseIdentity.EntitySubType.Wall)
           )
        {
            if ((other.GetComponent<BaseIdentity>().myLoyalty != myLoyalty) && !myOpponents.Contains(other))
                myOpponents.Add(other);
            if (myOpponents.Count > 0)
            {
                // if (!(myEntityType == BaseIdentity.EntityType.NonMovingEntity))
                if (myEntityType == BaseIdentity.EntityType.NPC) myMode = Mode.GetInRange;
                if (myEntityType == BaseIdentity.EntityType.Avatar) myMode = Mode.GetInRange;
            }
            else {
                myMode = myDefaultMode;

                // Debug.Log(name + ", Setting myDefaultMode->" + myDefaultMode + "<- in TriggerEnter2D");
            }
        }
    }


    protected IEnumerator DyingFeedback(Collider2D other) {
        float clipLength = 5f;
        /**
        AudioSource[] audioSources = new AudioSource[gameManager.GetComponents<AudioSource>().Length];
        audioSources = gameManager.GetComponents<AudioSource>();
        AudioSource audioSource = audioSources[audioSources.Length - 1];

        //  WallTower
        if (other.GetComponent<BaseIdentity>().myEntitySubType == EntitySubType.WallTower)
        {
            clipLength = gameManager.eventSounds[0].length;
            audioSource.clip = gameManager.eventSounds[0];
            audioSource.Play();
        }
        //  Player
        if (other.GetComponent<BaseIdentity>().myEntitySubType == EntitySubType.PlayerTypeA)
        {
            clipLength = gameManager.eventSounds[1].length;
            audioSource.clip = gameManager.eventSounds[1];
            audioSource.Play();
        }
        **/
        yield return new WaitForSeconds(clipLength);
    }


    //  This method can be called using a collider reference from the object which is calling the method.
    //  Or a collider referende for another object.
    //  The reason for this is that a dead/deactivated gameobject cannot call the method.
    //  So the method is called from another object.
    protected IEnumerator ShakeHandler(Collider2D thisObject, Collider2D otherObject) {
        /**
        float[] angleDiff = new float[gameManager.players.Length];
        //  Calculating the angles between the players and the Actor.
        //  Shake-cause due to other:
        if (thisObject == null)
            for (int i = 0; i < gameManager.players.Length; i++)
            {
                angleDiff[i] = Vector2.Angle(new Vector2(gameManager.players[i].transform.position.x, gameManager.players[i].transform.position.y),
                                             new Vector2(otherObject.transform.position.x, otherObject.transform.position.y)
                                            );
            }
        //  Shake-cause due to this:
        if (otherObject == null)
            for (int i = 0; i < gameManager.players.Length; i++)
            {
                angleDiff[i] = Vector2.Angle(new Vector2(gameManager.players[i].transform.position.x, gameManager.players[i].transform.position.y),
                                             new Vector2(thisObject.transform.position.x, thisObject.transform.position.y)
                                            );
            }

        //  Here we shake the cameras of the players.
        //  But only if the Actor which was just killed is visible on their particular camera.
        Transform[] shakeThis = new Transform[gameManager.players.Length];
        for (int i = 0; i < gameManager.players.Length; i++)
        {
            if (angleDiff[i] < 27f)
            {
                shakeThis[i] = gameManager.cameraAxes[i];
                if (thisObject == null)
                {
                    //  Handling screen shake for WallTower
                    if (otherObject.GetComponent<BaseIdentity>().myEntitySubType == BaseIdentity.EntitySubType.WallTower)
                    {
                        if (otherObject.GetComponent<BaseIdentity>().HP <= 0f)
                            shakeThis[i].GetComponent<ScreenShake>().ShakeObject(75, 0.2f);
                    }
                    //  Handling screen shake for Keep
                    else if (otherObject.GetComponent<BaseIdentity>().myEntitySubType == BaseIdentity.EntitySubType.Keep)
                    {
                        if (otherObject.GetComponent<BaseIdentity>().HP <= 0f)
                            shakeThis[i].GetComponent<ScreenShake>().ShakeObject(75, 0.3f);
                    }
                }
                //  Handling screen shake for Cannon & WallTower & Keep
                if (otherObject == null)
                {
                    if (thisObject.GetComponent<BaseIdentity>().myEntitySubType == BaseIdentity.EntitySubType.Cannon)
                        shakeThis[i].GetComponent<ScreenShake>().ShakeObject(25, 0.05f);
                    else if (thisObject.GetComponent<BaseIdentity>().myEntitySubType == BaseIdentity.EntitySubType.WallTower)
                        shakeThis[i].GetComponent<ScreenShake>().ShakeObject(50, 0.1f);
                    else if (thisObject.GetComponent<BaseIdentity>().myEntitySubType == BaseIdentity.EntitySubType.Keep)
                        shakeThis[i].GetComponent<ScreenShake>().ShakeObject(50, 0.2f);
                }
            }
        }
        **/
        yield return new WaitForSeconds(5);
        
    }



}
