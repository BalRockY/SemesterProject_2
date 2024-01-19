using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CamControl : MonoBehaviour
{
    public float dampTime = 0.2f;
    public float screenEdgeBuffer = 4f;
    //public float minSize = 6.5f;
    public float minSize = 15f;
    public List<Transform> targets;
    
    public Camera mainCamera;
    private float zoomSpeed;
    private Vector2 moveVelocity;
    private Vector2 desiredPosition;

    // Cam Control for PCG Scenes
    Scene currentScene;
    string currentSceneName;
    bool fullview;
    GameObject mapButton;
    GameObject GM_Obj;
    GameManager gameManager;
    PlayerController playerController;

    // Regarding mouse event.
    EventTrigger.Entry eventtype;

    // Regarding switching doorActor target
    [HideInInspector] public Transform door1;
    [HideInInspector] public Transform door3;
    [HideInInspector] public Transform doorTransform;




    private void Awake() {
        // Regarding mouse event.
        eventtype = new EventTrigger.Entry();
        eventtype.eventID = EventTriggerType.PointerClick;
        eventtype.callback.AddListener((eventData) => { HandleMouseEvent(); });
        // --------------------------------------------------------------------

        mainCamera = GetComponentInChildren<Camera>();


    }

    private void Start() {
        GM_Obj = GameObject.Find("GameManager");
        if (!GM_Obj) Debug.Log(this.name + " GameManager not found.");
        else {
            mapButton = GM_Obj.GetComponent<GameManager>().mapButton;
            gameManager = GM_Obj.GetComponent<GameManager>();
            playerController = gameManager.actorRefByBaseTag["Avatar_P"].GetComponent<PlayerController>();

            // Regarding mouse event
            mapButton.AddComponent<EventTrigger>();
            mapButton.GetComponent<EventTrigger>().triggers.Add(eventtype);
        }
    }

    private void HandleMouseEvent() {
        //playerController.Avatar_GrappleOff();
    }


    private void FixedUpdate() {
        currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "5_PCG_SemiFullRandom_Scene" || currentSceneName == "6_PCG_TrueFullRandom_Scene") {
            mapButton.SetActive(true);
        }
        else mapButton.SetActive(false);
        Move();
        Zoom();

        if (!gameManager._dm.rareCrystQuestComp) doorTransform = door1;
        else doorTransform = door3;
    }


    public void FullViewSwitch() {
        if (fullview) {
            fullview = false;
            IncludeDoorInCamView();
        }
        else {
            fullview = true;
            ExcludeDoorInCamView();
        }
    }

    private void IncludeDoorInCamView() {
        if (targets.Count < 2) {
            targets.Add(doorTransform);
        }
    }

    private void ExcludeDoorInCamView() {
        if (targets.Count > 1) {
            targets.Remove(doorTransform);
        }
    }


    private void Move() {
        FindAveragePosition();
        transform.position = Vector2.SmoothDamp(transform.position, desiredPosition, ref moveVelocity, dampTime);
    }


    private void FindAveragePosition()
    {
        Vector2 averagePos = new Vector2();
        int numTargets = 0;

        for (int i = 0; i < targets.Count; i++)
        {

            if (!targets[i].gameObject.activeSelf)
                continue;

            averagePos.x += targets[i].position.x;
            averagePos.y += targets[i].position.y;
            numTargets++;
        }

        if (numTargets > 0)
            averagePos /= numTargets;

        //averagePos.y = transform.position.y;

        desiredPosition = averagePos;
    }


    private void Zoom()
    {
        float requiredSize = FindRequiredSize();
        mainCamera.orthographicSize = Mathf.SmoothDamp(mainCamera.orthographicSize, requiredSize, ref zoomSpeed, dampTime);
    }


    private float FindRequiredSize()
    {
        Vector2 desiredLocalPos = transform.InverseTransformPoint(desiredPosition);

        float size = 0f;

        for (int i = 0; i < targets.Count; i++)
        {
            if (!targets[i].gameObject.activeSelf)
                continue;

            Vector2 targetLocalPos = transform.InverseTransformPoint(targets[i].position);

            Vector2 desiredPosToTarget = targetLocalPos - desiredLocalPos;

            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));

            size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / mainCamera.aspect);
        }

        size += screenEdgeBuffer;

        size = Mathf.Max(size, minSize);

        return size;
    }


    public void SetStartPositionAndSize()
    {
        FindAveragePosition();

        transform.position = desiredPosition;

        mainCamera.orthographicSize = FindRequiredSize();
    }


}
