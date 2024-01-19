using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour
{
    Transform playerPos;

    public float moveSpeed;

    public float aggroRange;

    float distanceToPlayer;

    bool initAttack;
    bool startAttack;

    SpriteRenderer _spr;

    Rigidbody2D _rb;

    Animator anim;


    void Start()
    {
        _spr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        StartCoroutine(WaitForSceneLoad());

        initAttack = false;
        startAttack = false;

    }

    private void GetPlayerTransform()
    {
        playerPos = GameObject.Find("Avatar_P(Clone)").transform;
    }

    private void Update()
    {
        
        if (playerPos != null)
        {            
            distanceToPlayer = Vector2.Distance(transform.position, playerPos.position);
            
            if (distanceToPlayer < aggroRange && initAttack == false)
            {
                initAttack = true;
                
                StartCoroutine(AttackCoroutine());
            }
            else if (distanceToPlayer > aggroRange)
            {
                initAttack = false;
                startAttack = false;
                _rb.velocity = Vector2.zero;
                anim.SetBool("isRunning", false);
                anim.SetBool("isPointing", false);
                StopAllCoroutines();
            }

            if(startAttack)
            {
                if (transform.position.x < playerPos.position.x)
                {
                    _spr.flipX = true;
                    _rb.velocity = new Vector2(moveSpeed, _rb.velocity.y);
                }
                // Run towards the right
                else if (transform.position.x > playerPos.position.x)
                {
                    _spr.flipX = false;
                    _rb.velocity = new Vector2(-moveSpeed, _rb.velocity.y);
                }
            }
        }

    }
  
    IEnumerator WaitForSceneLoad()
    {
        yield return new WaitForSeconds(1f);
        GetPlayerTransform();
    }

    IEnumerator AttackCoroutine()
    {        
        anim.SetBool("isPointing", true);
        yield return new WaitForSeconds(0.875f);
        startAttack = true;
        anim.SetBool("isPointing", false);
        anim.SetBool("isRunning", true);
        // Run towards the left

        
        
    }

}
