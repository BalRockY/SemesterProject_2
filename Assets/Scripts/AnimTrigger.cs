using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimTrigger : MonoBehaviour
{
    public GameObject dustAnimation;
    public GameObject oracleBody;
    public GameObject oracleHead;
    bool hasPlayed;

    private void Start()
    {
        dustAnimation.SetActive(false);
        oracleBody.SetActive(false);
        oracleHead.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Avatar_P0" && hasPlayed == false)
        {
            Debug.Log("Triggered");
            dustAnimation.SetActive(true);
            oracleBody.SetActive(true);
            oracleHead.SetActive(true);
            hasPlayed = true;            
        }
    }
}
