using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


class Door : BaseBehavior {

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
        aggro = 0.2f;
        have_AH = true;
        myEntityType = EntityType.NonMovingEntity;
        myEntitySubType = EntitySubType.Door;
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


}


