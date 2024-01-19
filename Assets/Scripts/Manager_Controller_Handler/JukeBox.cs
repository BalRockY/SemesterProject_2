using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JukeBox : MonoBehaviour {

    //  Arrays of music for different Scenes
    public AudioSource jukebox;
    public AudioClip[] loading_Scene;
    public AudioClip[] city_Scene;
    public AudioClip[] tutorial_Scene;
    public AudioClip[] camp_Scene;
    public AudioClip[] oracle_Scene;
    public AudioClip[] PCG_Scene;
    public AudioClip[] playerDeath;
    public AudioClip[] gameEnd;
    public int trackPlaying;

    string currentSceneName;
    string previousSceneName = "_";
    bool playerDied = false;
    bool gameEnded = false;

    // Ref. vars
    string lookFor;
    GameManager gameManager;
    ButtonActions buttonActions;


    private void Awake() {
        Random.InitState(System.DateTime.Now.Millisecond);
        jukebox = this.gameObject.GetComponent<AudioSource>();
        jukebox.volume = 0.3f;

        //  Ref. to GameManager
        //  -----------------------------------------------------------------------------
        lookFor = "GameManager";
        GameObject _instance = GameObject.Find(lookFor);
        if (_instance) {
            gameManager = _instance.GetComponent<GameManager>();
        }
        else Debug.Log(this.name + " : I didn't find GameObject" + lookFor);
        //  -----------------------------------------------------------------------------

        //  Ref. to ButtonScripts
        //  -----------------------------------------------------------------------------
        lookFor = "ButtonScripts";
        _instance = GameObject.Find(lookFor);
        if (_instance) {
            buttonActions = _instance.GetComponent<ButtonActions>();
        }
        else Debug.Log(this.name + " : I didn't find GameObject" + lookFor);
        //  -----------------------------------------------------------------------------

        currentSceneName = SceneManager.GetActiveScene().name;

        // Subscribing to event ############################################
        gameManager.I_Respawned += PlayerRespawned;
    }


    void PlayerRespawned() {
        playerDied = true;
        jukebox.Stop();
    }


    private void FixedUpdate() {
        currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != previousSceneName) {
            jukebox.Stop();
            previousSceneName = currentSceneName;
        }
        if (gameEnded) jukebox.Stop();

        if (!jukebox.isPlaying) {
            if (!playerDied && !gameEnded) {
                switch (currentSceneName) {
                    case ButtonActions.Loading_Scene_Name:
                        trackPlaying = Random.Range(0, loading_Scene.Length);
                        jukebox.clip = loading_Scene[trackPlaying];
                        break;
                    case ButtonActions.City_Scene_Name:
                        trackPlaying = Random.Range(0, city_Scene.Length);
                        jukebox.clip = city_Scene[trackPlaying];
                        break;
                    case ButtonActions.Tutorial_Scene_Name:
                        trackPlaying = Random.Range(0, tutorial_Scene.Length);
                        jukebox.clip = tutorial_Scene[trackPlaying];
                        break;
                    case ButtonActions.Camp_Scene_Name:
                        trackPlaying = Random.Range(0, camp_Scene.Length);
                        jukebox.clip = camp_Scene[trackPlaying];
                        break;
                    case ButtonActions.Oracle_Scene_Name:
                        trackPlaying = Random.Range(0, oracle_Scene.Length);
                        jukebox.clip = oracle_Scene[trackPlaying];
                        break;
                    case ButtonActions.PCG_Scene1_Name:
                        trackPlaying = Random.Range(0, PCG_Scene.Length);
                        jukebox.clip = PCG_Scene[trackPlaying];
                        break;
                    case ButtonActions.PCG_Scene2_Name:
                        trackPlaying = Random.Range(0, PCG_Scene.Length);
                        jukebox.clip = PCG_Scene[trackPlaying];
                        break;
                }
            }
            else if (playerDied && !gameEnded) {
                playerDied = false;
                trackPlaying = Random.Range(0, playerDeath.Length);
                jukebox.clip = playerDeath[trackPlaying];
            }
            else if (gameEnded) {
                gameEnded = false;
                trackPlaying = Random.Range(0, gameEnd.Length);
                jukebox.clip = gameEnd[trackPlaying];
            }
            jukebox.Play();
        }


    }


}
