using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grapling : MonoBehaviour
{
    private Vector3 _mousePos;
    public Camera _camera;

    private bool _check;

    private DistanceJoint2D _distanceJoint;

    private LineRenderer _lineRenderer;

    //private SpringJoint2D _springJoint2D;

    public Vector2 _graplingVerlocity;
    private Rigidbody2D _rb;    

    private Vector3 _temPos;

    private void Awake()
    {
        _camera = GameObject.Find("CameraRig").GetComponentInChildren<Camera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
        _rb = GetComponent<Rigidbody2D>();        

        _distanceJoint = GetComponent<DistanceJoint2D>();
        _lineRenderer = GetComponent<LineRenderer>();
        //_springJoint2D = GetComponent<SpringJoint2D>();

        _distanceJoint.enabled = false;
        _check = true;
        _lineRenderer.positionCount = 0;
        //_springJoint2D.enabled = false;
    }

    
    // Update is called once per frame
    void Update()
    {
        GetMousePos();
        if (Input.GetMouseButtonDown(0) && _check)
        {
            
            //_rb.velocity = new Vector2(_rb.velocity.x*0, _rb.velocity.y*0);
            GetComponent<PlayerController>().enabled = false;
            _distanceJoint.enabled = true;
            _distanceJoint.connectedAnchor = _mousePos;
            //_springJoint2D.enabled = true;
            //_springJoint2D.connectedAnchor = _mousePos;
            _lineRenderer.positionCount = 2;
            _temPos = _mousePos;
            _check = false;
        }
        else if (Input.GetMouseButtonDown(0))
        {
                  
            
            _distanceJoint.enabled = false;
            //_springJoint2D.enabled = false;
            _check = true;
            _lineRenderer.positionCount = 0;
            GetComponent<PlayerController>().enabled = true;
            //gameObject.GetComponent<Rigidbody2D>().velocity = _graplingVerlocity;
        }
        DrawLine();
        
    }

    private void DrawLine()
    {
        if (_lineRenderer.positionCount <= 0)
        {
            return;
        }
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, _temPos);
    }

    private void GetMousePos()
    {
        _mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
    }
}
