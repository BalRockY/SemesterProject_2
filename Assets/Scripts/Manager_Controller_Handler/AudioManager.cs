using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    /*
     *  Reference: Unity 5 Tutorial: Simple Audio Player: https://www.youtube.com/watch?v=zpiwhC8zp4A
     */

    //public AudioClip sound;
    public AudioSource source;
    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }
}
