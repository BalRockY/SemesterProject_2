using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



class Mentor : BaseBehavior {
    //private float prevHP = 0;

    protected override void Awake() {
        base.Awake();
        SetStats();
        //  Refs. to questObjects.
        //  -----------------------------------------------------------------------------
        for (int i = 0; i < AH_NameList.Count; i++) {
            GameObject _instance = GameObject.Find(AH_NameList[i]);
            if (_instance) {
                AH_RefList.Add(_instance.GetComponent<ActionHandler>());
            }
            else Debug.Log(this.name + " : I didn't find GameObject " + AH_NameList[i]);
        }
        //  -----------------------------------------------------------------------------
    }

    protected override void Start() {
        base.Start();
    }


    protected override void FixedUpdate() {
        base.FixedUpdate();
        CheckLoyalty(aggro);
    }


    //  Assigning base stats and HP.
    public void SetStats() {
        aggro = 0.4f;
        have_AH = false;
        myEntityType = EntityType.NonMovingEntity;
        myEntitySubType = EntitySubType.Mentor;
        myDefaultMode = Mode.Idle;
        myMode = myDefaultMode;
        baseHP = 100f;
        HP = baseHP;
        baseSpeed = 6f;
        speed = baseSpeed;
        baseDefence = 20f;
        defence = baseDefence;
        baseDamage = 25f;
        damage = baseDamage + 10f;
        attackSpeed = 1f;
        attackRange = 15f;

        existing = true;
    }

    public void BuffedStats() {
        damage = baseDamage * 1.3f;
        defence = baseDefence * 1.3f;
        speed = 8;
    }
    public void UnBuffedStats() {
        damage = baseDamage;
        defence = baseDefence;
        speed = baseSpeed;
    }

    /**
    protected override void Attack(Collider other) {
        base.Attack(other);
        //  Attack polish here ****************************
        //  ***********************************************
        if (this.gameObject.activeInHierarchy)
        {
            this.gameObject.GetComponent<AudioSource>().pitch = Random.Range(0.75f, 1.5f);
            this.gameObject.GetComponent<AudioSource>().Play();
            this.gameObject.GetComponentInChildren<Animator>().Play("PikeManAttack");
            //  Hurtsound other (player).
            if (other.gameObject.activeInHierarchy &&
                other.GetComponent<BaseIdentity>().myEntitySubType == BaseIdentity.EntitySubType.PlayerTypeA)
            {
                int trackPlaying = Random.Range(0, other.GetComponent<PlayerA>().hurtSounds.Length);
                AudioClip[] audioClip = other.GetComponent<PlayerA>().hurtSounds;
                other.GetComponent<AudioSource>().clip = audioClip[trackPlaying];
                other.GetComponent<AudioSource>().pitch = Random.Range(0.9f, 1.1f);
                other.GetComponent<AudioSource>().volume = 0.3f;
                other.GetComponent<AudioSource>().Play();
            }
        }
    }
    **/

    /**
    protected override void Move(GameObject gameObject, float signedRatio, float maxSpeed) {
        base.Move(this.gameObject, moveAxisInputValue, speed);
        //  Move polish here ******************************
        //  ***********************************************
        if (this.gameObject.activeInHierarchy)
        {
            // this.gameObject.GetComponentInChildren<Animator>().Play("PikeManWalk");
        }
    }
    **/



}

