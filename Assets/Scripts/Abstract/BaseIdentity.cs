using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseIdentity : MonoBehaviour {

    //  Entity types
    public enum EntityType {
        Avatar, NPC, NonMovingEntity
    }

    //  Entity sub-types.
    public enum EntitySubType {
        AvatarTypeA, Friend, Mentor, Oracle, InvisibleAH_Unique,
        UnivWalkingPCG_NPC, UnivFlyingPCG_NPC, Door,
        Wall
    }

    //  Which side is the entity on.
    public enum Loyalty { P0, P1, Neutral, Indestructible };

    //  Different modes
    public enum Mode {
        Auto, Idle, Move, Conversation, GetInRange, Combat,
        NPC_Follow, NPC_Defend
    }

    //  Misc.
    private string[] splitArray = new string[2];

    //  For all.
    [HideInInspector] public bool existing = false;
    public int myID;
    public Mode myMode;
    public Mode myDefaultMode;
    public EntityType myEntityType;
    public EntitySubType myEntitySubType;
    public Loyalty myLoyalty;
    public string myTag;
    private string myBaseTag;
    public float aggro;
    protected float aggroLimit = 0.5f;
    // ActionHandler
    public bool have_AH;
    public List<string> AH_NameList = new List<string>();
    [HideInInspector] public List<ActionHandler> AH_RefList = new List<ActionHandler>();
    // Dialogues
    public List<Item> inventory;
    public List<Dialogue> dialogues;

    public float attackRange;
    [HideInInspector] public int startLives = 3;
    public int lives;
    public float baseHP;
    public float HP;

    //  For Player, NPC & Projektile
    public float baseSpeed;
    public float speed;
    //public float destinationAngle = 1000;

    //  For Player, NPC
    public float baseDefence;
    public float defence;
    public float baseDamage;
    public float damage;
    public float attackSpeed;
    public GameObject[] myGear;

    //  For Non-noving Entity
    [HideInInspector] public float offsetRotation = 0f;

    //  For Gear
    public float gearDamage;
    public Vector3 gearOffset;

    //  For Projektiles
    public float projektileDamage;
    public Vector3 projektileOffset;


    //  Setting ID and Loyalty.
    protected virtual void Awake() {
        CheckLoyalty(aggro);
    }

    protected void CheckLoyalty(float aggro) {
        myTag = this.gameObject.tag;
        if (myTag.Contains("_P")) {
            splitArray = myTag.Split('_');
            myBaseTag = splitArray[0];
            if (aggro == 0) {
                myID = 0;
                myLoyalty = Loyalty.P0;
            }
            else {
                myID = 1;
                myLoyalty = Loyalty.Neutral;
            }
            this.gameObject.tag = myBaseTag + "_P" + myID;
        }
        else {
            myBaseTag = myTag;
            myLoyalty = Loyalty.Neutral;
        }
    }


}
